using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Para Enumerable.Range y OrderBy

/// <summary>
/// Gestiona la generación de cofres en la escena, determinando su cantidad,
/// ubicación y el contenido de cada uno usando modelos de simulación.
/// </summary>
public class ChestSpawner : MonoBehaviour
{
    [Header("Configuración General")]
    [Tooltip("Array de Transforms que marcan todas las posiciones posibles donde pueden aparecer cofres.")]
    [SerializeField] private Transform[] potentialSpawnPoints;
    [Tooltip("El Prefab del GameObject Cofre que se instanciará.")]
    [SerializeField] private GameObject chestPrefab;
    [Tooltip("Número mínimo de cofres a generar en la escena.")]
    [SerializeField] private int minChests = 3;
    [Tooltip("Número máximo de cofres a generar en la escena.")]
    [SerializeField] private int maxChests = 7;
    [Header("Contenido de Cofres")]
    [Tooltip("Prefab del item 'Contenedor de Corazón'.")]
    [SerializeField] private GameObject heartContainerPrefab;
    [Tooltip("Prefab del item 'Manzana'.")]
    [SerializeField] private GameObject applePrefab;
    [Header("Probabilidades de Transición de Contenido (Markov)")]
    // Estas probabilidades definen la Cadena de Markov para el contenido de los cofres.
    [Range(0f, 1f)] public float probAppleGivenApple = 0.7f;       // P(Manzana | Último fue Manzana)
    [Range(0f, 1f)] public float probContainerGivenApple = 0.3f;  // P(Contenedor | Último fue Manzana)
    [Range(0f, 1f)] public float probAppleGivenContainer = 0.9f;  // P(Manzana | Último fue Contenedor)
    [Range(0f, 1f)] public float probContainerGivenContainer = 0.1f;// P(Contenedor | Último fue Contenedor)
    [Range(0f, 1f)] public float probAppleGivenNone = 0.6f;       // P(Manzana | No hubo cofre anterior / Inicial)
    [Range(0f, 1f)] public float probContainerGivenNone = 0.4f;   // P(Contenedor | No hubo cofre anterior / Inicial)
    [Header("Configuración LCG para ChestSpawner")]
    // Parámetros para el Generador Lineal Congruencial (LCG) que este spawner utilizará.
    [SerializeField] private long baseSeedForLCG = 11223;
    [SerializeField] private long lcgMultiplier = 1664525;
    [SerializeField] private long lcgIncrement = 1013904223;
    [SerializeField] private long lcgModulus = 2147483647;
    [Tooltip("Cantidad de números pseudoaleatorios a generar y validar por el LCGManager.")]
    [SerializeField] private int numSamplesForLCG = 50;
    [Tooltip("Nivel Alpha para las pruebas estadísticas del LCG (ej: 0.05).")]
    [SerializeField] private double lcgAlphaTestLevel = 0.05;

    /// <summary>
    /// Define los posibles estados (contenidos) para la cadena de Markov de los cofres.
    /// </summary>
    public enum ItemChestState { None, Apple, HeartContainer }
    // Se usa el modelo de simulación LCGManager.
    private LCGManager lcgManager; // Instancia del generador de números LCG.
    private List<float> availableRandomNumbers; // Lista de números generados por LCGManager.
    private int currentRandomNumberIndex = 0; // Índice para consumir los números LCG.
    private bool lcgForSpawnerInitialized = false; // Flag para saber si el LCG se inicializó correctamente.
    // Se usa el modelo de simulación Markov.
    private Markov<ItemChestState> itemMarkovChain; // Instancia del gestor de la Cadena de Markov para el contenido.

    /// <summary>
    /// Se llama una vez cuando el script es cargado.
    /// Inicializa el LCGManager y la Cadena de Markov para el contenido de los cofres.
    /// </summary>
    void Awake()
    {
        // Genera una semilla única para esta instancia del spawner.
        long instanceSeed = System.DateTime.Now.Ticks + gameObject.GetInstanceID() + baseSeedForLCG;
        // Se usa el modelo de simulación LCGManager: Inicialización.
        lcgManager = new LCGManager(instanceSeed, lcgMultiplier, lcgIncrement, lcgModulus, lcgAlphaTestLevel);
        availableRandomNumbers = lcgManager.GetValidatedRiNumbers(numSamplesForLCG, out lcgForSpawnerInitialized);

        if (!lcgForSpawnerInitialized || availableRandomNumbers.Count == 0)
        {
            Debug.LogError("ChestSpawner: Falló la inicialización del LCG interno. Las decisiones aleatorias usarán Random.value() como fallback.", this);
        }

        // Inicializa la cadena de Markov para el contenido.
        InitializeItemMarkovChain();
    }

