using UnityEngine;

public class ApplePickup : MonoBehaviour
{
    [Tooltip("Cuánta vida recupera la manzana (1 = 1 corazón)")]
    [SerializeField] private float healAmount = 1.0f; // Recupera 1 corazón completo

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            // Comprobar si el jugador necesita curación antes de aplicarla (opcional)
            if (playerHealth != null && playerHealth.CurrentHealth < playerHealth.CurrentMaxHearts) // Asumiendo que tienes estos métodos/propiedades
            {
                Debug.Log($"Jugador recogió Manzana. Curando {healAmount} corazones.");
                playerHealth.Heal(healAmount); // Usar el método Heal existente

                // Efectos
                // ...

                Destroy(gameObject);
            }
            else if (playerHealth != null && playerHealth.CurrentHealth >= playerHealth.CurrentMaxHearts)
            {
                // Opcional: Qué hacer si el jugador tiene la vida llena?
                // Podrías no destruirlo, o destruirlo sin curar, o dar un pequeño feedback.
                 Debug.Log("Jugador recogió Manzana pero ya tiene la vida llena.");
                 Destroy(gameObject); // Destruir igualmente en este ejemplo
            }
        }
    }
}