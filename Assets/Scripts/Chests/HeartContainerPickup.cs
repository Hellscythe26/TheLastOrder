using UnityEngine;

/// <summary>
/// Controla el comportamiento de un item "Contenedor de Corazón"
/// que aumenta la vida máxima del jugador al ser recogido.
/// </summary>
public class HeartContainerPickup : MonoBehaviour
{
    [Tooltip("Cuántos contenedores de corazón (vida máxima) añade este item.")]
    [SerializeField] private int maxHealthIncrease = 1; // Cantidad de vida máxima a añadir.

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
            if (playerHealth != null) // Solo necesita que el script exista, PlayerHealth maneja si está vivo.
            {
                // Llama al método del jugador para aumentar su vida máxima.
                playerHealth.IncreaseMaxHearts(maxHealthIncrease);
                // Destruye el objeto contenedor de corazón una vez recogido.
                Destroy(gameObject);
            }
        }
    }
}