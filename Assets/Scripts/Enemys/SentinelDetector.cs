// SentinelDetector.cs
using UnityEngine;

[RequireComponent(typeof(Animator))] // Opcional: Si quieres controlar la animación desde aquí
public class SentinelDetector : MonoBehaviour
{
    [Header("Detección")]
    [SerializeField] private float roomTriggerRadius = 8f;
    [SerializeField] private LayerMask playerLayer; // Para optimizar la detección

    [Header("Activación")]
    [Tooltip("Arrastra aquí el RoomController que este centinela debe activar.")]
    [SerializeField] private RoomController roomToActivate;

    [Header("Componentes (Opcional)")]
    [SerializeField] private Animator animator; // Si necesitas controlar animaciones (ej. alerta)

    // Estado Interno
    private Transform playerTransform;
    private bool playerDetected = false;
    private bool hasTriggeredRoom = false;

    // Constante para animación (Ejemplo)
    // private const string ALERT_TRIGGER = "PlayerDetectedAlert";

    private void Awake()
    {
        // Si no se asigna en el Inspector, intenta obtenerlo
        if (animator == null) animator = GetComponent<Animator>();

        // Intentar encontrar al jugador al inicio (puede fallar si el jugador se instancia después)
        FindPlayer();
        hasTriggeredRoom = false;
    }

    private void Update()
    {
        // Si ya activó la sala, no necesita hacer más nada
        if (hasTriggeredRoom) return;

        // Si no encontró al jugador en Awake, intentar de nuevo
        if (playerTransform == null)
        {
            FindPlayer();
            // Si sigue sin encontrarlo, salir de Update por ahora
            if (playerTransform == null) return;
        }

        // Comprobar si el RoomController está asignado
        if (roomToActivate == null)
        {
            // Desactivar este script si no tiene sala que activar para ahorrar rendimiento
            Debug.LogWarning($"Centinela ({gameObject.name}) no tiene RoomController asignado. Desactivando detector.", this);
            enabled = false; // Desactiva el método Update de este script
            return;
        }

        // Realizar la detección (más eficiente que buscar por Tag cada frame)
        DetectPlayer();

        // Lógica de activación
        if (playerDetected && !hasTriggeredRoom)
        {
            Debug.Log($"¡CENTINELA ({gameObject.name}) detectó al jugador! Activando RoomController: {roomToActivate.gameObject.name}");

            // Opcional: Disparar animación de alerta en el centinela
            // if (animator != null) animator.SetTrigger(ALERT_TRIGGER);

            // Llamar al método público del RoomController
            roomToActivate.StartEncounter();
            hasTriggeredRoom = true; // Marcar como activado

            // Opcional: Desactivar este script después de activar la sala si ya no hace nada más
            // enabled = false;
        }
    }

    void FindPlayer()
    {
        // Busca el objeto jugador por Tag. Considera alternativas si tienes muchos objetos o el tag cambia.
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
            Debug.Log($"Centinela ({gameObject.name}) encontró al jugador.", this);
        }
        // else { Debug.Log($"Centinela ({gameObject.name}) no encontró al jugador en FindPlayer().", this); }
    }

    void DetectPlayer()
    {
        // Usar OverlapCircle para una detección eficiente basada en LayerMask
        Collider2D playerCollider = Physics2D.OverlapCircle(transform.position, roomTriggerRadius, playerLayer);

        if (playerCollider != null)
        {
            // Asegurarse de que el jugador (o lo que sea que detectó) está "vivo" si es relevante
            // Podrías necesitar obtener un componente PlayerHealth aquí si quieres esa comprobación.
            // Ejemplo: PlayerHealth playerHealth = playerCollider.GetComponent<PlayerHealth>();
            // if (playerHealth != null && playerHealth.IsAlive()) { ... }

            playerDetected = true; // Detectado y (opcionalmente) vivo
        }
        else
        {
            playerDetected = false;
        }
    }

    // Método para resetear si la sala se reinicia (llamado externamente)
    public void ResetSentinelTrigger()
    {
        hasTriggeredRoom = false;
        playerDetected = false; // Resetear detección también
        // enabled = true; // Reactivar si se desactivó en Update
        Debug.Log($"Centinela ({gameObject.name}) reseteado.");
    }

    // Dibujar Gizmo para visualizar el radio en el editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, roomTriggerRadius);
    }
}