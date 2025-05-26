// EnemyCombat.cs
using UnityEngine;

public class EnemyCombat : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float attackDamage = 1f;
    [SerializeField] public float attackRange = 1.5f; // Público para que EnemyMovement/Agents lo pueda consultar
    [SerializeField] private float attackCooldown = 2f;

    [Header("Component References")]
    [SerializeField] private EnemyHealth health;
    [SerializeField] private Animator animator;

    private float lastAttackTime = -Mathf.Infinity;

    private const string ATTACK_UP_TRIGGER = "AttackUp";
    private const string ATTACK_DOWN_TRIGGER = "AttackDown";
    private const string ATTACK_LEFT_TRIGGER = "AttackLeft";
    private const string ATTACK_RIGHT_TRIGGER = "AttackRight";

    private void Awake()
    {
        if (health == null) health = GetComponent<EnemyHealth>();
        if (animator == null) animator = GetComponent<Animator>();

        if (health == null) Debug.LogError("EnemyHealth component missing on " + gameObject.name + "!", this);
        if (animator == null) Debug.LogWarning("Animator component missing on " + gameObject.name + "!", this);
    }

    // El Update ya no toma decisiones proactivas de ataque.
    // Se confía en que EnemyMovement llamará a TryPerformAttack.
    // void Update() { }


    /// <summary>
    /// Intenta realizar un ataque si todas las condiciones se cumplen.
    /// Este método será llamado por el sistema del Agente (a través de EnemyMovement).
    /// </summary>
    public void TryPerformAttack(Transform targetPlayerTransform, IDamageable targetPlayerDamageable)
    {
        // Verificar condiciones de nuevo aquí es una buena práctica como doble chequeo,
        // especialmente si hay un pequeño delay entre la decisión del agente y la ejecución.
        if (CanAttackNow(targetPlayerTransform, targetPlayerDamageable))
        {
            ExecuteAttack(targetPlayerTransform, targetPlayerDamageable);
        }
    }

    /// <summary>
    /// Comprueba si el jugador está dentro del rango de ataque.
    /// Usado por EnemyMovement para actualizar las observaciones del Agente.
    /// </summary>
    public bool IsPlayerInAttackRange(Transform targetPlayerTransform)
    {
        if (targetPlayerTransform == null || health == null || !health.IsAlive())
        {
            return false;
        }
        return Vector2.Distance(transform.position, targetPlayerTransform.position) <= attackRange;
    }

    /// <summary>
    /// Comprueba si el cooldown de ataque ha terminado.
    /// Usado por EnemyMovement para actualizar las observaciones del Agente.
    /// </summary>
    public bool IsAttackOffCooldown()
    {
        return Time.time >= lastAttackTime + attackCooldown;
    }

    /// <summary>
    /// Verifica todas las condiciones necesarias para poder ejecutar un ataque.
    /// </summary>
    private bool CanAttackNow(Transform targetPlayerTransform, IDamageable targetPlayerDamageable)
    {
        return health != null && health.IsAlive() &&
               targetPlayerTransform != null &&
               targetPlayerDamageable != null && targetPlayerDamageable.IsAlive() &&
               IsPlayerInAttackRange(targetPlayerTransform) && // Comprueba rango de nuevo
               IsAttackOffCooldown(); // Comprueba cooldown de nuevo
    }

    /// <summary>
    /// Ejecuta la lógica del ataque (daño y animación).
    /// </summary>
    private void ExecuteAttack(Transform targetPlayerTransform, IDamageable targetPlayerDamageable)
    {
        Vector2 directionToPlayer = (targetPlayerTransform.position - transform.position).normalized;

        // Debug.Log($"{gameObject.name} attacks player ({targetPlayerTransform.name})!");
        targetPlayerDamageable.TakeDamage(attackDamage);
        lastAttackTime = Time.time;

        TriggerDirectionalAttackAnim(directionToPlayer);
    }

    private void TriggerDirectionalAttackAnim(Vector2 direction)
    {
        if (animator == null) return;

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            if (direction.x > 0) animator.SetTrigger(ATTACK_RIGHT_TRIGGER);
            else animator.SetTrigger(ATTACK_LEFT_TRIGGER);
        }
        else
        {
            if (direction.y > 0) animator.SetTrigger(ATTACK_UP_TRIGGER);
            else animator.SetTrigger(ATTACK_DOWN_TRIGGER);
        }
    }

    /// <summary>
    /// Llamado por EnemyHealth.Die() para detener futuras acciones de combate.
    /// </summary>
    public void StopCombat()
    {
        // Dado que las decisiones se toman en Update/FixedUpdate basadas en health.IsAlive(),
        // este método no necesita hacer mucho más que quizás detener una animación de ataque en curso
        // si fuera necesario, pero normalmente el Animator se encargaría de eso al cambiar estados.
        // Por ahora, lo dejamos así, ya que health.IsAlive() prevendrá nuevos ataques.
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue; // Color del Gizmo para el rango de ataque
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}