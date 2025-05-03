// RoomController.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Necesario para Listas

public class RoomController : MonoBehaviour
{
    [Header("Configuración de la Sala")]
    [Tooltip("Arrastra aquí los GameObjects de las puertas/barreras a bloquear")]
    [SerializeField] private GameObject[] doorsToLock;
    [Tooltip("El Collider 2D de este objeto que actúa como trigger de activación")]
    [SerializeField] private Collider2D activationTrigger;

    [Header("Configuración de Enemigos")]
    [Tooltip("Arrastra aquí los Prefabs de los enemigos que pueden aparecer")]
    [SerializeField] private GameObject[] enemyPrefabs;
    [Tooltip("Arrastra aquí los Transforms de los puntos de spawn")]
    [SerializeField] private Transform[] spawnPoints;
    [Tooltip("Número mínimo de enemigos a generar")]
    [SerializeField] private int minEnemies = 3;
    [Tooltip("Número máximo de enemigos a generar")]
    [SerializeField] private int maxEnemies = 6;
    [Tooltip("Retraso en segundos entre la activación de cada enemigo (simula línea de espera)")]
    [SerializeField] private float delayBetweenEnemyActivation = 0.5f;

    [Header("Estado (Solo Lectura)")]
    [SerializeField] // Lo mostramos para depurar, pero no lo tocamos en inspector
    private RoomState currentState = RoomState.Idle;

    // Lista para llevar la cuenta de los enemigos vivos en la sala
    private List<EnemyHealth> activeEnemies = new List<EnemyHealth>(); // Asumiendo que tus enemigos tienen un script EnemyHealth

    private bool playerInside = false; // Para evitar reactivaciones

    private enum RoomState
    {
        Idle,      // Esperando al jugador
        Locked,    // Jugador dentro, puertas cerradas, enemigos activos
        Cleared    // Todos los enemigos derrotados, puertas abiertas
    }

    private void Awake()
    {
        if (activationTrigger == null)
        {
            activationTrigger = GetComponent<Collider2D>();
        }
        // Asegurarse de que las puertas empiezan desactivadas (desbloqueadas)
        SetDoorsLocked(false);
        currentState = RoomState.Idle;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Solo activar si entró el jugador y la sala está en estado Idle
        if (currentState == RoomState.Idle && other.CompareTag("Player")) // Asegúrate de que tu jugador tenga el Tag "Player"
        {
            Debug.Log("Jugador entró en la sala. Bloqueando...");
            StartEncounter();
        }
    }

    public void StartEncounter()
    {
        currentState = RoomState.Locked;
        playerInside = true; // Marcamos que el jugador está
        activationTrigger.enabled = false; // Desactivamos el trigger para no reactivar

        // Bloquear puertas
        SetDoorsLocked(true);

        // Generar enemigos
        StartCoroutine(SpawnAndActivateEnemies());

        // Aquí podrías añadir otras cosas: cambiar música, mostrar mensaje, etc.
    }

    private void SetDoorsLocked(bool locked)
    {
        foreach (GameObject door in doorsToLock)
        {
            if (door != null)
            {
                door.SetActive(locked);
            }
        }
        Debug.Log($"Puertas {(locked ? "Bloqueadas" : "Desbloqueadas")}");
    }

    private IEnumerator SpawnAndActivateEnemies()
    {
        if (enemyPrefabs.Length == 0 || spawnPoints.Length == 0)
        {
            Debug.LogError("¡No hay prefabs de enemigos o puntos de spawn asignados en RoomController!");
            yield break; // Salir de la corutina si no hay nada que spawnear
        }

        activeEnemies.Clear(); // Limpiar lista por si acaso
        int enemiesToSpawn = Random.Range(minEnemies, maxEnemies + 1); // +1 porque Range(int, int) excluye el máximo
        Debug.Log($"Generando {enemiesToSpawn} enemigos.");

        List<GameObject> spawnedEnemies = new List<GameObject>(); // Lista temporal para activar

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            // Elegir prefab y punto de spawn aleatorio
            GameObject prefabToSpawn = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

            // Instanciar enemigo
            GameObject newEnemy = Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);
            spawnedEnemies.Add(newEnemy); // Añadir a la lista temporal

            // Obtener su script de vida y suscribirse a su evento de muerte
            EnemyHealth enemyHealth = newEnemy.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                activeEnemies.Add(enemyHealth); // Añadir a la lista de seguimiento principal
                enemyHealth.OnEnemyDiedCallback += HandleEnemyDefeated; // Suscribirse al evento

