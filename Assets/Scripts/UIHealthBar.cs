using UnityEngine;
using UnityEngine.UI; // Necesario para interactuar con componentes UI como Image.
using System.Collections.Generic; // Necesario para List<Image>.

/// <summary>
/// Gestiona la visualización de la barra de vida del jugador en la UI.
/// Se suscribe a los eventos de PlayerHealth para actualizar dinámicamente
/// el número de contenedores de corazón y su estado (lleno, medio, vacío).
/// </summary>
public class UIHealthBar : MonoBehaviour
{
    [Tooltip("Referencia al script PlayerHealth del jugador. Se intentará obtener de Player.Instance si se deja vacío.")]
    public PlayerHealth playerHealth; // Referencia al script de vida del jugador.
    [Tooltip("Sprite para un corazón completamente lleno.")]
    public Sprite fullHeart;
    [Tooltip("Sprite para medio corazón.")]
    public Sprite halfHeart;
    [Tooltip("Sprite para un contenedor de corazón vacío.")]
    public Sprite emptyHeart;
    [Tooltip("Prefab del GameObject UI que representa un solo contenedor de corazón. Debe tener un componente Image.")]
    public GameObject heartPrefab; // Prefab para instanciar los corazones en la UI.
    // Lista para almacenar las referencias a los componentes Image de cada corazón instanciado.
    private List<Image> heartImages = new List<Image>();

    /// <summary>
    /// Se llama antes del primer frame de Update.
    /// Busca la referencia a PlayerHealth, valida los prefabs y sprites,
    /// y se suscribe a los eventos de cambio de salud y vida máxima del jugador.
    /// </summary>
    private void Start()
    {
        // Intenta obtener PlayerHealth desde el Singleton Player.Instance si no está asignado.
        if (playerHealth == null)
        {
            if (Player.Instance != null)
            {
                playerHealth = Player.Instance.GetComponent<PlayerHealth>();
                if (playerHealth == null)
                {
                     Debug.LogError("UIHealthBar: ¡No se encontró el componente PlayerHealth en Player.Instance!", this.gameObject);
                     enabled = false; return;
                }
            }
            else
            {
                 Debug.LogError("UIHealthBar: ¡No se encontró Player.Instance! Asegúrate de que el jugador exista y use DontDestroyOnLoad.", this.gameObject);
                 enabled = false; return; // Salir si no hay jugador.
            }
        }
        // Validaciones de configuración.
        if (heartPrefab == null)
        {
            Debug.LogError("UIHealthBar: Heart Prefab no está asignado.", this.gameObject);
            enabled = false; return;
        }
        if (fullHeart == null || halfHeart == null || emptyHeart == null)
        {
            Debug.LogError("UIHealthBar: Uno o más sprites de corazón (Full, Half, Empty) no están asignados.", this.gameObject);
            enabled = false; return;
        }
        // Suscribirse a los eventos de PlayerHealth para actualizar la UI dinámicamente.
        playerHealth.OnHealthChanged.AddListener(UpdateHealthBarSprites);
        playerHealth.OnMaxHealthChanged.AddListener(SetupHearts);
        // Configura inicialmente los contenedores de corazón basados en la vida máxima actual del jugador.
        SetupHearts();
        // La actualización inicial del llenado de los corazones ocurrirá a través del evento OnHealthChanged
        // que PlayerHealth invoca en su propio Start(). Si no, se puede llamar aquí:
        // UpdateHealthBarSprites(playerHealth.CurrentHealth);
    }

    /// <summary>
    /// Reconstruye la visualización de los contenedores de corazón en la UI.
    /// Se llama cuando cambia la vida máxima del jugador (PlayerHealth.OnMaxHealthChanged).
    /// Limpia los corazones existentes y crea nuevos basados en 'playerHealth.CurrentMaxHearts'.
    /// </summary>
    void SetupHearts()
    {
        if (playerHealth == null) return; // Seguridad adicional.
        // Limpia los GameObjects de corazón existentes para evitar duplicados.
        foreach (Transform child in transform) // 'transform' se refiere al transform de este UIHealthBar (el contenedor).
        {
            Destroy(child.gameObject);
        }
        heartImages.Clear(); // Limpia la lista de referencias a Images.

        // Obtiene la capacidad máxima actual de contenedores de corazón del jugador.
        int numberOfHeartContainers = playerHealth.CurrentMaxHearts;

        // Instancia un GameObject de corazón (desde heartPrefab) por cada contenedor de vida máxima.
        for (int i = 0; i < numberOfHeartContainers; i++)
        {
            GameObject heartInstance = Instantiate(heartPrefab, transform); // Instancia como hijo de este objeto.
            heartInstance.name = "HeartContainer_" + i; // Nombre descriptivo.
            Image heartImageComponent = heartInstance.GetComponent<Image>();

            if (heartImageComponent != null)
            {
                heartImages.Add(heartImageComponent); // Añade a la lista para fácil acceso.
                // heartImageComponent.sprite = emptyHeart; // Podría empezar vacío, UpdateHealthBarSprites lo corregirá.
                // heartImageComponent.enabled = true;
            }
             else
            {
                 Debug.LogError("UIHealthBar: El prefab del corazón ('heartPrefab') no tiene un componente Image!", heartInstance);
            }
        }

        // Actualiza inmediatamente los sprites de los corazones para reflejar la vida actual.
        UpdateHealthBarSprites(playerHealth.CurrentHealth);
    }

    /// <summary>
    /// Actualiza los sprites de cada contenedor de corazón (lleno, medio o vacío)
    /// basándose en el valor de la salud actual del jugador.
    /// Se llama cuando la salud del jugador cambia (PlayerHealth.OnHealthChanged).
    /// </summary>
    /// <param name="currentHealthValue">La salud actual del jugador (ej: 2.5f = dos corazones y medio).</param>
    void UpdateHealthBarSprites(float currentHealthValue)
    {
        if (playerHealth == null) return;
        // Itera sobre cada componente Image de corazón que se ha instanciado.
        for (int i = 0; i < heartImages.Count; i++)
        {
            // El valor que representa este corazón si estuviera completamente lleno.
            // Corazón 0 (índice i=0) representa hasta 1.0 de vida.
            // Corazón 1 (índice i=1) representa hasta 2.0 de vida, etc.
            float heartFullValue = i + 1.0f;
            // El valor que representa este corazón si estuviera medio lleno.
            float heartHalfValue = i + 0.5f;
            if (currentHealthValue >= heartFullValue)
            {
                // Si la salud actual es mayor o igual al valor de este corazón lleno, mostrarlo lleno.
                heartImages[i].sprite = fullHeart;
            }
            else if (currentHealthValue >= heartHalfValue)
            {
                // Si la salud actual es mayor o igual al valor de medio corazón, mostrarlo medio lleno.
                heartImages[i].sprite = halfHeart;
            }
            else
            {
                // Si la salud actual es menor que el valor de medio corazón, mostrarlo vacío.
                heartImages[i].sprite = emptyHeart;
            }
        }
    }

    /// <summary>
    /// Se llama cuando el GameObject UIHealthBar es destruido.
    /// Se desuscribe de los eventos de PlayerHealth para prevenir errores y fugas de memoria.
    /// </summary>
    private void OnDestroy()
    {
        // Es importante desuscribirse de los eventos para evitar llamadas a métodos en objetos destruidos.
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged.RemoveListener(UpdateHealthBarSprites);
            playerHealth.OnMaxHealthChanged.RemoveListener(SetupHearts);
        }
    }
}