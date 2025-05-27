using UnityEngine;
using UnityEngine.UI; // Necesario para trabajar con componentes UI como Image.
using System.Collections.Generic; // Para List<float>

/// <summary>
/// Cambia aleatoriamente el sprite de fondo del menú principal al iniciar.
/// Utiliza el LCGManager para la selección aleatoria del índice del sprite.
/// </summary>
public class MainMenuBackgroundChanger : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Arrastra aquí el componente Image del fondo del menú principal.")]
    [SerializeField] private Image backgroundImageComponent;
    [Header("Background Options")]
    [Tooltip("Arrastra aquí los Sprites que quieres usar como fondo.")]
    [SerializeField] private Sprite[] possibleBackgrounds;
    [Header("Configuración LCG para Selección de Fondo")]
    // Parámetros para el LCG que este script utilizará.
    [Tooltip("Semilla base, se combinará con valores dinámicos para esta instancia.")]
    [SerializeField] private long baseSeedForLCG = 42; // Una semilla diferente para el menú.
    [SerializeField] private long lcgMultiplier = 1664525;
    [SerializeField] private long lcgIncrement = 1013904223;
    [SerializeField] private long lcgModulus = 2147483647;
    [Tooltip("Cantidad de números a generar por LCG. Para una sola selección, 1 es suficiente si se confía en los parámetros, o más para asegurar pruebas.")]
    [SerializeField] private int numSamplesForLCG = 10; // Generar pocos, solo necesitamos uno bueno.
    [Tooltip("Nivel Alpha para las pruebas estadísticas del LCG.")]
    [SerializeField] private double lcgAlphaTestLevel = 0.05;
    // Se usa el generador de números LCGManager.
    private LCGManager lcgManager;
    private bool lcgInitialized = false;
    private List<float> lcgNumbersForSelection;
    private int currentLCGNumberIndex = 0;

    /// <summary>
    /// Se llama una vez cuando el script es cargado, antes de Start.
    /// Ideal para configurar el estado inicial y seleccionar el fondo.
    /// </summary>
    void Awake()
    {
        // --- Verificaciones iniciales de configuración ---
        if (backgroundImageComponent == null)
        {
            Debug.LogError("MainMenuBackgroundChanger: No se ha asignado el componente Image del fondo en el Inspector.", this.gameObject);
            enabled = false; // Desactivar script si falta configuración crítica.
            return;
        }
        if (possibleBackgrounds == null || possibleBackgrounds.Length == 0)
        {
            Debug.LogError("MainMenuBackgroundChanger: No se han asignado Sprites de fondo en el array 'Possible Backgrounds'.", this.gameObject);
            enabled = false;
            return;
        }
        // --- Inicialización del LCGManager ---
        // Se usa el generador de números LCGManager: Inicialización.
        long instanceSeed = System.DateTime.Now.Ticks + gameObject.GetInstanceID() + baseSeedForLCG;
        lcgManager = new LCGManager(instanceSeed, lcgMultiplier, lcgIncrement, lcgModulus, lcgAlphaTestLevel);
        lcgNumbersForSelection = lcgManager.GetValidatedRiNumbers(numSamplesForLCG, out lcgInitialized);

        if (!lcgInitialized || lcgNumbersForSelection.Count == 0)
        {
            Debug.LogWarning("MainMenuBackgroundChanger: Falló la inicialización del LCG. Se usará Random.Range() como fallback para seleccionar el fondo.", this);
            // No desactivamos el script, solo usaremos el Random de Unity si LCG falla.
        }
        // --- Lógica de Selección de Fondo usando LCG (o fallback) ---
        SelectAndApplyBackground();
    }

    /// <summary>
    /// Selecciona un sprite de fondo aleatoriamente usando el LCGManager (o Random.Range como fallback)
    /// y lo aplica al componente Image.
    /// </summary>
    private void SelectAndApplyBackground()
    {
        int randomIndex;
        if (lcgInitialized && lcgNumbersForSelection.Count > 0)
        {
            // Se usa el generador de números LCGManager: Obtención de número para seleccionar índice.
            float randomValueLCG = GetNextLCGNumber();
            // Mapea el número LCG [0,1) a un índice válido [0, possibleBackgrounds.Length - 1].
            randomIndex = Mathf.FloorToInt(randomValueLCG * possibleBackgrounds.Length);
            // Asegura que el índice esté dentro de los límites (especialmente si randomValueLCG es 1.0, lo cual es raro pero posible con algunos LCG).
            randomIndex = Mathf.Clamp(randomIndex, 0, possibleBackgrounds.Length - 1);
        }
        else
        {
            // Fallback al generador de Unity si el LCG no está disponible.
            randomIndex = Random.Range(0, possibleBackgrounds.Length);
            Debug.LogWarning("MainMenuBackgroundChanger: Usando Random.Range de Unity para seleccionar fondo debido a fallo de inicialización LCG.");
        }
        // Selecciona el Sprite usando el índice aleatorio.
        Sprite chosenSprite = possibleBackgrounds[randomIndex];
        // Asigna el Sprite elegido al componente Image del fondo.
        backgroundImageComponent.sprite = chosenSprite;
    }

    /// <summary>
    /// Obtiene el siguiente número pseudoaleatorio de la secuencia generada por LCGManager.
    /// </summary>
    /// <returns>Un float entre 0.0 y 1.0.</returns>
    private float GetNextLCGNumber()
    {
        // Este método asume que lcgInitialized es true y lcgNumbersForSelection no está vacío.
        // La comprobación ya se hace antes de llamar.
        float num = lcgNumbersForSelection[currentLCGNumberIndex];
        currentLCGNumberIndex = (currentLCGNumberIndex + 1) % lcgNumbersForSelection.Count; // Cicla
        return num;
    }
}