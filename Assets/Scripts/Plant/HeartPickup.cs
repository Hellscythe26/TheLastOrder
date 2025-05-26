using UnityEngine;

public class HeartPickup : MonoBehaviour
{
    [SerializeField] private float healAmount = 0.5f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null && playerHealth.IsAlive())
            {
                Debug.Log($"Player recogió corazón. Curando {healAmount}.");
                playerHealth.Heal(healAmount);
                Destroy(gameObject);
            }
        }
    }
}