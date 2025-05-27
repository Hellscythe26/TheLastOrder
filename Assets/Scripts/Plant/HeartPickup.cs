using UnityEngine;

/// <summary>
/// Controla el comportamiento de un item "Corazón" que el jugador puede recoger
/// para recuperar una porción de vida (medio corazón por defecto).
/// </summary>
public class HeartPickup : MonoBehaviour
{
    [Tooltip("Cantidad de vida que este item recupera (0.5f = medio corazón, 1.0f = un corazón completo).")]
    [SerializeField] private float healAmount = 0.5f; // Cantidad de curación que proporciona.

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
            // Comprueba si se encontró el componente PlayerHealth y si el jugador está vivo.
            // La lógica de si el jugador ya tiene vida máxima se maneja dentro de PlayerHealth.Heal().
            if (playerHealth != null && playerHealth.IsAlive())
            {
                // Llama al método Heal del jugador para aplicar la curación.
                playerHealth.Heal(healAmount);
                // Destruye el objeto corazón una vez recogido.
                Destroy(gameObject);
            }
        }
    }
}