// HeartContainerPickup.cs
using UnityEngine;

public class HeartContainerPickup : MonoBehaviour
{
    [Tooltip("Cuánta vida máxima añade este contenedor")]
    [SerializeField] private int maxHealthIncrease = 1;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Asume que tu jugador tiene un script PlayerHealth
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                Debug.Log($"Jugador recogió Contenedor de Corazón. Aumentando vida máxima en {maxHealthIncrease}.");
                playerHealth.IncreaseMaxHearts(maxHealthIncrease); // Necesitas añadir este método a PlayerHealth

                // Aquí puedes añadir efectos de sonido/partículas
                // AudioSource.PlayClipAtPoint(pickupSound, transform.position);
                // Instantiate(pickupEffect, transform.position, Quaternion.identity);

                Destroy(gameObject); // Destruir el pickup
            }
        }
    }
}