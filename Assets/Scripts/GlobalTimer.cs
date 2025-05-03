// GlobalTimer.cs (MODIFICADO)
using UnityEngine;
using TMPro;
// Ya no necesita UnityEngine.Events si el evento se maneja en GameManager

public class GlobalTimer : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Referencia al objeto TextMeshProUGUI para mostrar el tiempo.")]
    [SerializeField] private TextMeshProUGUI timerText; // Necesita la referencia UI de ESTA escena

    // Ya no necesita variables de tiempo (startTimeInSeconds, currentTime, timerIsRunning)
    // Ya no necesita el evento OnTimerEnd

    void Start()
    {
        // Validar UI
        if (timerText == null)
        {
            Debug.LogError("¡Referencia a timerText no asignada en GlobalTimer!", this);
            enabled = false;
            return;
        }

        // Intentar iniciar el timer global LA PRIMERA VEZ que se carga una escena con este script
        // O si el GameManager existe pero el timer no está corriendo (ej. después de resetear)
        if (GameManager.Instance != null) // Asegurarse que el GameManager ya existe
        {
            // Podrías decidir si quieres resetear/iniciar aquí o en otro lado
            // Ejemplo: Iniciar solo si el tiempo está al máximo (recién reseteado o primera vez)
            // if (!GameManager.Instance.timerIsRunning && GameManager.Instance.CurrentTime >= GameManager.Instance.GetStartTime()) {
            //      GameManager.Instance.StartTimer();
            // }
            // O simplemente asegúrate de que se inicie en algún punto (quizás al empezar el nivel)
            // GameManager.Instance.StartTimer(); // Descomenta si quieres que SIEMPRE intente iniciar aquí
        } else {
            Debug.LogWarning("GlobalTimer.Start: GameManager.Instance aún no existe.");
        }

        // Actualizar la UI inmediatamente con el valor del GameManager (si existe)
        UpdateDisplay();

    }

    void Update()
    {
        // Actualizar la UI en cada frame leyendo del GameManager
        UpdateDisplay();
    }

    void UpdateDisplay()
    {
         if (GameManager.Instance != null)
         {
             DisplayTime(GameManager.Instance.CurrentTime);
         }
         else
         {
             timerText.text = "--:--"; // Mostrar si no hay GameManager
         }
    }

    // DisplayTime sigue igual
    void DisplayTime(float timeToDisplay)
    {
        if (timeToDisplay < 0) timeToDisplay = 0;
        float minutes = Mathf.FloorToInt(timeToDisplay / 60);
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    // Ya no necesita StartTimer, PauseTimer, ResetTimer, AddTime, GetCurrentTime
}