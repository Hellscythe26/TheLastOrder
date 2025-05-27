using UnityEngine;
using UnityEngine.Events; // Para UnityEvent

/// <summary>
/// Gestiona la salud del jugador, incluyendo la vida máxima, daño, curación y muerte.
/// Implementa la interfaz IDamageable.
/// </summary>
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Tooltip("Número inicial de contenedores de corazón con los que empieza el jugador.")]
    [SerializeField] private int startingHearts = 3;
    [Tooltip("Máximo número absoluto de contenedores de corazón que el jugador puede llegar a tener.")]
    [SerializeField] private int absoluteMaxHearts = 20;
    // Capacidad máxima ACTUAL de contenedores de corazón del jugador.
    private int currentMaxHearts;
    /// <summary>
    /// Obtiene la capacidad máxima actual de corazones del jugador.
    /// </summary>
    public int CurrentMaxHearts => currentMaxHearts;
    // Vida actual del jugador, donde 1.0f representa un corazón completo.
    private float currentHealth;
    /// <summary>
    /// Obtiene la salud actual del jugador (ej: 2.5f = dos corazones y medio).
    /// </summary>
    public float CurrentHealth => currentHealth;
    private bool isAlive = true; // Estado para controlar si el jugador está vivo.
    // Eventos que se disparan en diferentes cambios de estado de la salud.
    [Tooltip("Se dispara cuando la salud actual del jugador cambia. Pasa la nueva salud actual.")]
    public UnityEvent<float> OnHealthChanged;
    [Tooltip("Se dispara cuando la salud del jugador llega a cero.")]
    public UnityEvent OnDeath;
    [Tooltip("Se dispara cuando la capacidad máxima de corazones del jugador cambia.")]
    public UnityEvent OnMaxHealthChanged;

    /// <summary>
    /// Se llama una vez cuando el script es cargado.
    /// Inicializa la salud máxima y actual, y los eventos.
    /// </summary>
    private void Awake()
    {
        currentMaxHearts = startingHearts; // Establece la capacidad máxima inicial.
        currentHealth = currentMaxHearts;  // El jugador empieza con la vida llena.
        isAlive = true;
        // Inicializa los UnityEvents para evitar errores si no se asignan en el Inspector.
        if (OnHealthChanged == null) OnHealthChanged = new UnityEvent<float>();
        if (OnDeath == null) OnDeath = new UnityEvent();
        if (OnMaxHealthChanged == null) OnMaxHealthChanged = new UnityEvent();
    }

    /// <summary>
    /// Se llama una vez después de Awake, antes del primer frame de Update.
    /// Notifica a los sistemas (ej. UI) el estado inicial de la salud.
    /// </summary>
    private void Start()
    {
        // Dispara eventos para que la UI u otros sistemas reflejen el estado inicial.
        OnMaxHealthChanged.Invoke();
        OnHealthChanged.Invoke(currentHealth);
    }

    /// <summary>
    /// Implementación del método TakeDamage de la interfaz IDamageable.
    /// Reduce la salud actual del jugador por la cantidad especificada.
    /// </summary>
    /// <param name="damageAmountInHearts">La cantidad de daño a infligir, en unidades de "corazones".</param>
    public void TakeDamage(float damageAmountInHearts)
    {
        if (!isAlive) return; // No procesar daño si el jugador no está vivo.
        currentHealth -= damageAmountInHearts;
        currentHealth = Mathf.Max(0, currentHealth); // Asegura que la salud no sea negativa.
        OnHealthChanged.Invoke(currentHealth); // Dispara evento de cambio de salud.
        if (currentHealth <= 0)
        {
            Die(); // Si la salud llega a cero, el jugador muere.
        }
    }

    /// <summary>
    /// Aumenta la salud actual del jugador por la cantidad especificada,
    /// sin exceder la capacidad máxima actual de corazones.
    /// </summary>
    /// <param name="healAmountInHearts">La cantidad de curación, en unidades de "corazones".</param>
    public void Heal(float healAmountInHearts)
    {
        if (!isAlive || healAmountInHearts <=0) return; // No curar si no está vivo o la curación es nula/negativa.
        currentHealth += healAmountInHearts;
        // Limita la salud actual a la capacidad máxima actual de corazones.
        currentHealth = Mathf.Min(currentHealth, currentMaxHearts);
        OnHealthChanged.Invoke(currentHealth); // Dispara evento de cambio de salud.
    }

    /// <summary>
    /// Aumenta la capacidad máxima de corazones del jugador.
    /// También cura al jugador la cantidad de capacidad añadida.
    /// </summary>
    /// <param name="heartsToAdd">El número de contenedores de corazón completos a añadir a la capacidad máxima.</param>
    public void IncreaseMaxHearts(int heartsToAdd)
    {
        if (!isAlive || heartsToAdd <= 0) return; // No hacer nada si no está vivo o no se añaden corazones.
        // Calcula la nueva capacidad máxima, sin exceder el máximo absoluto permitido.
        int newMax = Mathf.Min(currentMaxHearts + heartsToAdd, absoluteMaxHearts);
        // Solo proceder si la nueva capacidad es realmente mayor que la actual.
        if (newMax > currentMaxHearts)
        {
            int addedCapacity = newMax - currentMaxHearts; // Cuántos contenedores se añadieron efectivamente.
            currentMaxHearts = newMax; // Actualiza la capacidad máxima.
            Heal(addedCapacity); // Cura al jugador por la cantidad de capacidad añadida.
            OnMaxHealthChanged.Invoke(); // Dispara evento de cambio de vida máxima (para UI, etc.).
        }
    }

    /// <summary>
    /// Inicia el proceso de muerte del jugador.
    /// Llama al método HandleDeath en el script Player principal.
    /// </summary>
    public void Die()
    {
        if (!isAlive) return; // Prevenir ejecución múltiple.
        isAlive = false;
        if (OnDeath != null) // Comprueba si hay suscriptores al evento OnDeath.
        {
            // Obtiene el script Player para llamar a su método HandleDeath.
            // Esto asume que PlayerHealth y Player están en el mismo GameObject.
            Player playerScript = GetComponent<Player>();
            if (playerScript != null)
            {
                playerScript.HandleDeath(); // El script Player gestiona las consecuencias de la muerte.
            }
            else
            {
                 Debug.LogError("PlayerHealth.Die(): No se encontró el script Player en este GameObject.", this);
            }
        }
    }

    /// <summary>
    /// Implementación del método IsAlive de la interfaz IDamageable.
    /// </summary>
    /// <returns>True si el jugador está vivo, false en caso contrario.</returns>
    public bool IsAlive()
    {
        return isAlive;
    }
}