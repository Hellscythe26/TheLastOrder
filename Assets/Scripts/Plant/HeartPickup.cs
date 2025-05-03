// HeartPickup.cs
using UnityEngine;

public class HeartPickup : MonoBehaviour
{
    // Cantidad de "corazones" que cura (0.5 para medio corazón)
    [SerializeField] private float healAmount = 0.5f;
    // Opcional: Efecto de sonido/partículas al recoger
    // public GameObject pickupEffect;
    // public AudioClip pickupSound;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Comprobar si el objeto que entró es el jugador
        if (other.CompareTag("Player"))
        {
            // Intentar obtener el componente PlayerHealth del jugador
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null && playerHealth.IsAlive()) // Asegurarse que el jugador esté vivo
            {
                Debug.Log($"Player recogió corazón. Curando {healAmount}.");
                // Llamar al método Heal del jugador
                playerHealth.Heal(healAmount);

                // Opcional: Instanciar efecto/reproducir sonido
                // if(pickupEffect) Instantiate(pickupEffect, transform.position, Quaternion.identity);
                // if(pickupSound) AudioSource.PlayClipAtPoint(pickupSound, transform.position);

                // Destruir el objeto del corazón
                Destroy(gameObject);
            }
        }
    }
}