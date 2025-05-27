using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para gestionar eventos de carga de escena.

/// <summary>
/// Script principal que gestiona el objeto Jugador.
/// Implementa el patrón Singleton para asegurar una única instancia persistente del jugador
/// a través de las escenas. Coordina los componentes de movimiento, animación, combate y vida.
/// </summary>
public class Player : MonoBehaviour
{
    // --- Singleton Instance ---
    /// <summary>
    /// Instancia estática y pública del Jugador, accesible desde cualquier script.
    /// </summary>
    public static Player Instance { get; private set; }
    // --- Referencias a Componentes del Jugador ---
    [Tooltip("Referencia al script PlayerMovement para la lógica de movimiento.")]
    [SerializeField] private PlayerMovement movement;
    [Tooltip("Referencia al script PlayerAnimation para controlar las animaciones.")]
    [SerializeField] private PlayerAnimation playerAnimation;
    [Tooltip("Referencia al script PlayerCombat para la lógica de combate.")]
    [SerializeField] private PlayerCombat combat;
    [Tooltip("Referencia al script PlayerHealth para gestionar la vida del jugador.")]
    [SerializeField] private PlayerHealth health;
    private Collider2D playerCollider; // Referencia al Collider2D principal del jugador.

    /// <summary>
    /// Se llama una vez cuando el script es cargado.
    /// Implementa la lógica Singleton para asegurar una única instancia persistente.
    /// Obtiene referencias a los componentes esenciales del jugador.
    /// </summary>
    private void Awake()
    {
        // --- Lógica Singleton y Persistencia ---
        if (Instance != null && Instance != this)
        {
            // Si ya existe una instancia y no es esta, destruye este GameObject duplicado.
            Debug.LogWarning("Instancia duplicada de Player encontrada. Destruyendo duplicado.", gameObject);
            Destroy(gameObject);
            return; // Detener la ejecución de Awake en el duplicado.
        }
        Instance = this; // Establece esta instancia como la Singleton.
        DontDestroyOnLoad(gameObject); // Hace que el GameObject del jugador no se destruya al cargar nuevas escenas.
        // Debug.Log("Player marcado como DontDestroyOnLoad.");
        // --- Fin Singleton ---
        // --- Obtener Componentes ---
        // Debug.Log("--- Player.Awake() INICIO (Instancia Persistente) ---");
        movement = GetComponent<PlayerMovement>();
        playerAnimation = GetComponent<PlayerAnimation>();
        combat = GetComponent<PlayerCombat>();
        health = GetComponent<PlayerHealth>();
        playerCollider = GetComponent<Collider2D>(); // Obtiene el collider para futuras referencias si es necesario.
        if (health == null) {
            Debug.LogError("¡ERROR! PlayerHealth component NO ENCONTRADO en Player.Awake(). El jugador podría no funcionar correctamente.", this);
            return;
        }
        // Configura los listeners de eventos, como el de la muerte.
        SetupEventListeners();
    }

    /// <summary>
    /// Configura los listeners para eventos, como el evento OnDeath de PlayerHealth.
    /// Se asegura de no suscribirse múltiples veces.
    /// </summary>
    private void SetupEventListeners()
    {
         if (health != null && health.OnDeath != null)
         {
             // Remueve primero para evitar suscripciones duplicadas, luego añade.
             health.OnDeath.RemoveListener(HandleDeath);
             health.OnDeath.AddListener(HandleDeath);
         }
    }

    /// <summary>
    /// Se llama una vez por frame.
    /// Gestiona la entrada del jugador para movimiento y ataque si está vivo.
    /// </summary>
    private void Update()
    {
        // No procesar input si el jugador no tiene vida o no está vivo.
        if (health == null || !health.IsAlive()) return;
        // --- Lógica de Movimiento y Ataque ---
        movement.HandleInput(); // Procesa la entrada de movimiento desde el script PlayerMovement.
        Vector2 moveInput = movement.GetMoveInput(); // Obtiene el vector de movimiento.
        // Comprueba si se presiona la tecla de ataque y el sistema de combate está listo.
        if (Input.GetKeyDown(KeyCode.Z) && combat != null && !combat.IsAttacking()) // Asume Z como tecla de ataque.
        {
            combat.Attack(); // Inicia un ataque.
        }
        // Aplica el movimiento si el componente de movimiento existe.
        if (movement != null)
        {
            movement.Move(moveInput, movement.GetMoveSpeed());
        }
        // Actualiza la animación basada en el input y velocidad.
        if (playerAnimation != null)
        {
            playerAnimation.UpdateAnimation(moveInput, movement.GetMoveSpeed());
        }
        // --- FIN Lógica Movimiento/Ataque ---
    }

