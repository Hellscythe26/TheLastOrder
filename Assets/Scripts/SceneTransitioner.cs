using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para gestionar la carga de escenas.

/// <summary>
/// Gestiona la transición entre escenas del juego.
/// Puede ser activado por un trigger (Collider2D) o mediante una llamada directa a su método Transition().
/// Comunica al PlayerSpawnManager el punto de entrada deseado en la siguiente escena.
/// </summary>
public class SceneTransitioner : MonoBehaviour
{
    [Header("Configuración de Transición")]
    [Tooltip("Nombre EXACTO del archivo de escena que se va a cargar (debe estar en Build Settings).")]
    [SerializeField] private string sceneToLoadName;
    [Tooltip("El ID del PlayerSpawner en la ESCENA DE DESTINO donde se desea que aparezca el jugador.")]
    [SerializeField] private string targetEntryPointIDInNextScene;
    [Header("Opcional: Detección por Trigger")]
    [Tooltip("Si es true, la transición se activará cuando un objeto con el 'activatingTag' entre en el Collider2D Trigger de este GameObject.")]
    [SerializeField] private bool useTrigger = true;
    [Tooltip("El Tag del GameObject que puede activar la transición por trigger (normalmente 'Player').")]
    [SerializeField] private string activatingTag = "Player";

    /// <summary>
    /// Ejecuta la lógica para cambiar de escena.
    /// Establece el punto de entrada para el jugador en la siguiente escena y luego carga dicha escena.
    /// Puede ser llamado desde un evento de UI (ej. OnClick de un botón) o desde OnTriggerEnter2D.
    /// </summary>
    public void Transition()
    {
        // Valida que se haya especificado un nombre de escena a cargar.
        if (string.IsNullOrEmpty(sceneToLoadName))
        {
            Debug.LogError($"SceneTransitioner en {gameObject.name}: No se ha especificado 'sceneToLoadName'. No se puede cambiar de escena.", this);
            return;
        }
        // Advierte si no se especificó un punto de entrada, el jugador podría aparecer en una posición por defecto.
        if (string.IsNullOrEmpty(targetEntryPointIDInNextScene))
        {
            Debug.LogWarning($"SceneTransitioner en {gameObject.name}: No se ha especificado 'targetEntryPointIDInNextScene'. El jugador podría no aparecer en la ubicación deseada en la escena '{sceneToLoadName}'.", this);
        }
        // 1. Se usa el modelo de simulación PlayerSpawnManager: Para guardar el ID del punto de entrada.
        // Guarda el ID del punto de entrada deseado en la siguiente escena para que Player.cs lo lea.
        PlayerSpawnManager.entryPointID = targetEntryPointIDInNextScene;
        // 2. Carga la nueva escena utilizando SceneManager.
        SceneManager.LoadScene(sceneToLoadName);
    }

    /// <summary>
    /// Se llama automáticamente por Unity cuando otro Collider2D entra en el Trigger de este GameObject.
    /// Si 'useTrigger' es true y el objeto que colisiona tiene el 'activatingTag', inicia la transición.
    /// </summary>
    /// <param name="other">El Collider2D del objeto que entró en el trigger.</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (useTrigger && other.CompareTag(activatingTag))
        {
            Transition(); // Llama al método principal de transición.
        }
    }

    /// <summary>
    /// Se llama en el editor cuando el GameObject está seleccionado.
    /// Dibuja un Gizmo para visualizar el área del trigger si está configurado para usarse.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (useTrigger) // Solo dibujar si se usa el trigger.
        {
            Collider2D col = GetComponent<Collider2D>();
            // Solo dibujar si hay un Collider2D y está configurado como trigger.
            if (col != null && col.isTrigger)
            {
                Gizmos.color = new Color(0, 1, 0, 0.3f); // Color verde semitransparente para el Gizmo.
                Gizmos.matrix = transform.localToWorldMatrix; // Asegura que el Gizmo escale y rote con el objeto.
                // Dibuja la forma del Gizmo según el tipo de Collider2D.
                if (col is BoxCollider2D box)
                {
                    Gizmos.DrawCube(box.offset, box.size);
                }
                else if (col is CircleCollider2D circle)
                {
                    Gizmos.DrawSphere(circle.offset, circle.radius);
                }
                // Se podrían añadir más tipos de collider si fueran necesarios (ej. PolygonCollider2D).
            }
        }
    }
}