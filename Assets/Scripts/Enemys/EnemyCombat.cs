// EnemyCombat.cs
using UnityEngine;

public class EnemyCombat : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float attackDamage = 1f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackCooldown = 2f;

    [Header("Component References")]
    [SerializeField] private EnemyHealth health;
    [SerializeField] private Animator animator;

    // Ya no necesitamos playerTransform y playerDamageable como variables de clase
    private float lastAttackTime = -Mathf.Infinity;
    private bool canAttack = true; // Esta se maneja ahora basado en si el jugador existe y está vivo

    private const string ATTACK_UP_TRIGGER = "AttackUp";
    private const string ATTACK_DOWN_TRIGGER = "AttackDown";
    private const string ATTACK_LEFT_TRIGGER = "AttackLeft";
    private const string ATTACK_RIGHT_TRIGGER = "AttackRight";

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
        animator = GetComponent<Animator>();
        if (animator == null) Debug.LogWarning("Animator component missing on EnemyCombat obj!", this);

        // Ya no buscamos al jugador en Awake aquí
    }

    private void Update()
    {
        // Obtenemos las referencias al jugador aquí, cada frame que necesitemos atacar.
        // Esto asegura que siempre tengamos la instancia correcta del jugador persistente.
        Transform currentPlayerTransform = null;
        IDamageable currentPlayerDamageable = null;

        if (Player.Instance != null) // Usamos el Singleton Player.Instance
        {
            currentPlayerTransform = Player.Instance.transform;
            currentPlayerDamageable = Player.Instance.GetComponent<IDamageable>(); // PlayerHealth implementa IDamageable
        }

        // Comprobación principal para atacar
        if (!CanEngage(currentPlayerTransform, currentPlayerDamageable)) return;

        // Comprobar Rango y Cooldown
        // Es importante que CanEngage ya haya verificado que currentPlayerTransform no es null
        float distanceToPlayer = Vector2.Distance(transform.position, currentPlayerTransform.position);
        if (distanceToPlayer <= attackRange && Time.time >= lastAttackTime + attackCooldown)
        {
            Attack(currentPlayerTransform, currentPlayerDamageable);
        }
    }

    // Modificado para recibir las referencias del jugador
    private bool CanEngage(Transform targetPlayerTransform, IDamageable targetPlayerDamageable)
    {
        // canAttack se refiere a si el componente de combate en sí está habilitado para atacar,
        // no necesariamente si el jugador está al alcance o vivo en este frame.
        // La condición de vida del jugador y si existe se comprueba con targetPlayerDamageable.
        return canAttack
               && health != null && health.IsAlive()
               && targetPlayerTransform != null // Asegurarse que el transform del jugador existe
               && targetPlayerDamageable != null && targetPlayerDamageable.IsAlive(); // Asegurarse que el IDamageable existe y está vivo
    }

    // Modificado para recibir las referencias del jugador
    private void Attack(Transform targetPlayerTransform, IDamageable targetPlayerDamageable)
    {
        // No necesitamos la doble comprobación de CanEngage aquí si Update ya lo hizo
        // y pasó las referencias correctas.

        Vector2 directionToPlayer = (targetPlayerTransform.position - transform.position).normalized;

        Debug.Log($"{gameObject.name} attacks player ({targetPlayerTransform.name})!");
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

    public void StopCombat()
    {
        canAttack = false; // Esto deshabilita la capacidad de este componente para iniciar ataques
    }
}