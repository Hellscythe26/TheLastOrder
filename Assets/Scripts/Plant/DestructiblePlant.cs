using UnityEngine;
using System.Collections.Generic; // Necesario para List<float>

/// <summary>
/// Controla el comportamiento de una planta destructible que puede recibir daño
/// y tiene una probabilidad de soltar un item "HeartPickup" al ser destruida.
/// Utiliza el LCGManager para la decisión de soltar el item.
/// </summary>
public class DestructiblePlant : MonoBehaviour, IDamageable // Implementa IDamageable para poder recibir daño
{
    [Header("Stats")]
    [Tooltip("Cantidad de daño que la planta puede resistir antes de destruirse.")]
    [SerializeField] private float health = 1f;
    [Header("Drops")]
    [Tooltip("Arrastra aquí el Prefab del HeartPickup que puede soltar esta planta.")]
    [SerializeField] private GameObject heartPickupPrefab;
    [Tooltip("Probabilidad (0.0 a 1.0) de que la planta suelte un HeartPickup al destruirse.")]
    [Range(0f, 1f)]
    [SerializeField] private float heartDropChance = 0.5f;
    [Header("LCG Parameters (para la decisión de drop)")]
    // Parámetros para el LCG que esta planta usará para la decisión de soltar un item.
    [Tooltip("Semilla base para el LCG, se combinará con valores dinámicos para cada instancia.")]
    [SerializeField] private long baseSeedForLCG = 54321;
    [SerializeField] private long lcgMultiplier = 1664525;
    [SerializeField] private long lcgIncrement = 1013904223;
    [SerializeField] private long lcgModulus = 2147483647; // Un primo de Mersenne común para LCGs.
    [Tooltip("Cantidad de números Ri a generar y probar. Para una sola decisión, 1 es suficiente si se confía en los parámetros, o más para asegurar pruebas estadísticas si se desea.")]
    [SerializeField] private int numSamplesForLCG = 10;
    [Tooltip("Nivel Alpha para las pruebas estadísticas del LCG (ej: 0.05).")]
    [SerializeField] private double lcgAlphaTestLevel = 0.05;
    private bool isAlive = true; // Estado para controlar si la planta ya ha sido destruida.
    // Se usa el generador de números LCGManager.
    private LCGManager lcgManager; // Instancia del generador de números.
    private List<float> availableRandomNumbers; // Lista de números generados por el LCGManager.
    private int currentRandomNumberIndex = 0; // Índice para consumir los números LCG.
    private bool lcgInitialized = false; // Indica si el LCG se inicializó correctamente.

    /// <summary>
    /// Se llama una vez cuando el script es cargado.
    /// Inicializa el LCGManager para esta instancia de la planta.
    /// </summary>
    private void Awake()
    {
        // Genera una semilla única para esta instancia de la planta.
        long instanceSeed = System.DateTime.Now.Ticks + gameObject.GetInstanceID() + baseSeedForLCG;
        // Se usa el generador de números LCGManager: Inicialización.
        lcgManager = new LCGManager(
            instanceSeed,
            lcgMultiplier,
            lcgIncrement,
            lcgModulus,
            lcgAlphaTestLevel
        );
        // Se usa el generador de números LCGManager: Obtención de la secuencia de números.
        availableRandomNumbers = lcgManager.GetValidatedRiNumbers(
            numSamplesForLCG, // Usa el valor configurado en el Inspector.
            out bool generationSucceeded
        );
        if (generationSucceeded && availableRandomNumbers != null && availableRandomNumbers.Count > 0)
        {
            lcgInitialized = true; // El LCG está listo para usarse.
        }
        else
        {
            lcgInitialized = false; // El LCG falló la inicialización.
            Debug.LogError($"DestructiblePlant {gameObject.name}: Falló la inicialización del LCG. La decisión de drop usará Random.value() como fallback.", this);
        }
    }

    /// <summary>
    /// Obtiene el siguiente número pseudoaleatorio de la secuencia generada por LCGManager.
    /// Si el LCG no se inicializó, usa Random.value() de Unity como fallback.
    /// </summary>
    /// <returns>Un float entre 0.0 y 1.0.</returns>
    private float GetNextLCGRandomNumber()
    {
        // Se usa el generador de números LCGManager: Obtención de un número de la secuencia.
        if (!lcgInitialized || availableRandomNumbers == null || availableRandomNumbers.Count == 0)
        {
            // Fallback si el LCG no está listo.
            Debug.LogWarning($"DestructiblePlant {gameObject.name}: LCG no inicializado o sin números. Usando Random.value() como fallback.");
            return Random.value;
        }
        // Obtiene el número actual y avanza el índice (ciclando si llega al final de la lista).
        float randomNumber = availableRandomNumbers[currentRandomNumberIndex];
        currentRandomNumberIndex = (currentRandomNumberIndex + 1) % availableRandomNumbers.Count;
        return randomNumber;
    }

    /// <summary>
    /// Implementación del método TakeDamage de la interfaz IDamageable.
    /// Reduce la salud de la planta y la destruye si la salud llega a cero.
    /// </summary>
    /// <param name="damage">La cantidad de daño a infligir.</param>
    public void TakeDamage(float damage)
    {
        if (!isAlive) return; // No procesar daño si ya está destruida.
        health -= damage; // Reduce la salud.
        if (health <= 0)
        {
            Die(); // Si la salud es cero o menos, la planta se destruye.
        }
    }

    /// <summary>
    /// Implementación del método IsAlive de la interfaz IDamageable.
    /// </summary>
    /// <returns>True si la planta aún no ha sido destruida, false en caso contrario.</returns>
    public bool IsAlive()
    {
        return isAlive;
    }

    /// <summary>
    /// Lógica que se ejecuta cuando la planta es destruida.
    /// Determina si se suelta un item y destruye el GameObject de la planta.
    /// </summary>
    private void Die()
    {
        if (!isAlive) return; // Prevenir ejecución múltiple.
        isAlive = false; // Marcar como destruida.
        // Comprueba si hay un prefab de item asignado para soltar.
        if (heartPickupPrefab != null)
        {
            // Se usa el generador de números LCGManager: Para la decisión de soltar un item.
            float randomValue = GetNextLCGRandomNumber(); // Obtiene un número de la secuencia LCG.

            // Compara el número aleatorio con la probabilidad de soltar el item.
            if (randomValue <= heartDropChance)
            {
                // Si el número es menor o igual a la probabilidad, instancia el item.
                Instantiate(heartPickupPrefab, transform.position, Quaternion.identity);
            }
        }
        // Finalmente, destruye el GameObject de la planta.
        Destroy(gameObject);
    }
}