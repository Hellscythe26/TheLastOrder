// SceneTransitioner.cs
using UnityEngine;
using UnityEngine.SceneManagement; // ¡Importante para manejar escenas!

public class SceneTransitioner : MonoBehaviour
{
    [Header("Configuración de Transición")]
    [Tooltip("Nombre EXACTO del archivo de escena a cargar (ej: EscenaCueva)")]
    [SerializeField] private string sceneToLoadName;

    [Tooltip("El ID del PlayerSpawner en la ESCENA DE DESTINO donde aparecerá el jugador.")]
    [SerializeField] private string targetEntryPointIDInNextScene;

    // Puedes usar un collider para detectar al jugador
    [Header("Opcional: Detección por Trigger")]
    [Tooltip("Si es verdadero, se usará OnTriggerEnter2D. Si no, necesitarás llamar a Transition() manualmente.")]
    [SerializeField] private bool useTrigger = true;
    [Tooltip("Tag del objeto que puede activar la transición (normalmente 'Player')")]
    [SerializeField] private string activatingTag = "Player";

    // Método público para llamar desde un botón de UI u otro script si no usas trigger
    public void Transition()
    {
        if (string.IsNullOrEmpty(sceneToLoadName))
        {
            Debug.LogError($"SceneTransitioner en {gameObject.name}: No se ha especificado sceneToLoadName.", this);
            return;
        }
        if (string.IsNullOrEmpty(targetEntryPointIDInNextScene))
        {
            Debug.LogWarning($"SceneTransitioner en {gameObject.name}: No se ha especificado targetEntryPointIDInNextScene. El jugador podría aparecer en una posición por defecto en la siguiente escena.", this);
        }

        Debug.Log($"Transicionando a escena: {sceneToLoadName}. Punto de entrada destino: {targetEntryPointIDInNextScene}");

        // 1. Guardar a dónde debe ir el jugador en la siguiente escena
        PlayerSpawnManager.entryPointID = targetEntryPointIDInNextScene;

        // 2. Cargar la nueva escena
        SceneManager.LoadScene(sceneToLoadName);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (useTrigger && other.CompareTag(activatingTag))
        {
            Transition();
        }
    }

    // Opcional: Dibuja un Gizmo para ver el área de trigger si lo usas
    private void OnDrawGizmos()
    {
        if (useTrigger)
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null && col.isTrigger)
            {
                Gizmos.color = new Color(0, 1, 0, 0.3f); // Verde semitransparente
                Gizmos.matrix = transform.localToWorldMatrix; // Para que el gizmo rote/escale con el objeto
                if (col is BoxCollider2D box)
                {
                    Gizmos.DrawCube(box.offset, box.size);
                }
                else if (col is CircleCollider2D circle)
                {
                    Gizmos.DrawSphere(circle.offset, circle.radius);
                }
                // Añadir más tipos de collider si es necesario
            }
        }
    }
}