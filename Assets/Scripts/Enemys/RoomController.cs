using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Gestiona la lógica de una sala de encuentro: bloqueo de puertas, generación de enemigos
/// y activación secuencial de estos usando modelos de simulación.
/// </summary>
public class RoomController : MonoBehaviour
{
    [Header("Configuración de la Sala")]
    [Tooltip("GameObjects que actúan como puertas/barreras a bloquear.")]
    [SerializeField] private GameObject[] doorsToLock;
    [Tooltip("Collider 2D (Trigger) que activa el encuentro al entrar el jugador.")]
    [SerializeField] private Collider2D activationTrigger;
    [Header("Configuración de Enemigos")]
    [Tooltip("Array de Prefabs de enemigos que pueden ser generados en esta sala.")]
    [SerializeField] private GameObject[] enemyPrefabs;
    [Tooltip("Array de Transforms que marcan las posiciones donde pueden aparecer los enemigos.")]
    [SerializeField] private Transform[] spawnPoints;
    [Tooltip("Número mínimo de enemigos a generar.")]
    [SerializeField] private int minEnemies = 3;
    [Tooltip("Número máximo de enemigos a generar.")]
    [SerializeField] private int maxEnemies = 6;
    [Tooltip("Retraso en segundos entre la activación de cada enemigo por el modelo WaitingLine.")]
    [SerializeField] private float delayBetweenEnemyActivation = 0.5f;
    [Header("Configuración LCG para RoomController")]
    // Parámetros para el LCG que este RoomController usará para sus decisiones aleatorias.
    [SerializeField] private long baseSeedForLCG = 78901;
    [SerializeField] private long lcgMultiplier = 1664525;
    [SerializeField] private long lcgIncrement = 1013904223;
    [SerializeField] private long lcgModulus = 2147483647;
    [Tooltip("Cuántos números Ri generar para las decisiones de este RoomController.")]
    [SerializeField] private int numSamplesForLCG = 50;
    [Tooltip("Nivel Alpha para las pruebas estadísticas del LCG.")]
    [SerializeField] private double lcgAlphaTestLevel = 0.05;
    [Header("Estado (Solo Lectura)")]
    [Tooltip("Estado actual de la sala (Idle, Locked, Cleared).")]
    [SerializeField]
    private RoomState currentState = RoomState.Idle;
    // Lista de enemigos activos generados por esta sala.
    private List<EnemyHealth> activeRoomEnemies = new List<EnemyHealth>();
    // Se usa el generador de números LCGManager.
    private LCGManager lcgManager; // Instancia del generador de números LCG.
    private List<float> lcgNumbersForRoom; // Lista de números generados por LCGManager.
    private int currentLCGNumberIndex = 0; // Índice para consumir los números LCG.
    private bool lcgForRoomInitialized = false; // Flag para saber si el LCG se inicializó bien.
    // Se usa el modelo de simulación WaitingLine.
    private WaitingLine enemyActivationLine; // Instancia que gestiona la activación secuencial de enemigos.

    /// <summary>
    /// Define los posibles estados de la sala de encuentro.
    /// </summary>
    private enum RoomState { Idle, Locked, Cleared }

    /// <summary>
    /// Se llama una vez cuando el script es cargado.
    /// Inicializa el estado de la sala, el LCGManager y el modelo WaitingLine.
    /// </summary>
    private void Awake()
    {
        // Asigna el trigger de activación desde el propio GameObject si no está asignado.
        if (activationTrigger == null && GetComponent<Collider2D>() != null)
        {
            activationTrigger = GetComponent<Collider2D>();
        }
        SetDoorsLocked(false); // Asegura que las puertas empiecen desbloqueadas.
        currentState = RoomState.Idle; // Estado inicial.
        // Se usa el modelo de simulación LCGManager: Inicialización.
        long instanceSeed = System.DateTime.Now.Ticks + gameObject.GetInstanceID() + baseSeedForLCG;
        lcgManager = new LCGManager(instanceSeed, lcgMultiplier, lcgIncrement, lcgModulus, lcgAlphaTestLevel);
        lcgNumbersForRoom = lcgManager.GetValidatedRiNumbers(numSamplesForLCG, out lcgForRoomInitialized);
        if (!lcgForRoomInitialized || lcgNumbersForRoom.Count == 0)
        {
            Debug.LogError($"RoomController {gameObject.name}: Falló la inicialización del LCG. Se usará Random.value() como fallback.", this);
        }
        // Se usa el modelo de simulación WaitingLine: Inicialización.
        // Se le pasa este MonoBehaviour (this) para que pueda ejecutar corutinas,
        // el delay entre activaciones, y el método SetEnemyActive como callback.
        enemyActivationLine = new WaitingLine(this, delayBetweenEnemyActivation, SetEnemyActive);
    }

