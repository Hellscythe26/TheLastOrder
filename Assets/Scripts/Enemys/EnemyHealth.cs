using UnityEngine;
using UnityEngine.Events; // Para UnityEvent

/// <summary>
/// Gestiona la salud y el proceso de muerte de un enemigo.
/// Implementa IDamageable para poder recibir daño.
/// </summary>
public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Tooltip("Salud máxima inicial del enemigo.")]
    [SerializeField] private float maxHealth = 10f;
    private float currentHealth; // Salud actual del enemigo.
    private bool isAlive = true; // Estado para controlar si el enemigo está vivo.

    [Tooltip("Evento que se dispara cuando el enemigo muere.")]
    public UnityEvent OnDeath; // Evento estándar de Unity para la muerte.
    [Tooltip("Evento que se dispara cuando el enemigo recibe daño. Pasa la cantidad de daño recibido.")]
    public UnityEvent<float> OnDamageTaken; // Evento para cuando recibe daño, envía el valor del daño.
    // Se usa para el RoomController: Notificación de muerte para el RoomController.
    /// <summary>
    /// Evento C# que se dispara cuando el enemigo muere.
    /// Es usado por RoomController para rastrear enemigos activos.
    /// </summary>
    public event System.Action<EnemyHealth> OnEnemyDiedCallback;
    // Referencias a otros componentes del enemigo para desactivarlos al morir.
    private EnemyMovement movement;
    private EnemyCombat combat;
    private Collider2D enemyCollider;
    private Rigidbody2D rb;
    private Animator animator;
    // Constantes para los nombres de los Triggers de animación de muerte.
    private const string DIE_UP_LEFT_TRIGGER = "DieUpLeft";
    private const string DIE_DOWN_RIGHT_TRIGGER = "DieDownRight";
    // Constantes para leer la última dirección de movimiento del Animator.
    private const string MOVE_X_PARAM = "MoveX";
    private const string MOVE_Y_PARAM = "MoveY";

    /// <summary>
    /// Se llama una vez cuando el script es cargado.
    /// Inicializa la salud y obtiene referencias a otros componentes.
    /// </summary>
     private void Awake()
    {
        currentHealth = maxHealth; // Establece la salud actual al máximo al inicio.
        isAlive = true;
        // Inicializa los UnityEvents si no lo están para evitar errores.
        if (OnDeath == null) OnDeath = new UnityEvent();
        if (OnDamageTaken == null) OnDamageTaken = new UnityEvent<float>();
        // Obtiene referencias a otros componentes del mismo GameObject.
        movement = GetComponent<EnemyMovement>();
        combat = GetComponent<EnemyCombat>();
        enemyCollider = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        if (animator == null) Debug.LogWarning("Animator component missing on EnemyHealth obj " + gameObject.name + "!", this);
    }

    /// <summary>
    /// Implementación del método TakeDamage de la interfaz IDamageable.
    /// Aplica daño al enemigo y comprueba si muere.
    /// </summary>
    /// <param name="damage">La cantidad de daño a infligir.</param>
    public void TakeDamage(float damage)
    {
         if (!isAlive) return; // No hacer nada si ya está muerto.
         currentHealth -= damage;
         currentHealth = Mathf.Max(0, currentHealth); // Asegura que la salud no sea negativa.
         OnDamageTaken.Invoke(damage); // Dispara el evento de daño recibido.
         if (currentHealth <= 0)
         {
             Die(); // Si la salud llega a cero o menos, el enemigo muere.
         }
    }

    /// <summary>
    /// Lógica que se ejecuta cuando el enemigo muere.
    /// Desactiva componentes, dispara animaciones y eventos, y programa la destrucción del GameObject.
    /// </summary>
    private void Die()
    {
        if (!isAlive) return; // Prevenir ejecución múltiple.
        isAlive = false;
        // Se usa para el modelo de simulación RoomController: Invocar callback de muerte.
        OnEnemyDiedCallback?.Invoke(this); // Notifica a los suscriptores (ej. RoomController).
        OnDeath.Invoke(); // Dispara el UnityEvent de muerte.
        // Lógica de animación de muerte direccional.
        if (animator != null)
        {
            float lastMoveX = animator.GetFloat(MOVE_X_PARAM);
            float lastMoveY = animator.GetFloat(MOVE_Y_PARAM);
            if (lastMoveY > 0.1f || lastMoveX < -0.1f) // Prioriza Arriba o Izquierda
            {
                animator.SetTrigger(DIE_UP_LEFT_TRIGGER);
            }
            else // Abajo o Derecha (o quieto mirando abajo/derecha por defecto)
            {
                 animator.SetTrigger(DIE_DOWN_RIGHT_TRIGGER);
            }
        }

        // Desactiva otros componentes del enemigo.
        if (movement != null) movement.StopMovement(); // Llama a StopMovement si existe el script.
        if (combat != null) combat.StopCombat();       // Llama a StopCombat si existe el script.
        if (enemyCollider != null) enemyCollider.enabled = false; // Desactiva el collider.
        if (rb != null)
        {
             rb.simulated = false; // Detiene la simulación física del Rigidbody.
             rb.linearVelocity = Vector2.zero;
             rb.angularVelocity = 0f;
        }

        // Destruye el GameObject después de un retraso (para permitir que se reproduzca la animación de muerte).
        Destroy(gameObject, 5f);
    }

    /// <summary>
    /// Limpia las suscripciones al evento OnEnemyDiedCallback cuando el objeto se destruye
    /// para prevenir posibles errores de referencia.
    /// </summary>
    private void OnDestroy()
    {
        // Se usa para el modelo de simulación RoomController: Limpieza de callback.
        OnEnemyDiedCallback = null;
    }

    /// <summary>
    /// Implementación del método IsAlive de la interfaz IDamageable.
    /// </summary>
    /// <returns>True si el enemigo está vivo, false en caso contrario.</returns>
    public bool IsAlive()
    {
        return isAlive;
    }
}