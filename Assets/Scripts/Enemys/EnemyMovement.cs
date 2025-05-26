// EnemyMovement.cs
using UnityEngine;
using System.Collections.Generic; // Necesario para List<float>

[RequireComponent(typeof(Rigidbody2D), typeof(EnemyHealth), typeof(Animator))]
public class EnemyMovement : MonoBehaviour
{
    [Header("Movement Stats")]
    [SerializeField] private float moveSpeed = 2f;         // Velocidad al perseguir
    [SerializeField] private float detectionRadius = 8f;   // Radio para detectar al jugador
    [Header("Random Walk Config")]
    [SerializeField] private float stepDuration = 1.5f;    // Cuánto dura cada "paso" en una dirección aleatoria
    [SerializeField] private float randomWalkSpeed = 1.5f; // Velocidad durante la caminata aleatoria
    [Header("LCG Parameters (Configurable per enemy type)")]
    [Tooltip("Semilla base, se aleatorizará por instancia para más variedad.")]
    [SerializeField] private long baseSeedForLCG = 12345;
    [SerializeField] private long lcgMultiplier = 1103515245;
    [SerializeField] private long lcgIncrement = 12345;
    [SerializeField] private long lcgModulus = 2147483648;
    [Header("LCG Test Parameters (Configurable per enemy type)")]
    [Tooltip("Cuántos números Ri generar y probar con LCGManager.")]
    [SerializeField] private int numSamplesToGenerate = 100; // Parámetro que querías en el Inspector
    [Tooltip("Nivel Alpha para las pruebas estadísticas (ej: 0.05).")]
    [SerializeField] private double lcgAlphaTestLevel = 0.05;
    [Header("Component References (Asignar en Inspector o se buscarán)")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private EnemyHealth health;
    [SerializeField] private Animator animator;
    private bool canMove = true;
    private bool playerDetected = false;
    private Vector2 currentMoveDirection = Vector2.zero;
    private Vector2 lastFacingDirection = Vector2.down; // Para mantener la dirección en idle
    private bool wasDetectingLastFrame = false;
    private LCGManager lcgManager;
    private RandomWalk randomWalker;
    private const string MOVE_X_PARAM = "MoveX";
    private const string MOVE_Y_PARAM = "MoveY";

    void Awake()
    {
        // Obtener componentes si no están asignados en el Inspector
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (health == null) health = GetComponent<EnemyHealth>();
        if (animator == null) animator = GetComponent<Animator>();

        if (animator == null) Debug.LogError("Animator component missing on " + gameObject.name + "!", this);
        if (rb == null) Debug.LogError("Rigidbody2D component missing on " + gameObject.name + "!", this);
        if (health == null) Debug.LogError("EnemyHealth component missing on " + gameObject.name + "!", this);

        // Aleatorizar la semilla para esta instancia específica del enemigo
        long instanceSeed = System.DateTime.Now.Ticks + gameObject.GetInstanceID() + baseSeedForLCG;

        // 1. Inicializar LCGManager
        lcgManager = new LCGManager(
            instanceSeed,
            lcgMultiplier,
            lcgIncrement,
            lcgModulus,
            lcgAlphaTestLevel
        );

        // 2. Obtener los números Ri validados
        List<float> riValues = lcgManager.GetValidatedRiNumbers(
            numSamplesToGenerate, // <-- El parámetro que querías configurar en Inspector
            out bool generationSucceeded
        );

        // 3. Inicializar RandomWalker con los números obtenidos
        if (generationSucceeded && riValues != null && riValues.Count > 0)
        {
            randomWalker = new RandomWalk(riValues, stepDuration);
            // Debug.Log($"Enemy {gameObject.name}: Random Walk initialized successfully with {riValues.Count} numbers.", this);
        }
        else
        {
            randomWalker = new RandomWalk(new List<float>(), stepDuration); // Inicializar con lista vacía para evitar nulls
            Debug.LogError($"Enemy {gameObject.name}: Failed to initialize Random Walk. LCG numbers not generated or invalid.", this);
        }
    }

    void FixedUpdate()
    {
        if (!canMove || !health.IsAlive())
        {
            StopMovementAndAnimation();
            CheckAndNotifyMusicManager(false);
            return;
        }

        Transform currentPlayerTransform = null;
        IDamageable currentPlayerIDamageable = null;
        if (Player.Instance != null)
        {
            currentPlayerTransform = Player.Instance.transform;
            currentPlayerIDamageable = Player.Instance.GetComponent<IDamageable>();
        }

        DetectPlayer(currentPlayerTransform, currentPlayerIDamageable);

        if (playerDetected && currentPlayerTransform != null)
        {
            // ESTADO: Persecución
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
            // ESTADO: Caminata Aleatoria
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
                // Si la caminata aleatoria no se inicializó o falló, quedarse quieto
                currentMoveDirection = Vector2.zero;
                StopRigidbody();
            }
        }
        UpdateAnimatorParameters(currentMoveDirection);
        CheckAndNotifyMusicManager(playerDetected);
    }

    void DetectPlayer(Transform targetPlayerTransform, IDamageable targetPlayerDamageable)
    {
        playerDetected = false;
        if (targetPlayerTransform != null && targetPlayerDamageable != null && targetPlayerDamageable.IsAlive())
        {
            Vector2 enemyPosition2D = new Vector2(transform.position.x, transform.position.y);
            Vector2 playerPosition2D = new Vector2(targetPlayerTransform.position.x, targetPlayerTransform.position.y);
            float distanceToPlayer = Vector2.Distance(enemyPosition2D, playerPosition2D);
            playerDetected = (distanceToPlayer <= detectionRadius);
        }
    }

    void MoveEnemy(Vector2 direction, float speed)
    {
        rb.MovePosition(rb.position + direction * speed * Time.fixedDeltaTime);
        if (direction.magnitude > 0.1f) // Solo actualizar si hay movimiento real
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
        UpdateAnimatorParameters(Vector2.zero); // Pone la animación en Idle usando la última dirección
    }

    void UpdateAnimatorParameters(Vector2 direction)
    {
        if (animator == null) return;

        Vector2 animDirToSet;
        bool isCurrentlyMoving = direction.magnitude > 0.01f; // Un pequeño umbral para considerar "movimiento"

        if (isCurrentlyMoving)
        {
            animDirToSet = direction.normalized;
            // lastFacingDirection se actualiza en MoveEnemy, así que no es necesario aquí si MoveEnemy se llama
        }
        else
        {
            animDirToSet = lastFacingDirection; // Usar la última dirección en la que se movió para Idle
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

    public void StopMovement() // Llamado por EnemyHealth.Die()
    {
        bool wasDetectingBeforeStop = playerDetected || wasDetectingLastFrame; // Capturar estado antes de cambiar
        playerDetected = false; // Actualizar estado de detección
        wasDetectingLastFrame = false; // Sincronizar con playerDetected
        canMove = false;
        StopMovementAndAnimation();
        if (wasDetectingBeforeStop && BattleMusicManager.Instance != null)
        {
            BattleMusicManager.Instance.ReleaseBattleMusic(gameObject);
        }
    }

    private void OnDestroy()
    {
        // Asegurarse de liberar la música si el enemigo se destruye mientras detectaba
        if (wasDetectingLastFrame && BattleMusicManager.Instance != null)
        {
            BattleMusicManager.Instance.ReleaseBattleMusic(gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; // Color del Gizmo para el radio de detección
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}