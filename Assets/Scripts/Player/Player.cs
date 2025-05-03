// --- En Player.cs ---
using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para SceneManager.sceneLoaded (opcional)

public class Player : MonoBehaviour
{
    // --- Singleton Instance ---
    public static Player Instance { get; private set; }

    // --- Referencias a Componentes (igual que antes) ---
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerAnimation playerAnimation;
    [SerializeField] private PlayerCombat combat;
    [SerializeField] private PlayerHealth health; // La vida persistirá con el jugador
    private Collider2D playerCollider;

    private void Awake()
    {
        // --- Lógica Singleton y Persistencia ---
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Instancia duplicada de Player encontrada. Destruyendo duplicado.", gameObject);
            // Opcional: Copiar datos importantes del duplicado al original si fuera necesario
            // Por ejemplo: Instance.health.SetHealth(this.health.CurrentHealth); // Necesitarías un método SetHealth
            Destroy(gameObject);
            return; // Detener ejecución de Awake en el duplicado
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // ¡Hacer persistente al jugador!
        Debug.Log("Player marcado como DontDestroyOnLoad.");
        // --- Fin Singleton ---


        // --- Obtener Componentes (igual que antes) ---
        Debug.Log("--- Player.Awake() INICIO (Instancia Persistente) ---");
        movement = GetComponent<PlayerMovement>();
        playerAnimation = GetComponent<PlayerAnimation>();
        combat = GetComponent<PlayerCombat>();
        health = GetComponent<PlayerHealth>();
        playerCollider = GetComponent<Collider2D>();

        if (health == null) {
            Debug.LogError("¡ERROR! PlayerHealth component NO ENCONTRADO en Player.Awake().", this);
            // Considera destruir si falta algo crítico
            // Destroy(gameObject);
            return;
        } else {
             Debug.Log("PlayerHealth component encontrado.");
        }

        // Suscribirse al evento de muerte (asegúrate de que no se suscriba múltiples veces)
        // La desuscripción en OnDisable/OnDestroy debería manejarlo si se carga una escena
        // donde ya existe un Player (que sería destruido por el Singleton)
        SetupEventListeners();

         Debug.Log("--- Player.Awake() FIN ---");

         // Opcional: Suscribirse al evento de carga de escena para posicionamiento
         // SceneManager.sceneLoaded += OnSceneLoaded; // Requiere método OnSceneLoaded
        }

    private void SetupEventListeners()
    {
         if (health != null && health.OnDeath != null)
         {
             // Quitar primero por seguridad, luego añadir
             health.OnDeath.RemoveListener(HandleDeath);
             health.OnDeath.AddListener(HandleDeath);
             Debug.Log("Suscripción a OnDeath realizada/actualizada.");
         }
         else if (health == null)
         {
             Debug.LogError("Health es null en SetupEventListeners");
         }
         else if (health.OnDeath == null)
         {
              Debug.LogError("health.OnDeath es null en SetupEventListeners");
         }
    }


    private void Update()
    {
        // Comprobar si está vivo (igual que antes)
        if (health == null || !health.IsAlive()) return;

        // --- Lógica de Movimiento/Ataque (MODIFICADA PREVIAMENTE para permitir movimiento al atacar) ---
        movement.HandleInput();
        Vector2 moveInput = movement.GetMoveInput();

        if (Input.GetKeyDown(KeyCode.Z) && combat != null && !combat.IsAttacking()) // Usas Z para atacar? Cambia si es necesario
        {
            combat.Attack();
        }

        // Moverse siempre
        if (movement != null)
        {
            movement.Move(moveInput, movement.GetMoveSpeed());
        }
        // Actualizar animación siempre (el script de animación decide qué mostrar)
        if (playerAnimation != null)
        {
            playerAnimation.UpdateAnimation(moveInput, movement.GetMoveSpeed());
        }
        // --- FIN Lógica Movimiento/Ataque ---
    }

