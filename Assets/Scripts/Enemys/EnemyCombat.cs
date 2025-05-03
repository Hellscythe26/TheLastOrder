// En EnemyCombat.cs
using UnityEngine;

public class EnemyCombat : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float attackDamage = 1f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackCooldown = 2f;

    [Header("Component References")]
    [SerializeField] private EnemyHealth health;
    [SerializeField] private Animator animator; // Necesitamos la referencia

    // Internals
    private Transform playerTransform;
    private IDamageable playerDamageable;
    private float lastAttackTime = -Mathf.Infinity;
    private bool canAttack = true;

    // Constantes para Triggers de Animación (¡DEBEN COINCIDIR CON TU ANIMATOR!)
    private const string ATTACK_UP_TRIGGER = "AttackUp";
    private const string ATTACK_DOWN_TRIGGER = "AttackDown";
    private const string ATTACK_LEFT_TRIGGER = "AttackLeft";
    private const string ATTACK_RIGHT_TRIGGER = "AttackRight";


    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
        animator = GetComponent<Animator>(); // Obtener Animator
        if (animator == null) Debug.LogWarning("Animator component missing on EnemyCombat obj!", this);

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null) {
            playerTransform = playerObject.transform;
            playerDamageable = playerObject.GetComponent<IDamageable>();
            if (playerDamageable == null) {
                 canAttack = false;
            }
        } else {
            canAttack = false;
        }
    }

    private void Update()
    {
        // Comprobación principal para atacar
        if (!CanEngage()) return;

        // Comprobar Rango y Cooldown
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer <= attackRange && Time.time >= lastAttackTime + attackCooldown)
        {
            Attack();
        }
    }

    // Función helper para comprobar si se puede atacar en general
    private bool CanEngage()
    {
         return canAttack
                && health != null && health.IsAlive()
                && playerTransform != null && playerDamageable != null && playerDamageable.IsAlive();
    }


    private void Attack()
    {
        if (!CanEngage()) return; // Doble check

        // Calcular dirección ANTES de aplicar daño (por si el jugador se mueve)
        Vector2 directionToPlayer = (playerTransform.position - transform.position).normalized;

        Debug.Log($"{gameObject.name} attacks player!");
        playerDamageable.TakeDamage(attackDamage);
        lastAttackTime = Time.time;

        // Disparar Animación de Ataque Direccional
        TriggerDirectionalAttackAnim(directionToPlayer);
    }

    private void TriggerDirectionalAttackAnim(Vector2 direction)
    {
         if (animator == null) return;

         // Determinar la dirección principal del ataque
         if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y)) {
             // Horizontal
             if (direction.x > 0) animator.SetTrigger(ATTACK_RIGHT_TRIGGER);
             else animator.SetTrigger(ATTACK_LEFT_TRIGGER);
         } else {
             // Vertical (o igual)
             if (direction.y > 0) animator.SetTrigger(ATTACK_UP_TRIGGER);
             else animator.SetTrigger(ATTACK_DOWN_TRIGGER);
         }
    }


    public void StopCombat() // Llamado por EnemyHealth.Die()
    {
        canAttack = false;
    }
}