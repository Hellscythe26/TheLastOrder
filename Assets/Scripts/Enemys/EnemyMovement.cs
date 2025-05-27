using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gestiona el movimiento de un enemigo, incluyendo la detección del jugador,
/// la persecución y una caminata aleatoria cuando el jugador no está detectado.
/// Utiliza modelos de simulación LCGManager, RandomWalker y Agents.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyHealth))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(EnemyCombat))]
public class EnemyMovement : MonoBehaviour
{
    [Header("Movement Stats")]
    [Tooltip("Velocidad del enemigo al perseguir al jugador.")]
    [SerializeField] private float moveSpeed = 2f;
    [Tooltip("Radio dentro del cual el enemigo detecta al jugador.")]
    [SerializeField] public float detectionRadius = 8f;
    [Header("Random Walk Config")]
    [Tooltip("Duración en segundos que el enemigo mantiene una dirección en la caminata aleatoria.")]
    [SerializeField] private float stepDuration = 1.5f;
    [Tooltip("Velocidad del enemigo durante la caminata aleatoria.")]
    [SerializeField] private float randomWalkSpeed = 1.5f;
    [Header("LCG Parameters")]
    [Tooltip("Semilla base para el LCG, se combinará con valores dinámicos para cada instancia.")]
    [SerializeField] private long baseSeedForLCG = 12345;
    [SerializeField] private long lcgMultiplier = 1103515245;
    [SerializeField] private long lcgIncrement = 12345;
    [SerializeField] private long lcgModulus = 2147483648;
    [Header("LCG Test Parameters")]
    [Tooltip("Cantidad de números Ri a generar y probar con el LCGManager.")]
    [SerializeField] private int numSamplesToGenerate = 100;
    [Tooltip("Nivel Alpha para las pruebas estadísticas del LCG (ej: 0.05).")]
    [SerializeField] private double lcgAlphaTestLevel = 0.05;
    [Header("Component References")]
    [Tooltip("Referencia al Rigidbody2D del enemigo.")]
    [SerializeField] private Rigidbody2D rb;
    [Tooltip("Referencia al script EnemyHealth del enemigo.")]
    [SerializeField] private EnemyHealth health;
    [Tooltip("Referencia al Animator del enemigo.")]
    [SerializeField] private Animator animator;
    [Tooltip("Referencia al script EnemyCombat del enemigo.")]
    [SerializeField] private EnemyCombat combatSystem;
    // Estado interno del movimiento
    private bool canMove = true; // Indica si el enemigo tiene permitido moverse.
    private bool internalPlayerDetectedState = false; // Estado de detección del jugador interno a este script.
    private Vector2 currentMoveDirection = Vector2.zero; // Dirección actual de movimiento.
    private Vector2 lastFacingDirection = Vector2.down; // Última dirección en la que se movió o miró.
    private bool wasDetectingLastFrame = false; // Estado de detección en el frame anterior (para música).
    // Se usa el modelo de simulación LCGManager.
    private LCGManager lcgManager; // Instancia del generador de números LCG.
    // Se usa el modelo de simulación RandomWalker.
    private RandomWalk randomWalker; // Instancia que gestiona la lógica de caminata aleatoria.
    // Se usa el modelo de simulación Agents.
    private Agents agentActions; // Instancia del agente que toma decisiones de comportamiento.
    // Constantes para los parámetros del Animator.
    private const string MOVE_X_PARAM = "MoveX";
    private const string MOVE_Y_PARAM = "MoveY";

    /// <summary>
    /// Se llama una vez cuando el script es cargado.
    /// Inicializa componentes y los modelos de simulación LCGManager, RandomWalk y Agents.
    /// </summary>
    void Awake()
    {
        // Obtención de referencias a componentes.
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (health == null) health = GetComponent<EnemyHealth>();
        if (animator == null) animator = GetComponent<Animator>();
        if (combatSystem == null) combatSystem = GetComponent<EnemyCombat>();
        // Verificación de componentes esenciales.
        if (animator == null) Debug.LogError("Animator component missing on " + gameObject.name + "!", this);
        if (rb == null) Debug.LogError("Rigidbody2D component missing on " + gameObject.name + "!", this);
        if (health == null) Debug.LogError("EnemyHealth component missing on " + gameObject.name + "!", this);
        if (combatSystem == null) Debug.LogError("EnemyCombat component missing on " + gameObject.name + "!", this);
        // Se usa el generador de números LCGManager: Inicialización.
        long instanceSeed = System.DateTime.Now.Ticks + gameObject.GetInstanceID() + baseSeedForLCG;
        lcgManager = new LCGManager(instanceSeed, lcgMultiplier, lcgIncrement, lcgModulus, lcgAlphaTestLevel);
        List<float> riValues = lcgManager.GetValidatedRiNumbers(numSamplesToGenerate, out bool generationSucceeded);
        // Se usa el modelo de simulación RandomWalk: Inicialización.
        if (generationSucceeded && riValues != null && riValues.Count > 0)
        {
            randomWalker = new RandomWalk(riValues, stepDuration);
        }
        else
        {
            randomWalker = new RandomWalk(new List<float>(), stepDuration); // Fallback con lista vacía.
            Debug.LogError($"Enemy {gameObject.name}: Falló inicialización de RandomWalker debido a LCG.", this);
        }
        // Se usa el modelo de simulación Agents: Inicialización.
        agentActions = new Agents();
    }

