// --- En PlayerHealth.cs ---
using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    // AHORA REPRESENTAN CORAZONES DIRECTAMENTE
    [Tooltip("Número inicial de contenedores de corazón")]
    [SerializeField] private int startingHearts = 3;

    [Tooltip("Máximo número de contenedores de corazón posibles")]
    [SerializeField] private int absoluteMaxHearts = 20;

    // Capacidad máxima ACTUAL de contenedores de corazón
    private int currentMaxHearts;
    public int CurrentMaxHearts => currentMaxHearts;

    // Vida actual (float, donde 1.0 = 1 corazón lleno, 0.5 = medio corazón)
    private float currentHealth;
    public float CurrentHealth => currentHealth;

    private bool isAlive = true;

    // Eventos
    public UnityEvent<float> OnHealthChanged; // Pasa la vida actual (ej: 2.5f)
    public UnityEvent OnDeath;
    public UnityEvent OnMaxHealthChanged; // Notifica cambio de capacidad

    private void Awake()
    {
        currentMaxHearts = startingHearts;
        currentHealth = currentMaxHearts; // Inicia lleno

        // Inicializar eventos
        OnHealthChanged = new UnityEvent<float>();
        OnDeath = new UnityEvent();
        OnMaxHealthChanged = new UnityEvent();
    }

    private void Start()
    {
        // Notificar estado inicial a la UI
        OnMaxHealthChanged.Invoke();
        OnHealthChanged.Invoke(currentHealth); // Enviar estado inicial
    }

    // El daño se recibe directamente en "corazones" (ej: 1.5f)
    public void TakeDamage(float damageAmountInHearts)
    {
        if (!isAlive) return;

        currentHealth -= damageAmountInHearts;
        currentHealth = Mathf.Max(0, currentHealth);
        OnHealthChanged.Invoke(currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // La curación es directamente en "corazones" (ej: 0.5f)
    public void Heal(float healAmountInHearts)
    {
        if (!isAlive) return;

        currentHealth += healAmountInHearts;
        // No exceder la capacidad máxima ACTUAL (que es un entero)
        currentHealth = Mathf.Min(currentHealth, currentMaxHearts);
        OnHealthChanged.Invoke(currentHealth);
    }

    // Aumenta la capacidad máxima en número de CONTENEDORES de corazón
    public void IncreaseMaxHearts(int heartsToAdd)
    {
        if (!isAlive || heartsToAdd <= 0) return;

        int newMax = Mathf.Min(currentMaxHearts + heartsToAdd, absoluteMaxHearts);

        if (newMax > currentMaxHearts)
        {
            int addedCapacity = newMax - currentMaxHearts; // Cuántos contenedores se añadieron realmente
            currentMaxHearts = newMax;

            // Opcional: Curar la cantidad añadida o al nuevo máximo?
            Heal(addedCapacity); // Cura tantos "corazones" como contenedores se añadieron

            OnMaxHealthChanged.Invoke();
        }
    }

    public void Die()
    {
        if (!isAlive) return;
        isAlive = false;
        if (OnDeath != null)
        {
            Player playerScript = GetComponent<Player>();
            playerScript.HandleDeath();
            //OnDeath.Invoke(); // Invoca el evento
        }
    }

    public bool IsAlive()
    {
        return isAlive;
    }
}