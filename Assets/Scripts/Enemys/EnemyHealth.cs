// En EnemyHealth.cs
using UnityEngine;
using UnityEngine.Events;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 10f;
    private float currentHealth;
    private bool isAlive = true;

    public UnityEvent OnDeath;
    public UnityEvent<float> OnDamageTaken;

    // Referencias (Animator ya debería estar)
    private EnemyMovement movement;
    private EnemyCombat combat;
    private Collider2D enemyCollider;
    private Rigidbody2D rb;
    private Animator animator; // Asegúrate que se obtenga en Awake

    // --- NUEVO: Constantes para Triggers de Muerte ---
    // !!! USA LOS NOMBRES EXACTOS QUE PUSISTE EN EL ANIMATOR CONTROLLER !!!
    private const string DIE_UP_LEFT_TRIGGER = "DieUpLeft";
    private const string DIE_DOWN_RIGHT_TRIGGER = "DieDownRight";
    // --- FIN NUEVO ---

    // Constantes para parámetros de movimiento (para leer la dirección)
    private const string MOVE_X_PARAM = "MoveX";
    private const string MOVE_Y_PARAM = "MoveY";


     private void Awake()
    {
        currentHealth = maxHealth;
        if (OnDeath == null) OnDeath = new UnityEvent();
        if (OnDamageTaken == null) OnDamageTaken = new UnityEvent<float>();

        // Obtener referencias
        movement = GetComponent<EnemyMovement>();
        combat = GetComponent<EnemyCombat>();
        enemyCollider = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>(); // Obtener Animator
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
        OnDeath.Invoke(); // Invocar evento primero por si algo reacciona

        // --- INICIO: Lógica de Animación de Muerte Direccional ---
        if (animator != null)
        {
            // Leer la última dirección registrada en el Animator
            // Esto asume que EnemyMovement actualiza MoveX/MoveY correctamente,
            // incluso poniendo (0, -1) o similar cuando está idle.
            float lastMoveX = animator.GetFloat(MOVE_X_PARAM);
            float lastMoveY = animator.GetFloat(MOVE_Y_PARAM);

            // Decidir qué animación disparar (misma lógica que el jugador)
            // Arriba (Y > 0) o Izquierda (X < 0)
            // Nota: Si está quieto (0, -1), entrará en la condición de abajo/derecha. Ajusta si necesitas otro comportamiento para idle.
            if (lastMoveY > 0.1f || lastMoveX < -0.1f) // Umbral pequeño para evitar errores de precisión
            {
                animator.SetTrigger(DIE_UP_LEFT_TRIGGER);
            }
            else // Abajo (Y <= 0.1) Y Derecha (X >= -0.1)
            {
                 animator.SetTrigger(DIE_DOWN_RIGHT_TRIGGER);
            }
        }
        // --- FIN: Lógica de Animación ---


        // --- Desactivar Componentes (después de disparar la animación) ---
        if (movement != null) movement.StopMovement();
        if (combat != null) combat.StopCombat();
        if (enemyCollider != null) enemyCollider.enabled = false;
        if (rb != null)
        {
             rb.simulated = false;
             rb.linearVelocity = Vector2.zero;
             rb.angularVelocity = 0f;
        }
        // --- Fin Desactivación ---


        // Destruir con Retraso
        Destroy(gameObject, 5f); // Ajusta el tiempo si necesitas que la animación termine
    }

    public bool IsAlive()
    {
        return isAlive;
    }
}