    /// <summary>
    /// Se llama una vez después de Awake, antes del primer frame de Update.
    /// Valida las probabilidades de Markov y genera los cofres.
    /// </summary>
    void Start()
    {
        ValidateProbabilities(); // Valida que las sumas de probabilidades de Markov sean correctas.
        SpawnChests(); // Comienza la generación de cofres.
    }

    /// <summary>
    /// Obtiene el siguiente número pseudoaleatorio de la secuencia generada por LCGManager.
    /// Este número se usará como entrada para la Cadena de Markov.
    /// </summary>
    /// <returns>Un float entre 0.0 y 1.0.</returns>
    private float GetNextLCGNumberForMarkov()
    {
        // Se usa el modelo de simulación LCGManager: Obtención de número para Markov.
        if (lcgForSpawnerInitialized && availableRandomNumbers.Count > 0)
        {
            float num = availableRandomNumbers[currentRandomNumberIndex];
            currentRandomNumberIndex = (currentRandomNumberIndex + 1) % availableRandomNumbers.Count; // Cicla en la lista
            return num;
        }
        Debug.LogWarning("ChestSpawner: LCG interno no disponible para Markov, usando Random.value().");
        return Random.value; // Fallback si el LCG falló.
    }

    /// <summary>
    /// Configura e inicializa la instancia de la Cadena de Markov para el contenido de los cofres.
    /// Define la matriz de transición con las probabilidades especificadas en el Inspector.
    /// </summary>
    void InitializeItemMarkovChain()
    {
        // Define la estructura de la matriz de transición para la cadena de Markov.
        var transitionMatrix = new Dictionary<ItemChestState, Dictionary<ItemChestState, float>>
        {
            // Desde el estado inicial "None" (para el primer cofre)
            {
                ItemChestState.None, new Dictionary<ItemChestState, float>
                {
                    { ItemChestState.Apple, probAppleGivenNone },
                    { ItemChestState.HeartContainer, probContainerGivenNone }
                }
            },
            // Desde el estado "Apple" (si el último cofre tuvo una manzana)
            {
                ItemChestState.Apple, new Dictionary<ItemChestState, float>
                {
                    { ItemChestState.Apple, probAppleGivenApple },
                    { ItemChestState.HeartContainer, probContainerGivenApple }
                }
            },
            // Desde el estado "HeartContainer" (si el último cofre tuvo un contenedor)
            {
                ItemChestState.HeartContainer, new Dictionary<ItemChestState, float>
                {
                    { ItemChestState.Apple, probAppleGivenContainer },
                    { ItemChestState.HeartContainer, probContainerGivenContainer }
                }
            }
        };
        // Se usa el modelo de simulación Markov: Inicialización.
        // Se le pasa el método que proveerá los números aleatorios (basados en LCG).
        itemMarkovChain = new Markov<ItemChestState>(ItemChestState.None, transitionMatrix, GetNextLCGNumberForMarkov);
    }

    /// <summary>
    /// Valida que las probabilidades de transición para cada estado de la Cadena de Markov sumen aproximadamente 1.0.
    /// Muestra una advertencia si no es así.
    /// </summary>
    void ValidateProbabilities()
    {
        if (!Mathf.Approximately(probAppleGivenNone + probContainerGivenNone, 1.0f))
            Debug.LogWarning("Probabilidades Markov para estado 'None' no suman 1.0!");
        if (!Mathf.Approximately(probAppleGivenApple + probContainerGivenApple, 1.0f))
            Debug.LogWarning("Probabilidades Markov para estado 'Apple' no suman 1.0!");
        if (!Mathf.Approximately(probAppleGivenContainer + probContainerGivenContainer, 1.0f))
            Debug.LogWarning("Probabilidades Markov para estado 'HeartContainer' no suman 1.0!");
    }

    /// <summary>
    /// Obtiene el siguiente número pseudoaleatorio de la secuencia generada por LCGManager.
    /// Este se usa para decisiones generales del Spawner, como la cantidad de cofres o su ubicación.
    /// </summary>
    /// <returns>Un float entre 0.0 y 1.0.</returns>
    private float GetNextGeneralRandomNumber()
    {
        // Se usa el modelo de simulación LCGManager: Obtención de número para decisiones generales.
        if (lcgForSpawnerInitialized && availableRandomNumbers.Count > 0)
        {
            float num = availableRandomNumbers[currentRandomNumberIndex];
            currentRandomNumberIndex = (currentRandomNumberIndex + 1) % availableRandomNumbers.Count; // Cicla en la lista
            return num;
        }
        Debug.LogWarning("ChestSpawner: LCG interno no disponible, usando Random.value() para decisión general.");
        return Random.value; // Fallback si el LCG falló.
    }

