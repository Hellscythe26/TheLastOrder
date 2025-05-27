using UnityEngine;

/// <summary>
/// Gestiona el estado y la interacción de un cofre que puede contener un item.
/// </summary>
public class ChestController : MonoBehaviour
{
    [Header("Estado")]
    [Tooltip("Indica si el cofre ya ha sido abierto.")]
    [SerializeField] private bool isOpen = false; // Estado actual del cofre (abierto/cerrado).
    [Tooltip("El prefab del item que contiene este cofre (asignado por el Spawner).")]
    [SerializeField] private GameObject containedItemPrefab; // Prefab del item que se instanciará al abrir.
    [Header("Configuración Visual/Interactiva")]
    [Tooltip("Sprite a mostrar cuando el cofre está abierto (si no se usa Animator).")]
    [SerializeField] private Sprite openSprite; // Sprite para el estado abierto.
    [Tooltip("Punto relativo (Transform hijo) donde aparecerá el item al abrir el cofre.")]
    [SerializeField] private Transform itemSpawnPoint; // Lugar exacto donde se generará el item.
    [Tooltip("Componente Animator para la animación de abrir el cofre.")]
    [SerializeField] private Animator animator; // Referencia al Animator para la animación de apertura.
    private SpriteRenderer spriteRenderer; // Referencia al SpriteRenderer para cambiar el sprite si no hay animator.
    private const string OPEN_ANIM_TRIGGER = "Open"; // Nombre del parámetro Trigger en el Animator para la animación de abrir.

    /// <summary>
    /// Se llama una vez cuando el script es cargado o un GameObject con el script es instanciado.
    /// Se usa para inicializar referencias a componentes.
    /// </summary>
    private void Awake()
    {
        // Obtiene el componente SpriteRenderer adjunto a este GameObject.
        spriteRenderer = GetComponent<SpriteRenderer>();
        // Si el Animator no fue asignado en el Inspector, intenta obtenerlo del GameObject.
        if (animator == null) animator = GetComponent<Animator>();
        // Si el itemSpawnPoint no fue asignado, usa la propia posición del cofre como fallback.
        if (itemSpawnPoint == null) itemSpawnPoint = transform;
    }

    /// <summary>
    /// Permite que un script externo (como ChestSpawner) establezca qué item contendrá este cofre.
    /// </summary>
    /// <param name="itemPrefab">El GameObject Prefab del item que este cofre guardará.</param>
    public void SetContainedItem(GameObject itemPrefab)
    {
        containedItemPrefab = itemPrefab;
    }

    /// <summary>
    /// Se llama automáticamente por Unity cuando otro Collider2D entra en el trigger de este objeto.
    /// Se usa para detectar la interacción con el jugador.
    /// </summary>
    /// <param name="other">El Collider2D del objeto que entró en el trigger.</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Comprueba si el cofre no está abierto y si el objeto que colisionó es el jugador.
        if (!isOpen && other.CompareTag("Player"))
        {
            // Llama al método para abrir el cofre.
            OpenChest();
        }
    }

    /// <summary>
    /// Lógica para abrir el cofre, cambiar su apariencia y soltar el item contenido.
    /// </summary>
    public void OpenChest()
    {
        // Si el cofre ya está abierto o no tiene un item asignado, no hace nada.
        if (isOpen || containedItemPrefab == null) return;
        isOpen = true; // Marca el cofre como abierto.
        // 1. Cambiar estado visual (Animación o Sprite)
        if (animator != null)
        {
            // Si hay un Animator, dispara el trigger para la animación de abrir.
            animator.SetTrigger(OPEN_ANIM_TRIGGER);
        }
        else if (openSprite != null && spriteRenderer != null)
        {
            // Si no hay Animator pero sí un openSprite, cambia el sprite directamente.
            spriteRenderer.sprite = openSprite;
        }
        // 2. Instanciar el item contenido en la posición de itemSpawnPoint.
        if (itemSpawnPoint != null)
        {
             Instantiate(containedItemPrefab, itemSpawnPoint.position, Quaternion.identity);
        }
        else // Fallback si itemSpawnPoint es null.
        {
             // Instancia el item un poco arriba de la posición del cofre.
             Instantiate(containedItemPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        }
        // 3. Desactivar futura interacción (opcional).
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            // Desactiva el collider del cofre para que no pueda ser interactuado de nuevo.
            col.enabled = false;
        }
    }
}