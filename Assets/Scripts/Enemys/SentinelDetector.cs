// SentinelDetector.cs (Modificado para manejar también el ataque)
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(EnemyCombat))] // Asegurar que tiene el sistema de combate
[RequireComponent(typeof(EnemyHealth))] // Asumiendo que el centinela puede morir
public class SentinelDetector : MonoBehaviour
{
    [Header("Detección de Sala")]
    [SerializeField] private float roomTriggerRadius = 10f; // Renombrado para claridad
    [SerializeField] private LayerMask playerLayer;
    [Tooltip("Arrastra aquí el RoomController que este centinela debe activar.")]
    [SerializeField] private RoomController roomToActivate;

    [Header("Combate del Centinela (Referencias)")]
    [SerializeField] private EnemyCombat combatSystem;
    [SerializeField] private EnemyHealth healthSystem;
    [SerializeField] private Animator animator; // Ya lo tenías

    // Estado interno
    private Transform playerTransform; // Referencia al transform del jugador
    private IDamageable playerDamageable; // Para atacar
    private bool playerDetectedForRoomTrigger = false; // Para la activación de la sala
    private bool hasTriggeredRoom = false;

    private Agents agentActions; // Usaremos la misma clase Agents para la lógica de decisión

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (combatSystem == null) combatSystem = GetComponent<EnemyCombat>();
        if (healthSystem == null) healthSystem = GetComponent<EnemyHealth>();

        if (combatSystem == null) Debug.LogError("SentinelDetector: EnemyCombat no asignado o no encontrado!", this);
        if (healthSystem == null) Debug.LogError("SentinelDetector: EnemyHealth no asignado o no encontrado!", this);

        // No necesitamos FindPlayer() aquí si lo hacemos en Update o al detectar
        hasTriggeredRoom = false;
        agentActions = new Agents(); // Crear la instancia del agente
    }

    void Update()
    {
        // Obtener referencias al jugador
        if (Player.Instance != null)
        {
            playerTransform = Player.Instance.transform;
            playerDamageable = Player.Instance.GetComponent<IDamageable>();
        }
        else
        {
            playerTransform = null;
            playerDamageable = null;
            // Si no hay jugador, el centinela no hace nada de detección o ataque
            return;
        }

        // ----- Lógica de Activación de Sala (como antes) -----
        if (!hasTriggeredRoom && roomToActivate != null)
        {
            // Detectar para el trigger de la sala
            DetectPlayerForRoom();
            if (playerDetectedForRoomTrigger)
            {
                Debug.Log($"¡CENTINELA ({gameObject.name}) detectó jugador para SALA! Activando RoomController: {roomToActivate.gameObject.name}");
                roomToActivate.StartEncounter();
                hasTriggeredRoom = true;
            }
        }

        // ----- Lógica de Combate del Centinela (NUEVO) -----
        if (combatSystem != null && healthSystem != null && healthSystem.IsAlive())
        {
            // 1. Observación para el combate
            bool playerInAttackRange = combatSystem.IsPlayerInAttackRange(playerTransform);
            bool attackIsReady = combatSystem.IsAttackOffCooldown();
            // Para el centinela, la "detección" para combate puede ser simplemente si el jugador está en rango de ataque
            // o puedes usar un radio de "alerta" diferente si quieres que mire al jugador antes de atacar.
            // Por simplicidad, asumamos que si está en rango de ataque, está "detectado" para combate.
            bool playerDetectedForCombat = playerInAttackRange;


            agentActions.UpdateObservations(
                playerDetectedForCombat, // Si está en rango de ataque, lo consideramos detectado para combatir
                playerInAttackRange,
                attackIsReady,
                healthSystem.IsAlive()
            );

            // 2. Decisión
            Agents.Action nextCombatAction = agentActions.DecideNextAction();

            // 3. Actuación (solo nos interesa la acción de ataque para el centinela)
            if (nextCombatAction == Agents.Action.AttackPlayer)
            {
                // El centinela no se mueve, así que no hay acción de "ChasePlayer" relevante aquí.
                // Orientar la animación hacia el jugador (si es necesario y tienes la lógica)
                if (playerTransform != null)
                {
                    Vector2 directionToPlayer = (playerTransform.position - transform.position).normalized;
                    // Actualizar parámetros del Animator si el centinela debe mirar antes de atacar
                    // animator.SetFloat("MoveX", directionToPlayer.x);
                    // animator.SetFloat("MoveY", directionToPlayer.y);
                }
                combatSystem.TryPerformAttack(playerTransform, playerDamageable);
            }
            // Si la acción es Idle_Or_RandomWalk, el centinela simplemente permanece en su animación Idle.
            // Si la acción es ChasePlayer, el centinela (al ser estático) no hará nada.
        }
    }

    void DetectPlayerForRoom() // Detección para activar la sala
    {
        playerDetectedForRoomTrigger = false;
        if (playerTransform != null && playerDamageable != null && playerDamageable.IsAlive())
        {
             // Usamos OverlapCircle para el radio de activación de la sala
             Collider2D playerCollider = Physics2D.OverlapCircle(transform.position, roomTriggerRadius, playerLayer);
             if (playerCollider != null && playerCollider.CompareTag("Player")) // Asegurarse que es el jugador
             {
                playerDetectedForRoomTrigger = true;
             }
        }
    }

    public void ResetSentinelTrigger() // Para la activación de la sala
    {
        hasTriggeredRoom = false;
        playerDetectedForRoomTrigger = false;
        // Debug.Log($"Centinela ({gameObject.name}) reseteado para activación de sala.");
    }

    private void OnDrawGizmosSelected()
    {
        // Gizmo para el radio de activación de la sala
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, roomTriggerRadius);

        // Gizmo para el radio de ataque (si EnemyCombat está presente y tiene un attackRange)
        if (combatSystem != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, combatSystem.attackRange);
        }
    }
}