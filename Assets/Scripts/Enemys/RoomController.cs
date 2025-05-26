using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class RoomController : MonoBehaviour
{
    [Header("Configuración de la Sala")]
    [SerializeField] private GameObject[] doorsToLock;
    [SerializeField] private Collider2D activationTrigger;
    [Header("Configuración de Enemigos")]
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private int minEnemies = 3;
    [SerializeField] private int maxEnemies = 6;
    [Tooltip("Retraso en segundos entre la activación de cada enemigo por WaitingLine")]
    [SerializeField] private float delayBetweenEnemyActivation = 0.5f;
    [Header("Configuración LCG para RoomController")]
    [SerializeField] private long baseSeedForLCG = 78901;
    [SerializeField] private long lcgMultiplier = 1664525;
    [SerializeField] private long lcgIncrement = 1013904223;
    [SerializeField] private long lcgModulus = 2147483647;
    [Tooltip("Cuántos números Ri generar para las decisiones de este RoomController.")]
    [SerializeField] private int numSamplesForLCG = 50;
    [SerializeField] private double lcgAlphaTestLevel = 0.05;
    [Header("Estado (Solo Lectura)")]
    [SerializeField]
    private RoomState currentState = RoomState.Idle;
    private List<EnemyHealth> activeRoomEnemies = new List<EnemyHealth>();
    private bool playerCurrentlyInside = false;
    private LCGManager lcgManager;
    private List<float> lcgNumbersForRoom;
    private int currentLCGNumberIndex = 0;
    private bool lcgForRoomInitialized = false;
    private WaitingLine enemyActivationLine;
    private enum RoomState { Idle, Locked, Cleared }

    private void Awake()
    {
        if (activationTrigger == null && GetComponent<Collider2D>() != null)
        {
            activationTrigger = GetComponent<Collider2D>();
        }
        SetDoorsLocked(false);
        currentState = RoomState.Idle;
        long instanceSeed = System.DateTime.Now.Ticks + gameObject.GetInstanceID() + baseSeedForLCG;
        lcgManager = new LCGManager(instanceSeed, lcgMultiplier, lcgIncrement, lcgModulus, lcgAlphaTestLevel);
        lcgNumbersForRoom = lcgManager.GetValidatedRiNumbers(numSamplesForLCG, out lcgForRoomInitialized);
        if (!lcgForRoomInitialized || lcgNumbersForRoom.Count == 0)
        {
            Debug.LogError($"RoomController {gameObject.name}: Falló la inicialización del LCG. Se usará Random.value() como fallback.", this);
        }
        enemyActivationLine = new WaitingLine(this, delayBetweenEnemyActivation, SetEnemyActive);
    }

    private float GetNextRoomLCGNumber()
    {
        if (lcgForRoomInitialized && lcgNumbersForRoom.Count > 0)
        {
            float num = lcgNumbersForRoom[currentLCGNumberIndex];
            currentLCGNumberIndex = (currentLCGNumberIndex + 1) % lcgNumbersForRoom.Count;
            return num;
        }
        return Random.value;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (currentState == RoomState.Idle && other.CompareTag("Player"))
        {
            StartEncounter();
        }
    }

    public void StartEncounter()
    {
        if (currentState != RoomState.Idle) return;
        currentState = RoomState.Locked;
        playerCurrentlyInside = true;
        if (activationTrigger != null)
        {
            activationTrigger.enabled = false;
        }
        SetDoorsLocked(true);
        StartCoroutine(SpawnAndQueueEnemiesCoroutine());
    }

    private void SetDoorsLocked(bool locked)
    {
        foreach (GameObject door in doorsToLock)
        {
            if (door != null) door.SetActive(locked);
        }
    }

    private IEnumerator SpawnAndQueueEnemiesCoroutine()
    {
        if (enemyPrefabs.Length == 0 || spawnPoints.Length == 0)
        {
            Debug.LogError("RoomController: ¡No hay prefabs de enemigos o puntos de spawn asignados!");
            yield break;
        }
        activeRoomEnemies.Clear();
        float randomForCount = GetNextRoomLCGNumber();
        int enemiesToSpawn = minEnemies + Mathf.FloorToInt(randomForCount * (maxEnemies - minEnemies + 1));
        enemiesToSpawn = Mathf.Clamp(enemiesToSpawn, minEnemies, maxEnemies);
        enemiesToSpawn = Mathf.Min(enemiesToSpawn, spawnPoints.Length);
        List<GameObject> newlySpawnedEnemies = new List<GameObject>();
        for (int i = 0; i < enemiesToSpawn; i++)
        {
            float randomForPrefab = GetNextRoomLCGNumber();
            int prefabIndex = Mathf.FloorToInt(randomForPrefab * enemyPrefabs.Length);
            prefabIndex = Mathf.Clamp(prefabIndex, 0, enemyPrefabs.Length - 1);
            GameObject prefabToSpawn = enemyPrefabs[prefabIndex];
            float randomForSpawnPoint = GetNextRoomLCGNumber();
            int spawnPointIndex = Mathf.FloorToInt(randomForSpawnPoint * spawnPoints.Length);
            spawnPointIndex = Mathf.Clamp(spawnPointIndex, 0, spawnPoints.Length - 1);
            Transform spawnPoint = spawnPoints[spawnPointIndex];
            GameObject newEnemy = Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);
            SetEnemyActive(newEnemy, false);
            newlySpawnedEnemies.Add(newEnemy);
            EnemyHealth enemyHealth = newEnemy.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                activeRoomEnemies.Add(enemyHealth);
                enemyHealth.OnEnemyDiedCallback += HandleEnemyDefeated;
            }
            else
            {
                Debug.LogWarning($"RoomController: Enemigo {newEnemy.name} no tiene script EnemyHealth.");
            }
            yield return null;
        }
        enemyActivationLine.AddEnemiesToQueue(newlySpawnedEnemies);
        enemyActivationLine.StartProcessingQueue();
    }

    public void SetEnemyActive(GameObject enemy, bool isActive)
    {
        if (enemy == null) return;
        var movementScript = enemy.GetComponent<EnemyMovement>();
        var combatScript = enemy.GetComponent<EnemyCombat>();
        if (movementScript != null) movementScript.enabled = isActive;
        if (combatScript != null) combatScript.enabled = isActive;
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

    private void HandleEnemyDefeated(EnemyHealth defeatedEnemy)
    {
        if (defeatedEnemy != null)
        {
            defeatedEnemy.OnEnemyDiedCallback -= HandleEnemyDefeated;
        }
        if (activeRoomEnemies.Contains(defeatedEnemy))
        {
            activeRoomEnemies.Remove(defeatedEnemy);
            if (activeRoomEnemies.Count == 0 && currentState == RoomState.Locked)
            {
                CompleteEncounter();
            }
        }
    }

    private void CompleteEncounter()
    {
        currentState = RoomState.Cleared;
        playerCurrentlyInside = false;
        SetDoorsLocked(false);
    }

    private void OnDestroy()
    {
        foreach (var enemyHealth in activeRoomEnemies)
        {
            if (enemyHealth != null)
            {
                enemyHealth.OnEnemyDiedCallback -= HandleEnemyDefeated;
            }
        }
        activeRoomEnemies.Clear();
        enemyActivationLine?.StopProcessingQueue();
    }

    public void ResetRoomForRetry()
    {
        if (currentState == RoomState.Cleared || currentState == RoomState.Locked)
        {
            Debug.Log($"RoomController ({gameObject.name}): Reseteando la sala para reintento.");
            enemyActivationLine?.StopProcessingQueue();
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
            SetDoorsLocked(false);
            currentState = RoomState.Idle;
            playerCurrentlyInside = false;
            if (activationTrigger != null)
            {
                activationTrigger.enabled = true;
            }
            Debug.Log($"RoomController ({gameObject.name}): Sala reseteada a Idle.");
        }
    }
}