using UnityEngine;
using UnityEngine.Events;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 10f;
    private float currentHealth;
    private bool isAlive = true;
    public UnityEvent OnDeath;
    public UnityEvent<float> OnDamageTaken;
    public event System.Action<EnemyHealth> OnEnemyDiedCallback; //Nueva Línea
    private EnemyMovement movement;
    private EnemyCombat combat;
    private Collider2D enemyCollider;
    private Rigidbody2D rb;
    private Animator animator;
    private const string DIE_UP_LEFT_TRIGGER = "DieUpLeft";
    private const string DIE_DOWN_RIGHT_TRIGGER = "DieDownRight";
    private const string MOVE_X_PARAM = "MoveX";
    private const string MOVE_Y_PARAM = "MoveY";

     private void Awake()
    {
        currentHealth = maxHealth;
        if (OnDeath == null) OnDeath = new UnityEvent();
        if (OnDamageTaken == null) OnDamageTaken = new UnityEvent<float>();
        movement = GetComponent<EnemyMovement>();
        combat = GetComponent<EnemyCombat>();
        enemyCollider = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        if (animator == null) Debug.LogWarning("Animator component missing on EnemyHealth obj!", this);
    }

    public void TakeDamage(float damage)
    {
         if (!isAlive) return;
         currentHealth -= damage;
         currentHealth = Mathf.Max(0, currentHealth);
         OnDamageTaken.Invoke(damage);
         if (currentHealth <= 0)
         {
             Die();
         }
    }

    private void Die()
    {
        if (!isAlive) return;
        isAlive = false;
        Debug.Log($"{gameObject.name} ha muerto.");
        OnEnemyDiedCallback?.Invoke(this);
        OnDeath.Invoke();
        if (animator != null)
        {
            // Leer la última dirección registrada en el Animator
            float lastMoveX = animator.GetFloat(MOVE_X_PARAM);
            float lastMoveY = animator.GetFloat(MOVE_Y_PARAM);
            // Decidir qué animación disparar (misma lógica que el jugador)
            // Arriba (Y > 0) o Izquierda (X < 0)
            // Nota: Si está quieto (0, -1), entrará en la condición de abajo/derecha.
            if (lastMoveY > 0.1f || lastMoveX < -0.1f)
            {
                animator.SetTrigger(DIE_UP_LEFT_TRIGGER);
            }
            else // Abajo (Y <= 0.1) Y Derecha (X >= -0.1)
            {
                 animator.SetTrigger(DIE_DOWN_RIGHT_TRIGGER);
            }
        }
        if (movement != null) movement.StopMovement();
        if (combat != null) combat.StopCombat();
        if (enemyCollider != null) enemyCollider.enabled = false;
        if (rb != null)
        {
             rb.simulated = false;
             rb.linearVelocity = Vector2.zero;
             rb.angularVelocity = 0f;
        }
        // Destruir con Retraso
        Destroy(gameObject, 5f);
    }

    private void OnDestroy()
    {
        OnEnemyDiedCallback = null; // Limpia los suscriptores
    }

    public bool IsAlive()
    {
        return isAlive;
    }
}