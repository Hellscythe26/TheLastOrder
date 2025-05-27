using UnityEngine;
using TMPro; // Necesario para TextMeshProUGUI.

/// <summary>
/// Actualiza un elemento de UI TextMeshPro para mostrar el tiempo de juego
/// obtenido del GameManager (Singleton).
/// Este script es un presentador para el tiempo global.
/// </summary>
public class GlobalTimer : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Referencia al objeto TextMeshProUGUI en la UI donde se mostrará el tiempo.")]
    [SerializeField] private TextMeshProUGUI timerText; // Componente UI para el texto del temporizador.

    /// <summary>
    /// Se llama antes del primer frame de Update.
    /// Valida la referencia al TextMeshPro y actualiza la pantalla inicialmente.
    /// </summary>
    void Start()
    {
        // Valida que el componente de texto esté asignado.
        if (timerText == null)
        {
            Debug.LogError("¡Referencia a timerText no asignada en GlobalTimer! El temporizador no se mostrará.", this);
            enabled = false; // Desactiva el script si falta la referencia crucial.
            return;
        }
        // Comprueba si GameManager existe. El GameManager es responsable de iniciar su propio temporizador.
        if (GameManager.Instance == null) {
        }
        // Actualiza la UI inmediatamente con el valor actual del GameManager (si existe).
        UpdateDisplay();
    }

    /// <summary>
    /// Se llama una vez por frame.
    /// Actualiza continuamente la visualización del tiempo en la UI.
    /// </summary>
    void Update()
    {
        // Actualiza la UI en cada frame leyendo el tiempo del GameManager.
        UpdateDisplay();
    }

    /// <summary>
    /// Actualiza el componente TextMeshProUGUI con el tiempo actual del GameManager.
    /// Si GameManager no existe, muestra un placeholder.
    /// </summary>
    void UpdateDisplay()
    {
         if (GameManager.Instance != null) // Obtiene el tiempo del GameManager (Singleton).
         {
             DisplayTime(GameManager.Instance.CurrentTime);
         }
         else // Si no hay GameManager, muestra un texto por defecto.
         {
             if(timerText != null) timerText.text = "--:--";
         }
    }

    /// <summary>
    /// Formatea el tiempo (en segundos) a un formato MM:SS y lo muestra en el TextMeshProUGUI.
    /// </summary>
    /// <param name="timeToDisplay">El tiempo en segundos a mostrar.</param>
    void DisplayTime(float timeToDisplay)
    {
        // Asegura que el tiempo no sea negativo para la visualización.
        if (timeToDisplay < 0) timeToDisplay = 0;
        // Calcula minutos y segundos.
        float minutes = Mathf.FloorToInt(timeToDisplay / 60);
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);
        // Actualiza el texto con el formato MM:SS.
        if(timerText != null) timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}