// DestructiblePlant.cs
using UnityEngine;

// Implementamos IDamageable para que pueda recibir daño del jugador
public class DestructiblePlant : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    [Tooltip("Cuántos golpes necesita para destruirse (o cuánta vida tiene)")]
    [SerializeField] private float health = 1f; // Por ejemplo, 1 golpe la destruye

    [Header("Drops")]
    [Tooltip("Arrastra aquí el Prefab del HeartPickup")]
    [SerializeField] private GameObject heartPickupPrefab;
    [Tooltip("Probabilidad (0.0 a 1.0) de soltar el corazón al destruirse")]
    [Range(0f, 1f)] // Slider en el inspector
    [SerializeField] private float heartDropChance = 0.5f; // 50% por defecto

    // Opcional: Animación/Efectos
    // [SerializeField] private Animator animator;
    // [SerializeField] private GameObject destructionEffect;
    // private const string HIT_TRIGGER = "Hit";
    // private const string DESTROY_TRIGGER = "Destroy";

    private bool isAlive = true; // Para implementar IDamageable

    // --- IDamageable Implementation ---

    public void TakeDamage(float damage)
    {
        if (!isAlive) return; // No hacer nada si ya está destruida

        health -= damage;
        // Opcional: Disparar animación de golpe
        // if (animator != null) animator.SetTrigger(HIT_TRIGGER);

        if (health <= 0)
        {
            Die();
        }
    }

    public bool IsAlive()
    {
        return isAlive;
    }

    // --- Lógica de Destrucción ---

    private void Die()
    {
        if (!isAlive) return; // Evitar doble ejecución
        isAlive = false;
        Debug.Log("Planta destruida!");

        // --- Lógica de Drop ---
        if (heartPickupPrefab != null) // Comprobar si hay prefab asignado
        {
            // Generar un número aleatorio entre 0.0 y 1.0
            float randomValue = Random.value; // UnityEngine.Random.value

            if (randomValue <= heartDropChance)
            {
                // ¡Soltar el corazón!
                Debug.Log("¡Soltando corazón!");
                Instantiate(heartPickupPrefab, transform.position, Quaternion.identity);
            }
            else
            {
                Debug.Log("No se soltó corazón esta vez.");
            }
        }
        else
        {
            Debug.LogWarning("Heart Pickup Prefab no asignado en la planta.", this);
        }
        // --- Fin Lógica de Drop ---


        // Opcional: Disparar animación/efecto de destrucción
        // if (animator != null) animator.SetTrigger(DESTROY_TRIGGER);
        // if (destructionEffect != null) Instantiate(destructionEffect, transform.position, Quaternion.identity);

        // Destruir el GameObject de la planta
        // Podrías añadir un pequeño delay si tienes animación de destrucción
        Destroy(gameObject /*, delay*/);
    }
}