    /// <summary>
    /// Manejador para el evento OnDeath de PlayerHealth.
    /// Ejecuta la lógica de muerte del jugador (detener música, movimiento, animaciones, etc.).
    /// </summary>
    public void HandleDeath()
    {
        // Notifica al BattleMusicManager si existe.
        if (BattleMusicManager.Instance != null)
        {
            BattleMusicManager.Instance.PlayerDied();
        }
        // Detiene el movimiento y deshabilita los componentes de acción.
        if (movement != null) {
            movement.StopMoving();
            movement.enabled = false;
        }
        if (combat != null) {
            combat.enabled = false;
        }
        // Reproduce la animación de muerte.
        if (playerAnimation != null) {
            playerAnimation.PlayDeathAnimation();
        }
        // Programa la destrucción del GameObject del jugador después de un retraso.
        Destroy(gameObject, 5f);
    }

    /// <summary>
    /// Se llama cuando el GameObject se habilita.
    /// Se suscribe a eventos necesarios, como la carga de escena.
    /// </summary>
    private void OnEnable()
    {
        // Se suscribe al evento sceneLoaded para posicionar al jugador después de una transición.
        SceneManager.sceneLoaded += OnSceneLoaded;
        // Re-configura listeners de eventos (especialmente importante si el objeto fue desactivado y reactivado).
        SetupEventListeners();
    }

    /// <summary>
    /// Se llama cuando el GameObject se deshabilita.
    /// Se desuscribe de eventos para prevenir errores.
    /// </summary>
    private void OnDisable()
    {
        // Se desuscribe del evento sceneLoaded.
        SceneManager.sceneLoaded -= OnSceneLoaded;
        // Se desuscribe del evento de muerte.
        if (health != null && health.OnDeath != null) // Añadir null check para OnDeath también
        {
            health.OnDeath.RemoveListener(HandleDeath);
        }
    }

    /// <summary>
    /// Se llama cuando el GameObject es destruido.
    /// Limpia la referencia Singleton si esta es la instancia que se destruye.
    /// </summary>
     private void OnDestroy()
     {
          // Asegurarse de desuscribir por si OnDisable no se llamó (ej. al cerrar el juego).
          if (health != null && health.OnDeath != null)
          {
               health.OnDeath.RemoveListener(HandleDeath);
          }
         // Si esta es la instancia Singleton, la limpia para permitir una nueva si es necesario.
         if (Instance == this) {
             Instance = null;
         }
     }

    /// <summary>
    /// Método que se llama automáticamente después de que una nueva escena ha terminado de cargar.
    /// Llama a la lógica para posicionar al jugador en el punto de entrada correcto.
    /// </summary>
    /// <param name="scene">La escena que se cargó.</param>
    /// <param name="mode">El modo en que se cargó la escena.</param>
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindAndMoveToEntryPoint(); // Llama a la función para buscar y moverse.
    }

    /// <summary>
    /// Busca un PlayerSpawner en la escena actual que coincida con el 'entryPointID'
    /// guardado en PlayerSpawnManager y mueve al jugador a esa posición.
    /// Limpia el 'entryPointID' después de usarlo.
    /// </summary>
    void FindAndMoveToEntryPoint()
    {
        // Si no se especificó un punto de entrada (entryPointID está vacío o nulo), no hace nada.
        if (string.IsNullOrEmpty(PlayerSpawnManager.entryPointID))
        {
            return;
        }
        // Busca todos los objetos PlayerSpawner en la escena actual.
        // Se usa el modelo de simulación PlayerSpawnManager: Para obtener el ID del punto de entrada.
        PlayerSpawner[] spawners = FindObjectsByType<PlayerSpawner>(FindObjectsSortMode.None); // FindObjectsByType es más moderno que FindObjectsOfType.
        bool foundSpawner = false;
        foreach (PlayerSpawner spawner in spawners)
        {
            // Compara el identificador de cada spawner con el ID guardado.
            if (spawner.entryPointIdentifier == PlayerSpawnManager.entryPointID)
            {
                // Mueve el transform de este jugador a la posición y rotación del spawner encontrado.
                this.transform.position = spawner.transform.position;
                this.transform.rotation = spawner.transform.rotation; // Opcional: ajusta también la rotación.
                foundSpawner = true;
                break; // Sale del bucle una vez que se encuentra el spawner correcto.
            }
        }

        if (!foundSpawner)
        {
            Debug.LogWarning($"Player: No se encontró un PlayerSpawner con el ID: '{PlayerSpawnManager.entryPointID}' en la escena actual '{SceneManager.GetActiveScene().name}'. El jugador permanecerá en su posición actual.");
        }
        // Limpia el ID del punto de entrada para que no se reutilice accidentalmente en futuras cargas de escena.
        // Se usa el modelo de simulación PlayerSpawnManager: Limpieza del ID.
        PlayerSpawnManager.entryPointID = null;
    }
}