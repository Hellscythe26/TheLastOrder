// ChestSpawner.cs
using UnityEngine;
using System.Collections.Generic; // Para List
using System.Linq; // Para OrderBy

public class ChestSpawner : MonoBehaviour
{
    [Header("Configuración General")]
    [Tooltip("Arrastra aquí todos los GameObjects que marcan posibles posiciones de cofres")]
    [SerializeField] private Transform[] potentialSpawnPoints;
    [Tooltip("Prefab del cofre a instanciar")]
    [SerializeField] private GameObject chestPrefab;
    [Tooltip("Número mínimo de cofres a generar")]
    [SerializeField] private int minChests = 5;
    [Tooltip("Número máximo de cofres a generar")]
    [SerializeField] private int maxChests = 10;

    [Header("Contenido de Cofres")]
    [Tooltip("Prefab del contenedor de corazón")]
    [SerializeField] private GameObject heartContainerPrefab;
    [Tooltip("Prefab de la manzana")]
    [SerializeField] private GameObject applePrefab;

    [Header("Cadena de Markov para Contenido")]
    [Tooltip("Probabilidad de obtener Manzana si el cofre ANTERIOR tenía Manzana")]
    [Range(0f, 1f)]
    [SerializeField] private float probAppleGivenApple = 0.7f; // P(A|A)

    [Tooltip("Probabilidad de obtener Contenedor si el cofre ANTERIOR tenía Manzana")]
    [Range(0f, 1f)]
    [SerializeField] private float probContainerGivenApple = 0.3f; // P(C|A)

    [Tooltip("Probabilidad de obtener Manzana si el cofre ANTERIOR tenía Contenedor")]
    [Range(0f, 1f)]
    [SerializeField] private float probAppleGivenContainer = 0.9f; // P(A|C)

    [Tooltip("Probabilidad de obtener Contenedor si el cofre ANTERIOR tenía Contenedor")]
    [Range(0f, 1f)]
    [SerializeField] private float probContainerGivenContainer = 0.1f; // P(C|C)

    // Estado de Markov (qué contenía el último cofre generado)
    private enum LastItemState { None, Apple, HeartContainer }
    private LastItemState lastItemGenerated = LastItemState.None;

    void Start()
    {
        // Validar probabilidades (cada par debe sumar ~1)
        ValidateProbabilities();

        SpawnChests();
    }

    void ValidateProbabilities()
    {
        if (!Mathf.Approximately(probAppleGivenApple + probContainerGivenApple, 1.0f))
        {
            Debug.LogWarning("Probabilidades dado Manzana no suman 1!");
            // Opcional: Normalizar aquí si quieres auto-corregir
        }
        if (!Mathf.Approximately(probAppleGivenContainer + probContainerGivenContainer, 1.0f))
        {
            Debug.LogWarning("Probabilidades dado Contenedor no suman 1!");
        }
    }

    void SpawnChests()
    {
        if (potentialSpawnPoints.Length == 0 || chestPrefab == null || heartContainerPrefab == null || applePrefab == null)
        {
            Debug.LogError("Faltan referencias en ChestSpawner (SpawnPoints, Prefabs). No se generarán cofres.");
            return;
        }

        int numChestsToSpawn = Random.Range(minChests, maxChests + 1);
        // Asegurarse de no intentar generar más cofres que puntos disponibles
        numChestsToSpawn = Mathf.Min(numChestsToSpawn, potentialSpawnPoints.Length);

        Debug.Log($"Intentando generar {numChestsToSpawn} cofres...");

        // --- Selección Aleatoria de Ubicaciones ÚNICAS ---
        // 1. Crear una lista de índices de los puntos disponibles
        List<int> availableIndices = Enumerable.Range(0, potentialSpawnPoints.Length).ToList();
        // 2. Barajar aleatoriamente la lista de índices
        System.Random rng = new System.Random();
        availableIndices = availableIndices.OrderBy(x => rng.Next()).ToList();
        // 3. Tomar los primeros 'numChestsToSpawn' índices de la lista barajada
        List<int> chosenIndices = availableIndices.Take(numChestsToSpawn).ToList();
        // -------------------------------------------------

        // --- Generar Cofres en las Ubicaciones Elegidas ---
        foreach (int index in chosenIndices)
        {
            Transform spawnPoint = potentialSpawnPoints[index];

            // --- Decidir Contenido usando Cadena de Markov ---
            GameObject itemToContain = DetermineNextItem();
            // ---------------------------------------------

            // Instanciar el cofre
            GameObject chestInstance = Instantiate(chestPrefab, spawnPoint.position, spawnPoint.rotation);

            // Configurar el cofre con el item que contendrá
            ChestController chestController = chestInstance.GetComponent<ChestController>();
            if (chestController != null)
            {
                chestController.SetContainedItem(itemToContain);
                //Debug.Log($"Cofre generado en {spawnPoint.name} contendrá {itemToContain.name}");
            }
            else
            {
                Debug.LogError($"El prefab del cofre ({chestPrefab.name}) no tiene el script ChestController!");
                Destroy(chestInstance); // Destruir si está mal configurado
            }
        }
         Debug.Log($"Generados {chosenIndices.Count} cofres.");
    }

    // --- Lógica de la Cadena de Markov para el Contenido ---
    GameObject DetermineNextItem()
    {
        float randomValue = Random.value; // Valor aleatorio [0.0, 1.0)
        GameObject chosenItem;

        // Determinar probabilidades basadas en el estado anterior
        float probApple; // Probabilidad de que el item actual sea Manzana

        switch (lastItemGenerated)
        {
            case LastItemState.Apple:
                probApple = probAppleGivenApple;
                break;
            case LastItemState.HeartContainer:
                probApple = probAppleGivenContainer;
                break;
            case LastItemState.None: // Primer cofre, usar probabilidades base (podríamos definirlas o usar un promedio)
            default:
                // Para el primer cofre, usemos una probabilidad simple (ej. 50/50 o basado en Apple|Apple)
                // O podrías definir P(A|None) y P(C|None) explícitamente
                probApple = probAppleGivenApple; // Usar P(A|A) como una base razonable
                // Otra opción: probApple = 0.5f;
                break;
        }

        // Decidir el item actual
        if (randomValue < probApple)
        {
            chosenItem = applePrefab;
            lastItemGenerated = LastItemState.Apple; // Actualizar estado
        }
        else
        {
            chosenItem = heartContainerPrefab;
            lastItemGenerated = LastItemState.HeartContainer; // Actualizar estado
        }

        return chosenItem;
    }
    // --- Fin Lógica Markov ---
}