    /// <summary>
    /// Obtiene el siguiente número pseudoaleatorio de la secuencia LCG de esta sala.
    /// Se usa para decisiones como cantidad de enemigos, tipo de enemigo y punto de spawn.
    /// </summary>
    /// <returns>Un float entre 0.0 y 1.0.</returns>
    private float GetNextRoomLCGNumber()
    {
        // Se usa el generador de números LCGManager: Obtención de número.
        if (lcgForRoomInitialized && lcgNumbersForRoom.Count > 0)
        {
            float num = lcgNumbersForRoom[currentLCGNumberIndex];
            currentLCGNumberIndex = (currentLCGNumberIndex + 1) % lcgNumbersForRoom.Count; // Cicla en la lista.
            return num;
        }
        // Fallback a Random.value() si el LCG no está listo.
        Debug.LogWarning($"RoomController {gameObject.name}: LCG no inicializado, usando Random.value() para decisión.");
        return Random.value;
    }

    /// <summary>
    /// Se llama cuando un Collider2D entra en el trigger de esta sala (si está configurado y activo).
    /// Inicia el encuentro si es el jugador y la sala está en estado Idle.
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (currentState == RoomState.Idle && other.CompareTag("Player"))
        {
            StartEncounter();
        }
    }

    /// <summary>
    /// Inicia la secuencia del encuentro: bloquea puertas, genera enemigos y comienza su activación.
    /// Puede ser llamado por OnTriggerEnter2D o externamente (ej. por un SentinelDetector).
    /// </summary>
    public void StartEncounter()
    {
        if (currentState != RoomState.Idle) return; // Solo se activa si la sala está en espera.

        currentState = RoomState.Locked; // Cambia el estado a bloqueado.
        // Desactiva el trigger de área para evitar reactivaciones.
        if (activationTrigger != null)
        {
            activationTrigger.enabled = false;
        }
        SetDoorsLocked(true); // Bloquea las puertas.
        StartCoroutine(SpawnAndQueueEnemiesCoroutine()); // Inicia la corutina de generación y encolado.
    }

    /// <summary>
    /// Activa o desactiva los GameObjects de las puertas/barreras.
    /// </summary>
    /// <param name="locked">True para bloquear (activar), false para desbloquear (desactivar).</param>
    private void SetDoorsLocked(bool locked)
    {
        foreach (GameObject door in doorsToLock)
        {
            if (door != null) door.SetActive(locked);
        }
    }

    /// <summary>
    /// Corutina que genera los enemigos, los añade a la lista de enemigos activos de la sala,
    /// los establece como inactivos inicialmente, y luego los encola en el modelo WaitingLine para su activación secuencial.
    /// </summary>
    private IEnumerator SpawnAndQueueEnemiesCoroutine()
    {
        if (enemyPrefabs.Length == 0 || spawnPoints.Length == 0)
        {
            Debug.LogError("RoomController: ¡No hay prefabs de enemigos o puntos de spawn asignados!");
            yield break; // Termina la corutina si no hay cómo generar enemigos.
        }
        activeRoomEnemies.Clear(); // Limpia la lista de enemigos de encuentros anteriores.
        // Se usa el generador de números LCGManager: Para decidir la cantidad de enemigos.
        float randomForCount = GetNextRoomLCGNumber();
        int enemiesToSpawn = minEnemies + Mathf.FloorToInt(randomForCount * (maxEnemies - minEnemies + 1));
        enemiesToSpawn = Mathf.Clamp(enemiesToSpawn, minEnemies, maxEnemies);
        enemiesToSpawn = Mathf.Min(enemiesToSpawn, spawnPoints.Length); // No más enemigos que puntos de spawn.

        List<GameObject> newlySpawnedEnemies = new List<GameObject>(); // Lista temporal para los recién generados.

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            // Se usa el generador de números LCGManager: Para elegir el tipo de enemigo.
            float randomForPrefab = GetNextRoomLCGNumber();
            int prefabIndex = Mathf.FloorToInt(randomForPrefab * enemyPrefabs.Length);
            prefabIndex = Mathf.Clamp(prefabIndex, 0, enemyPrefabs.Length - 1);
            GameObject prefabToSpawn = enemyPrefabs[prefabIndex];
            // Se usa el generador de números LCGManager: Para elegir el punto de spawn.
            float randomForSpawnPoint = GetNextRoomLCGNumber();
            int spawnPointIndex = Mathf.FloorToInt(randomForSpawnPoint * spawnPoints.Length);
            spawnPointIndex = Mathf.Clamp(spawnPointIndex, 0, spawnPoints.Length - 1);
            Transform spawnPoint = spawnPoints[spawnPointIndex];
            // Instancia el enemigo.
            GameObject newEnemy = Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);
            SetEnemyActive(newEnemy, false); // Nace inactivo.
            newlySpawnedEnemies.Add(newEnemy);
            // Registra el enemigo para rastrear su muerte.
            EnemyHealth enemyHealth = newEnemy.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                activeRoomEnemies.Add(enemyHealth);
                enemyHealth.OnEnemyDiedCallback += HandleEnemyDefeated; // Suscripción al evento de muerte.
            }
            else
            {
                Debug.LogWarning($"RoomController: Enemigo {newEnemy.name} no tiene script EnemyHealth y no será rastreado.");
            }
            yield return null; // Pequeña pausa para distribuir la carga de instanciación.
        }
        // Se usa el modelo de simulación WaitingLine: Añade los enemigos a la cola de activación.
        enemyActivationLine.AddEnemiesToQueue(newlySpawnedEnemies);
        // Se usa el modelo de simulación WaitingLine: Inicia el proceso de activación secuencial.
        enemyActivationLine.StartProcessingQueue();
    }

    /// <summary>
    /// Activa o desactiva los componentes de comportamiento de un enemigo (movimiento y combate).
    /// Este método es usado como callback por el modelo WaitingLine.
    /// </summary>
    /// <param name="enemy">El GameObject del enemigo a activar/desactivar.</param>
    /// <param name="isActive">True para activar, false para desactivar.</param>
    public void SetEnemyActive(GameObject enemy, bool isActive)
    {
        if (enemy == null) return;
        var movementScript = enemy.GetComponent<EnemyMovement>();
        var combatScript = enemy.GetComponent<EnemyCombat>();
        if (movementScript != null) movementScript.enabled = isActive;
        if (combatScript != null) combatScript.enabled = isActive;
        // Adicionalmente, se detiene el Rigidbody cuando se desactiva.
        var rb = enemy.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            if (!isActive)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }
    }

    /// <summary>
    /// Método que se ejecuta cuando un enemigo (suscrito a OnEnemyDiedCallback) muere.
    /// Elimina al enemigo de la lista de activos y comprueba si el encuentro ha terminado.
    /// </summary>
    /// <param name="defeatedEnemy">El script EnemyHealth del enemigo que murió.</param>
    private void HandleEnemyDefeated(EnemyHealth defeatedEnemy)
    {
        if (defeatedEnemy != null)
        {
            defeatedEnemy.OnEnemyDiedCallback -= HandleEnemyDefeated; // Importante desuscribirse.
        }
        if (activeRoomEnemies.Contains(defeatedEnemy))
        {
            activeRoomEnemies.Remove(defeatedEnemy);
            // Comprueba si todos los enemigos han sido derrotados y la sala estaba bloqueada.
            if (activeRoomEnemies.Count == 0 && currentState == RoomState.Locked)
            {
                CompleteEncounter();
            }
        }
    }

    /// <summary>
    /// Se ejecuta cuando todos los enemigos de la sala han sido derrotados.
    /// Cambia el estado de la sala a Cleared y desbloquea las puertas.
    /// </summary>
    private void CompleteEncounter()
    {
        currentState = RoomState.Cleared;
        // playerCurrentlyInside = false; // Resetear para posible reactivación.
        SetDoorsLocked(false); // Desbloquea las puertas.
    }

    /// <summary>
    /// Se llama cuando el GameObject RoomController es destruido.
    /// Limpia suscripciones a eventos y detiene la línea de espera si está activa.
    /// </summary>
    private void OnDestroy()
    {
        // Desuscribirse de todos los enemigos restantes.
        foreach (var enemyHealth in activeRoomEnemies)
        {
            if (enemyHealth != null)
            {
                enemyHealth.OnEnemyDiedCallback -= HandleEnemyDefeated;
            }
        }
        activeRoomEnemies.Clear();
        // Se usa el modelo de simulación WaitingLine: Detener el procesamiento al destruir.
        enemyActivationLine?.StopProcessingQueue();
    }

    /// <summary>
    /// Resetea el estado de la sala para permitir un nuevo encuentro.
    /// </summary>
    public void ResetRoomForRetry()
    {
        if (currentState == RoomState.Cleared || currentState == RoomState.Locked)
        {
            Debug.Log($"RoomController ({gameObject.name}): Reseteando la sala para reintento.");
            // Se usa el modelo de simulación WaitingLine: Detener activación en curso.
            enemyActivationLine?.StopProcessingQueue(); // Nombre corregido aquí.
            // Destruir enemigos activos y limpiar referencias.
            List<EnemyHealth> enemiesToDestroy = new List<EnemyHealth>(activeRoomEnemies);
            activeRoomEnemies.Clear();
            foreach (var enemyHealth in enemiesToDestroy)
            {
                if (enemyHealth != null)
                {
                    enemyHealth.OnEnemyDiedCallback -= HandleEnemyDefeated;
                    Destroy(enemyHealth.gameObject);
                }
            }
            SetDoorsLocked(false); // Asegurar que las puertas estén desbloqueadas.
            currentState = RoomState.Idle; // Volver al estado inicial.
            // Reactivar el trigger de área si existe y se usa.
            if (activationTrigger != null)
            {
                activationTrigger.enabled = true;
            }
        }
    }
}