    /// <summary>
    /// Se llama a intervalos de tiempo fijos, usado para la lógica de física y movimiento.
    /// Contiene el ciclo principal de Observación -> Decisión -> Actuación del enemigo.
    /// </summary>
    void FixedUpdate()
    {
        bool isCurrentlyAlive = health.IsAlive();
        // Detener acciones si el enemigo no puede moverse o no está vivo.
        if (!canMove || !isCurrentlyAlive)
        {
            StopMovementAndAnimation();
            CheckAndNotifyMusicManager(false); // Asegura que la música de batalla se detenga.
            return;
        }
        // Obtener referencias al jugador (Singleton).
        Transform currentPlayerTransform = null;
        IDamageable currentPlayerIDamageable = null;
        if (Player.Instance != null)
        {
            currentPlayerTransform = Player.Instance.transform;
            currentPlayerIDamageable = Player.Instance.GetComponent<IDamageable>();
        }
        // --- 1. OBSERVACIÓN ---
        // Actualiza el estado de detección del jugador.
        DetectPlayer(currentPlayerTransform, currentPlayerIDamageable);
        // Recopila información para el agente sobre el estado del combate.
        bool playerInAttackRange = false;
        bool attackIsReady = false;
        if (combatSystem != null && currentPlayerTransform != null)
        {
            playerInAttackRange = combatSystem.IsPlayerInAttackRange(currentPlayerTransform);
            attackIsReady = combatSystem.IsAttackOffCooldown();
        }
        // Se usa el modelo de simulación Agents: Actualiza las observaciones del agente.
        agentActions.UpdateObservations(
            internalPlayerDetectedState, // Resultado de DetectPlayer()
            playerInAttackRange,
            attackIsReady,
            isCurrentlyAlive
        );
        // --- 2. DECISIÓN ---
        // Se usa el modelo de simulación Agents: El agente decide la siguiente acción.
        Agents.Action nextAction = agentActions.DecideNextAction();
        // --- 3. ACTUACIÓN ---
        // Ejecuta la acción decidida por el agente.
        switch (nextAction)
        {
            case Agents.Action.ChasePlayer:
                if (currentPlayerTransform != null)
                {
                    // Calcula la dirección 2D hacia el jugador.
                    Vector3 playerPosOnEnemyPlane = new Vector3(currentPlayerTransform.position.x, currentPlayerTransform.position.y, transform.position.z);
                    Vector3 enemyPos = transform.position;
                    Vector2 vectorToPlayer2D = playerPosOnEnemyPlane - enemyPos;
                    // Normaliza la dirección si el vector no es casi cero.
                    if (vectorToPlayer2D.magnitude > 0.01f)
                        currentMoveDirection = vectorToPlayer2D.normalized;
                    else
                        currentMoveDirection = Vector2.zero; // Evita movimiento si están superpuestos.
                    // Mueve al enemigo o lo detiene.
                    if (currentMoveDirection != Vector2.zero)
                        MoveEnemy(currentMoveDirection, moveSpeed);
                    else
                        StopRigidbody();
                }
                else
                {
                    // Si no hay jugador, no hay persecución.
                    currentMoveDirection = Vector2.zero;
                    StopRigidbody();
                }
                break;
            case Agents.Action.AttackPlayer:
                StopRigidbody(); // El enemigo se detiene para atacar.
                if (currentPlayerTransform != null && combatSystem != null)
                {
                    // Actualiza la dirección para que el Animator mire al jugador.
                    currentMoveDirection = (currentPlayerTransform.position - transform.position).normalized;
                    // Delega la ejecución del ataque al sistema de combate.
                    combatSystem.TryPerformAttack(currentPlayerTransform, currentPlayerIDamageable);
                } else {
                    // Si no hay jugador, usa la última dirección conocida para la animación.
                    currentMoveDirection = lastFacingDirection;
                }
                break;
            case Agents.Action.Idle_Or_RandomWalk:
            default:
                // Se usa el modelo de simulación RandomWalk: Obtiene la dirección de caminata.
                if (randomWalker != null && randomWalker.IsInitialized())
                {
                    currentMoveDirection = randomWalker.UpdateWalk(Time.fixedDeltaTime);
                    if (currentMoveDirection != Vector2.zero)
                        MoveEnemy(currentMoveDirection, randomWalkSpeed);
                    else
                        StopRigidbody(); // Si RandomWalk devuelve cero (ej. al final de una secuencia corta).
                }
                else
                {
                    // Si RandomWalk no está listo, el enemigo se queda quieto.
                    currentMoveDirection = Vector2.zero;
                    StopRigidbody();
                }
                break;
        }
        // Actualiza la animación y la música de batalla.
        UpdateAnimatorParameters(currentMoveDirection);
        CheckAndNotifyMusicManager(internalPlayerDetectedState);
    }