    // HandleDeath sigue igual, ya notifica al BattleMusicManager
    public void HandleDeath()
    {
        Debug.LogError(">>> ¡¡¡ Player.HandleDeath() EJECUTADO !!! <<<");

        if (BattleMusicManager.Instance != null)
        {
            BattleMusicManager.Instance.PlayerDied();
        }

        if (movement != null) {
            movement.StopMoving();
            movement.enabled = false;
        }
        if (combat != null) {
            combat.enabled = false;
        }
        if (playerAnimation != null) {
            playerAnimation.PlayDeathAnimation();
        } else {
             Debug.LogError("PlayerAnimation es null en HandleDeath!");
        }

        // Destruir después de 5 segundos
        Destroy(gameObject, 5f); // A pesar de DontDestroyOnLoad, Destroy() sí lo destruye
        Debug.Log("Kai ha sido derrotado. Destrucción programada.");
    }

    private void OnEnable()
    {
        Debug.LogError("<<<<< PLAYER OnEnable >>>>> - Suscribiendo a eventos."); // Log para verificar
        // Suscribirse a la carga de escena AQUÍ
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Suscribirse a la muerte (asegurando no duplicados)
        SetupEventListeners(); // Llama a tu método existente para suscribir OnDeath
    }

    private void OnDisable()
    {
        Debug.LogError("<<<<< PLAYER OnDisable >>>>> - Desuscribiendo de eventos."); // Log para verificar
        // Desuscribirse de la carga de escena AQUÍ
        SceneManager.sceneLoaded -= OnSceneLoaded;

        // Desuscribirse de la muerte (tu código existente está bien)
        if (health != null) // Buena idea comprobar null aquí también
        {
            // Asumiendo que OnDeath existe si health existe (basado en PlayerHealth.Awake)
            health.OnDeath.RemoveListener(HandleDeath);
        }
    }

     private void OnDestroy()
     {
         Debug.Log("Player OnDestroy");
          if (health != null)
          {
               health.OnDeath.RemoveListener(HandleDeath);
          }
         // Limpiar referencia Singleton si este es el objeto que se destruye
         if (Instance == this) {
             Instance = null;
         }
     }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"Player: Escena '{scene.name}' cargada. Buscando punto de entrada...");
        FindAndMoveToEntryPoint(); // Llama a la función para buscar y moverse
    }

    // --- NUEVO: Método para buscar el punto de entrada y moverse ---
    void FindAndMoveToEntryPoint()
    {
        // Si no se especificó un punto de entrada, no hacer nada
        if (string.IsNullOrEmpty(PlayerSpawnManager.entryPointID))
        {
            Debug.Log("Player: No se especificó entryPointID.");
            return;
        }

        // Buscar TODOS los PlayerSpawner en la escena actual
        PlayerSpawner[] spawners = FindObjectsByType<PlayerSpawner>(FindObjectsSortMode.None);
        bool foundSpawner = false;

        foreach (PlayerSpawner spawner in spawners)
        {
            // Si el identificador del spawner coincide con el que guardamos...
            if (spawner.entryPointIdentifier == PlayerSpawnManager.entryPointID)
            {
                Debug.Log($"Player: Moviéndose al punto de entrada '{spawner.entryPointIdentifier}' en {spawner.gameObject.name}");
                // Mover al jugador a la posición y rotación del spawner
                this.transform.position = spawner.transform.position;
                this.transform.rotation = spawner.transform.rotation; // Opcional, si la rotación importa
                foundSpawner = true;
                break; // Salir del bucle una vez encontrado
            }
        }

        if (!foundSpawner)
        {
            Debug.LogWarning($"Player: No se encontró un PlayerSpawner con el ID: '{PlayerSpawnManager.entryPointID}' en esta escena.");
        }

        // Limpiar el ID para que no se reutilice accidentalmente si se recarga la escena
        PlayerSpawnManager.entryPointID = null;
    }
}