                 // --- IMPORTANTE: Para la activación secuencial ---
                 // Desactivar componentes de IA/Movimiento inicialmente
                 SetEnemyActive(newEnemy, false);
                 // ---------------------------------------------

                 // Opcional: Hacer al enemigo hijo del RoomController para organizar la jerarquía
                 // newEnemy.transform.SetParent(this.transform);
            }
            else
            {
                Debug.LogWarning($"Enemigo {newEnemy.name} no tiene script EnemyHealth. No se rastreará.");
                // Considera destruir este enemigo si es un error crítico
                // Destroy(newEnemy);
                // O simplemente añadir el GameObject a una lista diferente si no necesitas rastrear vida
            }
             yield return null; // Pequeña pausa para evitar sobrecarga en un frame si spawneas muchos
        }

        // --- Activación Secuencial ("Línea de Espera") ---
        Debug.Log("Comenzando activación secuencial de enemigos...");
        foreach(GameObject enemyGO in spawnedEnemies)
        {
            if (enemyGO != null) // Comprobar si no fue destruido por alguna razón
            {
                SetEnemyActive(enemyGO, true); // Activar IA/Movimiento
                Debug.Log($"Activando enemigo: {enemyGO.name}");
                yield return new WaitForSeconds(delayBetweenEnemyActivation); // Esperar antes de activar el siguiente
            }
        }
         Debug.Log("Todos los enemigos activados.");
        // -------------------------------------------------
    }

    // Método helper para activar/desactivar IA del enemigo
    private void SetEnemyActive(GameObject enemy, bool isActive)
    {
        var movementScript = enemy.GetComponent<EnemyMovement>();
        var combatScript = enemy.GetComponent<EnemyCombat>();
        if (movementScript != null) movementScript.enabled = isActive;
        if (combatScript != null) combatScript.enabled = isActive;
        var rb = enemy.GetComponent<Rigidbody2D>();
    if (rb != null)
    {
        if (!isActive)
        {
            rb.linearVelocity = Vector2.zero; // Detener movimiento físico
            rb.angularVelocity = 0f;
            // Considera si quieres desactivar la simulación física por completo:
            // rb.simulated = isActive;
        } else {
             // rb.simulated = true; // Asegurarse de que esté simulado si lo desactivaste antes
        }
    }
        // Añade un log para verificar
        Debug.Log($"SetEnemyActive en '{enemy.name}' a: {isActive}. Movement Enabled: {movementScript?.enabled}. Combat Enabled: {combatScript?.enabled}");
    }


    // Este método se llamará cuando un enemigo muera (gracias a la suscripción al evento)
    private void HandleEnemyDefeated(EnemyHealth defeatedEnemy)
    {
        Debug.Log($"Enemigo derrotado: {defeatedEnemy.gameObject.name}");
        if (defeatedEnemy != null) // Comprobar si no es null (puede pasar si se destruye antes)
        {
            defeatedEnemy.OnEnemyDiedCallback -= HandleEnemyDefeated;
        }
        if (activeEnemies.Contains(defeatedEnemy))
        {
            activeEnemies.Remove(defeatedEnemy);
            Debug.Log($"Enemigos restantes: {activeEnemies.Count}");
            // Comprobar si ya no quedan enemigos
            if (activeEnemies.Count == 0 && currentState == RoomState.Locked)
            {
                CompleteEncounter();
            }
        } else
        {
            Debug.LogWarning($"HandleEnemyDefeated llamado para {defeatedEnemy?.name}, pero no estaba en la lista activeEnemies.");
        }
    }

    private void CompleteEncounter()
    {
        Debug.Log("¡Todos los enemigos derrotados! Desbloqueando sala.");
        currentState = RoomState.Cleared;

        // Desbloquear puertas
        SetDoorsLocked(false);

        // Aquí podrías añadir otras cosas: cambiar música a normal, dar recompensa, etc.
    }

     // --- Importante: Limpieza al destruir el objeto ---
    private void OnDestroy()
    {
        foreach(var enemyHealth in activeEnemies)
        {
            if(enemyHealth != null)
            {
                enemyHealth.OnEnemyDiedCallback -= HandleEnemyDefeated;
            }
        }
        activeEnemies.Clear();
    }
} 