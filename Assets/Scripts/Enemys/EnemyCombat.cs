using UnityEngine;

/// <summary>
/// Gestiona la lógica de ataque de un enemigo, incluyendo daño, rango, cooldown y animaciones.
/// </summary>
public class EnemyCombat : MonoBehaviour
{
    [Header("Stats")]
    [Tooltip("Daño infligido por cada ataque.")]
    [SerializeField] private float attackDamage = 1f;
    [Tooltip("Rango dentro del cual el enemigo puede ejecutar un ataque.")]
    [SerializeField] public float attackRange = 1.5f; // Público para ser consultado por otros scripts (ej. Agents).
    [Tooltip("Tiempo mínimo en segundos entre ataques sucesivos.")]
    [SerializeField] private float attackCooldown = 2f;
    [Header("Component References")]
    [Tooltip("Referencia al sistema de vida del enemigo.")]
    [SerializeField] private EnemyHealth health;
    [Tooltip("Referencia al Animator para las animaciones de ataque.")]
    [SerializeField] private Animator animator;
    private float lastAttackTime = -Mathf.Infinity; // Tiempo en el que se realizó el último ataque.
    // Constantes para los nombres de los Triggers de animación de ataque.
    private const string ATTACK_UP_TRIGGER = "AttackUp";
    private const string ATTACK_DOWN_TRIGGER = "AttackDown";
    private const string ATTACK_LEFT_TRIGGER = "AttackLeft";
    private const string ATTACK_RIGHT_TRIGGER = "AttackRight";

    /// <summary>
    /// Se llama una vez cuando el script es cargado. Inicializa referencias.
    /// </summary>
    private void Awake()
    {
        if (health == null) health = GetComponent<EnemyHealth>();
        if (animator == null) animator = GetComponent<Animator>();
        if (health == null) Debug.LogError("EnemyHealth component missing on " + gameObject.name + "!", this);
        // Animator es opcional para el combate, pero se advierte si falta.
        if (animator == null) Debug.LogWarning("Animator component missing on " + gameObject.name + "! Attack animations might not play.", this);
    }

    /// <summary>
    /// Intenta ejecutar un ataque contra el objetivo especificado.
    /// Este método es llamado externamente (ej. por EnemyMovement o SentinelDetector)
    /// cuando el Agente ha decidido que la acción es atacar.
    /// </summary>
    /// <param name="targetPlayerTransform">El Transform del jugador objetivo.</param>
    /// <param name="targetPlayerDamageable">La interfaz IDamageable del jugador para infligirle daño.</param>
    public void TryPerformAttack(Transform targetPlayerTransform, IDamageable targetPlayerDamageable)
    {
        // Realiza una última verificación de todas las condiciones antes de ejecutar el ataque.
        if (CanAttackNow(targetPlayerTransform, targetPlayerDamageable))
        {
            ExecuteAttack(targetPlayerTransform, targetPlayerDamageable);
        }
    }

    /// <summary>
    /// Comprueba si el jugador objetivo está dentro del rango de ataque definido.
    /// Esta información es usada por el Agente para tomar decisiones.
    /// </summary>
    /// <param name="targetPlayerTransform">El Transform del jugador objetivo.</param>
    /// <returns>True si el jugador está en rango, false en caso contrario.</returns>
    public bool IsPlayerInAttackRange(Transform targetPlayerTransform)
    {
        if (targetPlayerTransform == null || health == null || !health.IsAlive())
        {
            return false; // No se puede determinar el rango si no hay objetivo o el atacante no está en condiciones.
        }
        // Calcula la distancia 2D entre el enemigo y el jugador.
        // Se asume un juego 2D o que el rango de ataque es un círculo/esfera.
        return Vector2.Distance(new Vector2(transform.position.x, transform.position.y), 
                                new Vector2(targetPlayerTransform.position.x, targetPlayerTransform.position.y)) <= attackRange;
    }

    /// <summary>
    /// Comprueba si el cooldown del ataque ha terminado, permitiendo un nuevo ataque.
    /// Esta información es usada por el Agente para tomar decisiones.
    /// </summary>
    /// <returns>True si el ataque está listo (fuera de cooldown), false en caso contrario.</returns>
    public bool IsAttackOffCooldown()
    {
        return Time.time >= lastAttackTime + attackCooldown;
    }
    /// <summary>
    /// Verifica internamente si todas las condiciones para un ataque se cumplen:
    /// el atacante está vivo, el objetivo es válido y está vivo,
    /// el objetivo está en rango y el ataque no está en cooldown.
    /// </summary>
    private bool CanAttackNow(Transform targetPlayerTransform, IDamageable targetPlayerDamageable)
    {
        return health != null && health.IsAlive() &&
               targetPlayerTransform != null &&
               targetPlayerDamageable != null && targetPlayerDamageable.IsAlive() &&
               IsPlayerInAttackRange(targetPlayerTransform) &&
               IsAttackOffCooldown();
    }

    /// <summary>
    /// Ejecuta la acción de ataque: inflige daño al objetivo y dispara la animación de ataque.
    /// </summary>
    private void ExecuteAttack(Transform targetPlayerTransform, IDamageable targetPlayerDamageable)
    {
        // Calcula la dirección hacia el jugador para la animación.
        Vector2 directionToPlayer = (targetPlayerTransform.position - transform.position).normalized;
        targetPlayerDamageable.TakeDamage(attackDamage); // Inflige daño al objetivo.
        lastAttackTime = Time.time; // Registra el tiempo del ataque para el cooldown.
        TriggerDirectionalAttackAnim(directionToPlayer); // Dispara la animación de ataque.
    }

    /// <summary>
    /// Dispara la animación de ataque correcta en el Animator basándose en la dirección al jugador.
    /// </summary>
    /// <param name="direction">Vector normalizado de la dirección hacia el jugador.</param>
    private void TriggerDirectionalAttackAnim(Vector2 direction)
    {
        if (animator == null) return; // No hacer nada si no hay animator.
        // Determina la animación predominante (horizontal o vertical).
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            // Movimiento horizontal es más fuerte.
            if (direction.x > 0) animator.SetTrigger(ATTACK_RIGHT_TRIGGER);
            else animator.SetTrigger(ATTACK_LEFT_TRIGGER);
        }
        else
        {
            // Movimiento vertical es más fuerte o igual.
            if (direction.y > 0) animator.SetTrigger(ATTACK_UP_TRIGGER);
            else animator.SetTrigger(ATTACK_DOWN_TRIGGER);
        }
    }

    /// <summary>
    /// Método llamado externamente (ej. por EnemyHealth cuando el enemigo muere)
    /// para indicar que el combate debe detenerse.
    /// </summary>
    public void StopCombat()
    {
        // Actualmente, la lógica de CanAttackNow() ya impide atacar si health.IsAlive() es false.
        // Este método podría usarse para lógicas adicionales si fueran necesarias (ej. cancelar un ataque en curso).
    }

    /// <summary>
    /// Se llama en el editor cuando el GameObject está seleccionado. Dibuja el Gizmo del rango de ataque.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}