    /// <summary>
    /// Orquesta la generación de cofres: decide cuántos, dónde y qué contendrán.
    /// </summary>
    void SpawnChests()
    {
        // Validación inicial de referencias.
        if (potentialSpawnPoints.Length == 0 || chestPrefab == null || heartContainerPrefab == null || applePrefab == null)
        {
            Debug.LogError("Faltan referencias en ChestSpawner. No se generarán cofres.");
            return;
        }
        // Se usa el modelo de simulación LCGManager: Para decidir la cantidad de cofres.
        float randomForCount = GetNextGeneralRandomNumber();
        // Mapea el número LCG [0,1) al rango [minChests, maxChests].
        int numChestsToSpawn = minChests + Mathf.FloorToInt(randomForCount * (maxChests - minChests + 1));
        numChestsToSpawn = Mathf.Clamp(numChestsToSpawn, minChests, maxChests); // Asegura que esté en el rango.
        // No generar más cofres que puntos de spawn disponibles.
        numChestsToSpawn = Mathf.Min(numChestsToSpawn, potentialSpawnPoints.Length);
        // Debug.Log($"Intentando generar {numChestsToSpawn} cofres...");
        // Se usa el modelo de simulación LCGManager: Para seleccionar ubicaciones únicas.
        List<Transform> chosenSpawnPoints = SelectRandomUniqueSpawnPoints(numChestsToSpawn);
        // Itera sobre las ubicaciones elegidas para generar cada cofre.
        foreach (Transform spawnPoint in chosenSpawnPoints)
        {
            // Se usa el modelo de simulación Markov: Para determinar el contenido del cofre actual.
            ItemChestState nextItemState = itemMarkovChain.GetNextState();
            GameObject itemToContain = null;
            // Asigna el prefab del item basado en el estado determinado por la cadena de Markov.
            switch (nextItemState)
            {
                case ItemChestState.Apple:
                    itemToContain = applePrefab;
                    break;
                case ItemChestState.HeartContainer:
                    itemToContain = heartContainerPrefab;
                    break;
                default: // Caso de error o estado 'None' inesperado aquí.
                    Debug.LogError("Estado de item Markov inesperado o no manejado. Se usará Manzana como fallback.");
                    itemToContain = applePrefab;
                    break;
            }
            // Instancia el prefab del cofre en la posición y rotación del punto de spawn.
            GameObject chestInstance = Instantiate(chestPrefab, spawnPoint.position, spawnPoint.rotation);
            // Obtiene el controlador del cofre para asignarle el item.
            ChestController chestController = chestInstance.GetComponent<ChestController>();
            if (chestController != null)
            {
                chestController.SetContainedItem(itemToContain);
            }
            else
            {
                Debug.LogError($"El prefab del cofre ({chestPrefab.name}) no tiene el script ChestController!");
                Destroy(chestInstance); // Destruir instancia si no se puede configurar.
            }
        }
    }

    /// <summary>
    /// Selecciona un número 'count' de Transforms únicos y aleatorios desde la lista 'potentialSpawnPoints'.
    /// Utiliza números del LCGManager para la selección.
    /// </summary>
    /// <param name="count">El número de puntos de spawn únicos a seleccionar.</param>
    /// <returns>Una lista de Transforms seleccionados.</returns>
    private List<Transform> SelectRandomUniqueSpawnPoints(int count)
    {
        // Se usa el modelo de simulación LCGManager: Para la selección aleatoria de índices.
        List<Transform> allPoints = new List<Transform>(potentialSpawnPoints); // Copia para poder modificarla.
        List<Transform> chosenPoints = new List<Transform>();
        for (int i = 0; i < count; i++)
        {
            if (allPoints.Count == 0) break; // No hay más puntos disponibles para elegir.

            float randomForIndex = GetNextGeneralRandomNumber(); // Obtener número LCG.
            // Mapear el número LCG [0,1) a un índice válido en la lista actual de 'allPoints'.
            int randomIndex = Mathf.FloorToInt(randomForIndex * allPoints.Count);
            randomIndex = Mathf.Clamp(randomIndex, 0, allPoints.Count - 1); // Asegurar que el índice esté dentro de los límites.

            chosenPoints.Add(allPoints[randomIndex]); // Añadir el punto elegido a la lista de resultados.
            allPoints.RemoveAt(randomIndex); // Quitar el punto elegido de la lista de disponibles para asegurar unicidad.
        }
        return chosenPoints;
    }
}