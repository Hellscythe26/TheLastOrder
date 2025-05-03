// ChestController.cs
using UnityEngine;

public class ChestController : MonoBehaviour
{
    [Header("Estado")]
    [SerializeField] private bool isOpen = false;
    [Tooltip("El prefab del item que contiene este cofre (asignado por el Spawner)")]
    [SerializeField] private GameObject containedItemPrefab; // Asignado por ChestSpawner

    [Header("Configuración Visual/Interactiva")]
    [Tooltip("Sprite a mostrar cuando el cofre está abierto")]
    [SerializeField] private Sprite openSprite;
    [Tooltip("Punto relativo donde aparecerá el item al abrir")]
    [SerializeField] private Transform itemSpawnPoint; // Un GameObject hijo vacío un poco arriba del cofre
    [Tooltip("Componente Animator para la animación de abrir")]
    [SerializeField] private Animator animator;

    private SpriteRenderer spriteRenderer;
    private const string OPEN_ANIM_TRIGGER = "Open"; // Nombre del trigger en tu Animator

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        // Intenta obtener Animator si no está asignado
        if (animator == null) animator = GetComponent<Animator>();
        // Asegúrate de que el punto de spawn exista, si no, usa la posición del cofre
        if (itemSpawnPoint == null) itemSpawnPoint = transform;
    }

    // Método para que el Spawner le diga qué item guardar
    public void SetContainedItem(GameObject itemPrefab)
    {
        containedItemPrefab = itemPrefab;
    }

    // --- Lógica de Interacción (Ejemplo: Al entrar en Trigger) ---
    // Podrías cambiar esto para que funcione con un botón de acción
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isOpen && other.CompareTag("Player")) // Asume que el jugador tiene el Tag "Player"
        {
            Debug.Log("Jugador interactuó con el cofre.");
            OpenChest();
        }
    }

    // Método para abrir el cofre
    public void OpenChest()
    {
        if (isOpen || containedItemPrefab == null) return; // Ya abierto o no configurado

        isOpen = true;
        Debug.Log($"Abriendo cofre, contiene: {containedItemPrefab.name}");

        // 1. Cambiar estado visual (Animación o Sprite)
        if (animator != null)
        {
            animator.SetTrigger(OPEN_ANIM_TRIGGER);
        }
        else if (openSprite != null && spriteRenderer != null)
        {
            // Alternativa si no hay Animator: Cambiar directamente el sprite
            spriteRenderer.sprite = openSprite;
        }

        // 2. Instanciar el item contenido
        if (itemSpawnPoint != null)
        {
             Instantiate(containedItemPrefab, itemSpawnPoint.position, Quaternion.identity);
        }
        else // Fallback por si acaso
        {
             Instantiate(containedItemPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity); // Un poco arriba
        }


        // 3. Desactivar futura interacción (opcional)
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false; // Desactiva el collider para que no se pueda volver a activar
        }
        // O podrías destruir el cofre después de un tiempo, o simplemente dejarlo abierto visualmente
        // Destroy(gameObject, 2f); // Ejemplo
    }
}