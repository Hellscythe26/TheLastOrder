// EnemyMovement.cs
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))] // Primer componente requerido
[RequireComponent(typeof(EnemyHealth))]  // Segundo componente requerido
[RequireComponent(typeof(Animator))]    // Tercer componente requerido
[RequireComponent(typeof(EnemyCombat))]  // Cuarto componente requerido
public class EnemyMovement : MonoBehaviour
{
    [Header("Movement Stats")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] public float detectionRadius = 8f;

    [Header("Random Walk Config")]
    [SerializeField] private float stepDuration = 1.5f;
    [SerializeField] private float randomWalkSpeed = 1.5f;

    [Header("LCG Parameters")]
    [SerializeField] private long baseSeedForLCG = 12345;
    [SerializeField] private long lcgMultiplier = 1103515245;
    [SerializeField] private long lcgIncrement = 12345;
    [SerializeField] private long lcgModulus = 2147483648;

    [Header("LCG Test Parameters")]
    [SerializeField] private int numSamplesToGenerate = 100;
    [SerializeField] private double lcgAlphaTestLevel = 0.05;

    [Header("Component References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private EnemyHealth health;
    [SerializeField] private Animator animator;
    [SerializeField] private EnemyCombat combatSystem; // Referencia al sistema de combate

    private bool canMove = true;
    private bool internalPlayerDetectedState = false;
    private Vector2 currentMoveDirection = Vector2.zero;
    private Vector2 lastFacingDirection = Vector2.down;
    private bool wasDetectingLastFrame = false;

    private LCGManager lcgManager;
    private RandomWalk randomWalker;

    private Agents agentActions; // <--- CAMBIO DE NOMBRE DE CLASE AQUÍ

    private const string MOVE_X_PARAM = "MoveX";
    private const string MOVE_Y_PARAM = "MoveY";

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (health == null) health = GetComponent<EnemyHealth>();
        if (animator == null) animator = GetComponent<Animator>();
        if (combatSystem == null) combatSystem = GetComponent<EnemyCombat>();

        if (animator == null) Debug.LogError("Animator component missing on " + gameObject.name + "!", this);
        if (rb == null) Debug.LogError("Rigidbody2D component missing on " + gameObject.name + "!", this);
        if (health == null) Debug.LogError("EnemyHealth component missing on " + gameObject.name + "!", this);
        if (combatSystem == null) Debug.LogError("EnemyCombat component missing on " + gameObject.name + "! Asegúrate de que esté en el mismo GameObject.", this);

        long instanceSeed = System.DateTime.Now.Ticks + gameObject.GetInstanceID() + baseSeedForLCG;
        lcgManager = new LCGManager(instanceSeed, lcgMultiplier, lcgIncrement, lcgModulus, lcgAlphaTestLevel);
        List<float> riValues = lcgManager.GetValidatedRiNumbers(numSamplesToGenerate, out bool generationSucceeded);

        if (generationSucceeded && riValues != null && riValues.Count > 0)
        {
            randomWalker = new RandomWalk(riValues, stepDuration);
        }
        else
        {
            randomWalker = new RandomWalk(new List<float>(), stepDuration); // Evitar null
            Debug.LogError($"Enemy {gameObject.name}: Falló inicialización de RandomWalk.", this);
        }

        agentActions = new Agents(); // <--- CAMBIO DE NOMBRE DE CLASE AQUÍ
    }

    void FixedUpdate()
    {
        bool isCurrentlyAlive = health.IsAlive();
        if (!canMove || !isCurrentlyAlive)
        {
            StopMovementAndAnimation();
            CheckAndNotifyMusicManager(false);
            return;
        }

        Transform currentPlayerTransform = null;
        IDamageable currentPlayerIDamageable = null;
        bool playerSingletonExists = Player.Instance != null;

        if (playerSingletonExists)
        {
            currentPlayerTransform = Player.Instance.transform;
            currentPlayerIDamageable = Player.Instance.GetComponent<IDamageable>();
        }

        // 1. Observación
        DetectPlayer(currentPlayerTransform, currentPlayerIDamageable);

        bool playerInAttackRange = false;
        bool attackIsReady = false;
        if (combatSystem != null && currentPlayerTransform != null)
        {
            playerInAttackRange = combatSystem.IsPlayerInAttackRange(currentPlayerTransform);
            attackIsReady = combatSystem.IsAttackOffCooldown();
        }

        agentActions.UpdateObservations(
            internalPlayerDetectedState,
            playerInAttackRange,
            attackIsReady,
            isCurrentlyAlive
        );

        // 2. Decisión
        Agents.Action nextAction = agentActions.DecideNextAction(); // <--- CAMBIO DE NOMBRE DE CLASE AQUÍ

        // 3. Actuación
        switch (nextAction)
        {
            case Agents.Action.ChasePlayer: // <--- CAMBIO DE NOMBRE DE CLASE AQUÍ
                if (currentPlayerTransform != null)
                {
                    Vector3 playerPosOnEnemyPlane = new Vector3(currentPlayerTransform.position.x, currentPlayerTransform.position.y, transform.position.z);
                    Vector3 enemyPos = transform.position;
                    Vector2 vectorToPlayer2D = playerPosOnEnemyPlane - enemyPos;

                    if (vectorToPlayer2D.magnitude > 0.01f)
                        currentMoveDirection = vectorToPlayer2D.normalized;
                    else
                        currentMoveDirection = Vector2.zero;

                    if (currentMoveDirection != Vector2.zero)
                        MoveEnemy(currentMoveDirection, moveSpeed);
                    else
                        StopRigidbody();
                }
                else
                {
                    currentMoveDirection = Vector2.zero;
                    StopRigidbody();
                }
                break;

            case Agents.Action.AttackPlayer: // <--- CAMBIO DE NOMBRE DE CLASE AQUÍ
                StopRigidbody();
                if (currentPlayerTransform != null && combatSystem != null)
                {
                    currentMoveDirection = (currentPlayerTransform.position - transform.position).normalized;
                    combatSystem.TryPerformAttack(currentPlayerTransform, currentPlayerIDamageable);
                } else {
                    currentMoveDirection = lastFacingDirection;
                }
                break;

            case Agents.Action.Idle_Or_RandomWalk: // <--- CAMBIO DE NOMBRE DE CLASE AQUÍ
            default:
                if (randomWalker != null && randomWalker.IsInitialized())
                {
                    currentMoveDirection = randomWalker.UpdateWalk(Time.fixedDeltaTime);
                    if (currentMoveDirection != Vector2.zero)
                        MoveEnemy(currentMoveDirection, randomWalkSpeed);
                    else
                        StopRigidbody();
                }
                else
                {
                    currentMoveDirection = Vector2.zero;
                    StopRigidbody();
                }
                break;
        }
        UpdateAnimatorParameters(currentMoveDirection);
        CheckAndNotifyMusicManager(internalPlayerDetectedState);
    }

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

    void MoveEnemy(Vector2 direction, float speed)
    {
        rb.MovePosition(rb.position + direction * speed * Time.fixedDeltaTime);
        if (direction.magnitude > 0.1f)
        {
            lastFacingDirection = direction.normalized;
        }
    }

    void StopRigidbody()
    {
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    void StopMovementAndAnimation()
    {
        StopRigidbody();
        UpdateAnimatorParameters(Vector2.zero);
    }

    void UpdateAnimatorParameters(Vector2 direction)
    {
        if (animator == null) return;
        Vector2 animDirToSet;
        bool isCurrentlyMoving = direction.magnitude > 0.01f;

        if (isCurrentlyMoving)
        {
            animDirToSet = direction.normalized;
        }
        else
        {
            animDirToSet = lastFacingDirection;
        }
        animator.SetFloat(MOVE_X_PARAM, animDirToSet.x);
        animator.SetFloat(MOVE_Y_PARAM, animDirToSet.y);
    }

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

    public void StopMovement()
    {
        bool wasDetectingBeforeStop = internalPlayerDetectedState || wasDetectingLastFrame;
        internalPlayerDetectedState = false;
        wasDetectingLastFrame = false;
        canMove = false;
        StopMovementAndAnimation();
        if (wasDetectingBeforeStop && BattleMusicManager.Instance != null)
        {
            BattleMusicManager.Instance.ReleaseBattleMusic(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (wasDetectingLastFrame && BattleMusicManager.Instance != null)
        {
            BattleMusicManager.Instance.ReleaseBattleMusic(gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}