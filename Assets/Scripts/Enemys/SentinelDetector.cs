using UnityEngine;

/// <summary>
/// Controla el comportamiento de un enemigo centinela estático.
/// Es responsable de detectar al jugador para activar un RoomController
/// y también de gestionar la lógica de combate del propio centinela utilizando un Agente.
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(EnemyCombat))]
[RequireComponent(typeof(EnemyHealth))]
public class SentinelDetector : MonoBehaviour
{
    [Header("Detección de Sala")]
    [Tooltip("Radio grande para detectar al jugador y activar el RoomController.")]
    [SerializeField] private float roomTriggerRadius = 10f;
    [Tooltip("LayerMask para filtrar la detección del jugador.")]
    [SerializeField] private LayerMask playerLayer;
    [Tooltip("Referencia al RoomController que este centinela debe activar.")]
    [SerializeField] private RoomController roomToActivate;
    [Header("Combate del Centinela (Referencias)")]
    [Tooltip("Referencia al sistema de combate del centinela.")]
    [SerializeField] private EnemyCombat combatSystem;
    [Tooltip("Referencia al sistema de vida del centinela.")]
    [SerializeField] private EnemyHealth healthSystem;
    [Tooltip("Referencia al Animator del centinela.")]
    [SerializeField] private Animator animator;
    // Estado interno
    private Transform playerTransform; // Referencia al transform del jugador.
    private IDamageable playerDamageable; // Interfaz para dañar al jugador.
    private bool playerDetectedForRoomTrigger = false; // Indica si el jugador fue detectado para activar la sala.
    private bool hasTriggeredRoom = false; // Indica si la sala ya fue activada por este centinela.
    // Se usa el modelo de simulación Agents.
    private Agents agentActions; // Instancia del agente para la toma de decisiones de combate.

    /// <summary>
    /// Se llama una vez cuando el script es cargado o un GameObject con el script es instanciado.
    /// Inicializa referencias a componentes y crea la instancia del agente de decisión.
    /// </summary>
    void Awake()
    {
        // Obtiene componentes si no están asignados en el Inspector.
        if (animator == null) animator = GetComponent<Animator>();
        if (combatSystem == null) combatSystem = GetComponent<EnemyCombat>();
        if (healthSystem == null) healthSystem = GetComponent<EnemyHealth>();
        // Logs de error si faltan componentes cruciales.
        if (combatSystem == null) Debug.LogError("SentinelDetector: EnemyCombat no asignado o no encontrado en " + gameObject.name + "!", this);
        if (healthSystem == null) Debug.LogError("SentinelDetector: EnemyHealth no asignado o no encontrado en " + gameObject.name + "!", this);
        hasTriggeredRoom = false; // Asegura que el estado inicial sea no haber activado la sala.
        // Se usa el modelo de simulación Agents: Creación de la instancia del agente.
        agentActions = new Agents();
    }

    /// <summary>
    /// Se llama una vez por frame.
    /// Gestiona la obtención de referencias al jugador, la activación de la sala y la lógica de combate del centinela.
    /// </summary>
    void Update()
    {
        // Obtiene referencias actualizadas al jugador (Singleton).
        if (Player.Instance != null)
        {
            playerTransform = Player.Instance.transform;
            playerDamageable = Player.Instance.GetComponent<IDamageable>(); // PlayerHealth implementa IDamageable
        }
        else
        {
            // Si no hay jugador, el centinela no puede realizar acciones dependientes de él.
            playerTransform = null;
            playerDamageable = null;
            return;
        }
        // Lógica para activar el RoomController (solo una vez).
        if (!hasTriggeredRoom && roomToActivate != null)
        {
            DetectPlayerForRoom(); // Realiza la detección en el radio grande.
            if (playerDetectedForRoomTrigger)
            {
                // Debug.Log($"¡CENTINELA ({gameObject.name}) detectó jugador para SALA! Activando RoomController: {roomToActivate.gameObject.name}");
                roomToActivate.StartEncounter(); // Activa el encuentro en la sala.
                hasTriggeredRoom = true; // Marca la sala como activada.
            }
        }
        // Lógica de combate del centinela si está vivo y tiene sistema de combate.
        if (combatSystem != null && healthSystem != null && healthSystem.IsAlive())
        {
            // 1. Observación del entorno para el combate.
            bool playerInAttackRange = combatSystem.IsPlayerInAttackRange(playerTransform);
            bool attackIsReady = combatSystem.IsAttackOffCooldown();
            // Para el centinela, se considera "detectado para combate" si está en rango de ataque.
            bool playerDetectedForCombat = playerInAttackRange;
            // Se usa el modelo de simulación Agents: Actualización de observaciones.
            agentActions.UpdateObservations(
                playerDetectedForCombat,
                playerInAttackRange,
                attackIsReady,
                healthSystem.IsAlive()
            );
            // 2. Decisión del agente.
            // Se usa el modelo de simulación Agents: Obtención de la siguiente acción.
            Agents.Action nextCombatAction = agentActions.DecideNextAction();
            // 3. Actuación (solo la acción de ataque es relevante para el centinela estático).
            if (nextCombatAction == Agents.Action.AttackPlayer)
            {
                // Podría orientar la animación hacia el jugador aquí si fuera necesario.
                // if (playerTransform != null && animator != null)
                // {
                //     Vector2 directionToPlayer = (playerTransform.position - transform.position).normalized;
                //     animator.SetFloat("MoveX", directionToPlayer.x); // Asumiendo parámetros de animación
                //     animator.SetFloat("MoveY", directionToPlayer.y);
                // }
                combatSystem.TryPerformAttack(playerTransform, playerDamageable); // Intenta ejecutar el ataque.
            }
            // Las acciones ChasePlayer o Idle_Or_RandomWalk no implican movimiento para el centinela.
        }
    }

    /// <summary>
    /// Realiza la detección del jugador dentro del 'roomTriggerRadius' para activar la sala.
    /// Actualiza la variable 'playerDetectedForRoomTrigger'.
    /// </summary>
    void DetectPlayerForRoom()
    {
        playerDetectedForRoomTrigger = false;
        if (playerTransform != null && playerDamageable != null && playerDamageable.IsAlive())
        {
             // Comprueba si algún collider en la 'playerLayer' está dentro del círculo de detección.
             Collider2D playerCollider = Physics2D.OverlapCircle(transform.position, roomTriggerRadius, playerLayer);
             if (playerCollider != null && playerCollider.CompareTag("Player")) // Confirma que el objeto detectado es el jugador.
             {
                playerDetectedForRoomTrigger = true;
             }
        }
    }

    /// <summary>
    /// Resetea el estado de activación de la sala para este centinela.
    /// Podría ser llamado si la sala se resetea y el centinela necesita poder activarla de nuevo.
    /// </summary>
    public void ResetSentinelTrigger()
    {
        hasTriggeredRoom = false;
        playerDetectedForRoomTrigger = false;
    }

    /// <summary>
    /// Se llama en el editor cuando el GameObject está seleccionado. Dibuja Gizmos para visualización.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Dibuja el radio de activación de la sala (rojo).
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, roomTriggerRadius);
        // Dibuja el radio de ataque del sistema de combate (azul), si está disponible.
        if (combatSystem != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, combatSystem.attackRange);
        }
    }
}