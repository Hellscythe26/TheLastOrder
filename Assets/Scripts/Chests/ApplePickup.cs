using UnityEngine;

/// <summary>
/// Controla el comportamiento de un item "Manzana" que el jugador puede recoger para curarse.
/// </summary>
public class ApplePickup : MonoBehaviour
{
    [Tooltip("Cuánta vida recupera la manzana (1 = 1 corazón)")]
    [SerializeField] private float healAmount = 1.0f; // Recupera 1 corazón completo

    /// <summary>
    /// Se llama automáticamente por Unity cuando otro Collider2D entra en el trigger de este objeto.
    /// </summary>
    /// <param name="other">El Collider2D del objeto que entró en el trigger.</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Comprueba si el objeto que colisionó es el jugador (usando el Tag "Player").
        if (other.CompareTag("Player"))
        {
            // Intenta obtener el componente PlayerHealth del objeto jugador.
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            // Comprueba si se encontró el componente PlayerHealth y si el jugador tiene menos vida que su máximo actual.
            if (playerHealth != null && playerHealth.CurrentHealth < playerHealth.CurrentMaxHearts)
            {
                Debug.Log($"Jugador recogió Manzana. Curando {healAmount} corazones.");
                // Llama al método Heal del jugador para aplicar la curación.
                playerHealth.Heal(healAmount);
                // Destruye el objeto manzana una vez recogido.
                Destroy(gameObject);
            }
            // Comprueba si el jugador ya tiene la vida al máximo.
            else if (playerHealth != null && playerHealth.CurrentHealth >= playerHealth.CurrentMaxHearts)
            {
                Debug.Log("Jugador recogió Manzana pero ya tiene la vida llena.");
                // Destruye el objeto manzana igualmente, aunque no cure.
                Destroy(gameObject);
            }
        }
    }
}