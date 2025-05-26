using UnityEngine;

[RequireComponent(typeof(Animator))]
public class SentinelDetector : MonoBehaviour
{
    [Header("Detección")]
    [SerializeField] private float roomTriggerRadius = 8f;
    [SerializeField] private LayerMask playerLayer; // Para optimizar la detección
    [Header("Activación")]
    [Tooltip("Arrastra aquí el RoomController que este centinela debe activar.")]
    [SerializeField] private RoomController roomToActivate;
    [Header("Componentes (Opcional)")]
    [SerializeField] private Animator animator;
    private Transform playerTransform;
    private bool playerDetected = false;
    private bool hasTriggeredRoom = false;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        FindPlayer();
        hasTriggeredRoom = false;
    }

    private void Update()
    {
        if (hasTriggeredRoom) return;
        if (playerTransform == null)
        {
            FindPlayer();
            if (playerTransform == null) return;
        }
        if (roomToActivate == null)
        {
            Debug.LogWarning($"Centinela ({gameObject.name}) no tiene RoomController asignado. Desactivando detector.", this);
            enabled = false;
            return;
        }
        DetectPlayer();
        if (playerDetected && !hasTriggeredRoom)
        {
            Debug.Log($"¡CENTINELA ({gameObject.name}) detectó al jugador! Activando RoomController: {roomToActivate.gameObject.name}");
            roomToActivate.StartEncounter();
            hasTriggeredRoom = true;
        }
    }

    void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
            Debug.Log($"Centinela ({gameObject.name}) encontró al jugador.", this);
        }
    }

    void DetectPlayer()
    {
        Collider2D playerCollider = Physics2D.OverlapCircle(transform.position, roomTriggerRadius, playerLayer);
        if (playerCollider != null)
        {
            playerDetected = true;
        }
        else
        {
            playerDetected = false;
        }
    }

    public void ResetSentinelTrigger()
    {
        hasTriggeredRoom = false;
        playerDetected = false;
        Debug.Log($"Centinela ({gameObject.name}) reseteado.");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, roomTriggerRadius);
    }
}