    /// <summary>
    /// Detecta si el jugador está dentro del radio de detección y está vivo.
    /// Actualiza la variable 'internalPlayerDetectedState'.
    /// </summary>
    void DetectPlayer(Transform targetPlayerTransform, IDamageable targetPlayerDamageable)
    {
        internalPlayerDetectedState = false;
        if (targetPlayerTransform != null && targetPlayerDamageable != null && targetPlayerDamageable.IsAlive())
        {
            Vector2 enemyPosition2D = new Vector2(transform.position.x, transform.position.y);
            Vector2 playerPosition2D = new Vector2(targetPlayerTransform.position.x, targetPlayerTransform.position.y);
            float distanceToPlayer = Vector2.Distance(enemyPosition2D, playerPosition2D);
            internalPlayerDetectedState = (distanceToPlayer <= detectionRadius);
        }
    }

    /// <summary>
    /// Mueve el Rigidbody2D del enemigo en la dirección y velocidad especificadas.
    /// Actualiza 'lastFacingDirection'.
    /// </summary>
    void MoveEnemy(Vector2 direction, float speed)
    {
        rb.MovePosition(rb.position + direction * speed * Time.fixedDeltaTime);
        if (direction.magnitude > 0.1f) // Solo actualizar si hay movimiento significativo.
        {
            lastFacingDirection = direction.normalized;
        }
    }

    /// <summary>
    /// Detiene completamente el movimiento del Rigidbody2D.
    /// </summary>
    void StopRigidbody()
    {
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    /// <summary>
    /// Detiene el movimiento del Rigidbody y actualiza la animación a un estado de reposo (idle),
    /// usando la última dirección conocida para la orientación del sprite.
    /// </summary>
    void StopMovementAndAnimation()
    {
        StopRigidbody();
        UpdateAnimatorParameters(Vector2.zero); // Vector2.zero indica al Animator que use lastFacingDirection.
    }

    /// <summary>
    /// Actualiza los parámetros 'MoveX' y 'MoveY' del Animator basados en la dirección de movimiento.
    /// Si no hay movimiento, usa la 'lastFacingDirection' para la animación de idle.
    /// </summary>
    void UpdateAnimatorParameters(Vector2 direction)
    {
        if (animator == null) return;

        Vector2 animDirToSet;
        bool isCurrentlyMoving = direction.magnitude > 0.01f; // Umbral para considerar movimiento.

        if (isCurrentlyMoving)
        {
            animDirToSet = direction.normalized;
            // 'lastFacingDirection' se actualiza en MoveEnemy cuando hay movimiento.
        }
        else
        {
            animDirToSet = lastFacingDirection; // Usa la última dirección para la orientación en idle.
        }
        animator.SetFloat(MOVE_X_PARAM, animDirToSet.x);
        animator.SetFloat(MOVE_Y_PARAM, animDirToSet.y);
    }

    /// <summary>
    /// Comprueba si el estado de detección del jugador ha cambiado y notifica al BattleMusicManager.
    /// </summary>
    private void CheckAndNotifyMusicManager(bool isCurrentlyDetecting)
    {
        if (isCurrentlyDetecting && !wasDetectingLastFrame)
        {
            if (BattleMusicManager.Instance != null) BattleMusicManager.Instance.RequestBattleMusic(gameObject);
        }
        else if (!isCurrentlyDetecting && wasDetectingLastFrame)
        {
            if (BattleMusicManager.Instance != null) BattleMusicManager.Instance.ReleaseBattleMusic(gameObject);
        }
        wasDetectingLastFrame = isCurrentlyDetecting;
    }

    /// <summary>
    /// Método público para detener el movimiento del enemigo, usualmente llamado por EnemyHealth.Die().
    /// </summary>
    public void StopMovement()
    {
        bool wasDetectingBeforeStop = internalPlayerDetectedState || wasDetectingLastFrame;
        internalPlayerDetectedState = false;
        wasDetectingLastFrame = false;
        canMove = false; // Impide futuros intentos de movimiento en FixedUpdate.
        StopMovementAndAnimation();
        // Libera la música de batalla si el enemigo estaba contribuyendo a ella.
        if (wasDetectingBeforeStop && BattleMusicManager.Instance != null)
        {
            BattleMusicManager.Instance.ReleaseBattleMusic(gameObject);
        }
    }

    /// <summary>
    /// Se llama cuando el GameObject es destruido.
    /// Asegura liberar la solicitud de música de batalla si estaba activa.
    /// </summary>
    private void OnDestroy()
    {
        if (wasDetectingLastFrame && BattleMusicManager.Instance != null)
        {
            BattleMusicManager.Instance.ReleaseBattleMusic(gameObject);
        }
    }

    /// <summary>
    /// Dibuja Gizmos en el editor para visualizar el radio de detección.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}