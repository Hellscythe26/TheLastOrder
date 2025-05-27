using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para suscribirse al evento de carga de escena.

/// <summary>
/// Controla la cámara principal del juego para que siga a un objetivo (el jugador).
/// Implementa un patrón Singleton y persistencia para que la misma cámara
/// siga al jugador a través de diferentes escenas.
/// </summary>
public class CameraController : MonoBehaviour
{
    /// <summary>
    /// Instancia estática Singleton de CameraController para acceso global.
    /// </summary>
    public static CameraController Instance { get; private set; }
    [Header("Seguimiento del Objetivo")]
    [Tooltip("El Transform del GameObject que la cámara debe seguir (normalmente el jugador).")]
    public Transform objective;
    // La variable camraVelocity fue eliminada en favor de un seguimiento directo en LateUpdate
    // Si se reintroduce un suavizado, esta variable y su lógica necesitarían ser restauradas.
    // public float camraVelocity = 1f; // Si se usara para suavizado.
    [Tooltip("Desplazamiento (offset) de la cámara respecto a la posición del objetivo.")]
    public Vector3 scrolling; // Define la posición relativa de la cámara al objetivo.
    [Header("Auto-Find Player (Si 'Objective' es null)")]
    [Tooltip("Tag del GameObject del Jugador que se buscará si 'objective' no está asignado.")]
    [SerializeField] private string playerTag = "Player";

    /// <summary>
    /// Se llama una vez cuando el script es cargado.
    /// Implementa la lógica Singleton y DontDestroyOnLoad para persistencia.
    /// Intenta asignar el objetivo (jugador) si no está ya asignado.
    /// </summary>
    void Awake()
    {
        // Lógica Singleton para asegurar una única instancia persistente.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destruye este GameObject si ya existe otra instancia.
            return;
        }
        Instance = this; // Establece esta como la instancia Singleton.
        DontDestroyOnLoad(gameObject); // Hace que esta cámara persista entre cargas de escena.
        // Si el objetivo no está asignado en el Inspector, intenta encontrarlo.
        if (objective == null)
        {
            FindAndSetPlayerObjective();
        }
    }

    /// <summary>
    /// Se llama cuando el GameObject se habilita.
    /// Se suscribe al evento 'sceneLoaded' de SceneManager para re-evaluar el objetivo al cargar una nueva escena.
    /// </summary>
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    /// <summary>
    /// Se llama cuando el GameObject se deshabilita.
    /// Se desuscribe del evento 'sceneLoaded' para prevenir errores.
    /// </summary>
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// Método llamado automáticamente después de que una nueva escena ha terminado de cargar.
    /// Intenta reasignar el objetivo del jugador para asegurar que la cámara siga al jugador persistente.
    /// </summary>
    /// <param name="scene">La escena que se cargó.</param>
    /// <param name="mode">El modo en que se cargó la escena.</param>
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Es una buena práctica re-buscar o re-validar el objetivo
        // después de cargar una escena, especialmente con objetos persistentes.
        FindAndSetPlayerObjective();
    }

    /// <summary>
    /// Busca el GameObject del jugador usando el 'playerTag' y lo asigna como el 'objective' de la cámara.
    /// Se llama desde Awake y OnSceneLoaded si el objetivo es nulo.
    /// </summary>
    public void FindAndSetPlayerObjective()
    {
        // Si ya hay un objetivo asignado, no es necesario buscar de nuevo.
        if (objective != null) return;

        // Se usa el modelo de simulación Player (Singleton): Para obtener la referencia al jugador.
        // (Aunque aquí se busca por Tag, Player.Instance.transform sería la forma directa si Player.cs ya está listo)
        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject != null)
        {
            objective = playerObject.transform; // Asigna el Transform del jugador como objetivo.
        }
        else
        {
            Debug.LogWarning($"CameraController ({gameObject.name}) NO PUDO encontrar un GameObject con el Tag '{playerTag}'. El seguimiento no funcionará hasta que se asigne un objetivo.", this);
        }
    }

    /// <summary>
    /// Se llama después de que todos los métodos Update han sido llamados, cada frame.
    /// Es el lugar ideal para el movimiento de cámara, para asegurar que el objetivo
    /// ya ha completado su movimiento del frame actual.
    /// </summary>
    private void LateUpdate()
    {
        // Si no hay objetivo asignado, no mover la cámara.
        if (objective == null) return;
        // Calcula la posición deseada de la cámara sumando el 'scrolling' (offset) a la posición del objetivo.
        Vector3 desiredPosition = objective.position + scrolling;
        // Mantiene la coordenada Z actual de la cámara para evitar movimientos indeseados en profundidad (típico en juegos 2D/2.5D).
        desiredPosition.z = transform.position.z;
        // Asigna directamente la posición deseada a la cámara para un seguimiento instantáneo (Opción 1 que elegiste).
        transform.position = desiredPosition;
    }
}