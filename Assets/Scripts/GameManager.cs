using UnityEngine;
using UnityEngine.Events; // Necesario para UnityEvent.

/// <summary>
/// Gestiona el estado global del juego, como el tiempo de juego.
/// Implementa un patrón Singleton y persistencia para mantener su estado
/// a través de diferentes escenas.
/// </summary>
public class GameManager : MonoBehaviour
{
    /// <summary>
    /// Instancia estática Singleton de GameManager para acceso global.
    /// </summary>
    public static GameManager Instance { get; private set; }
    [Header("Timer Settings")]
    [Tooltip("Tiempo inicial en segundos con el que comienza el temporizador del juego.")]
    [SerializeField] private float startTimeInSeconds = 120f;
    /// <summary>
    /// Obtiene el tiempo actual restante en segundos.
    /// </summary>
    public float CurrentTime { get; private set; }
    private bool timerIsRunning = false; // Estado interno para controlar si el temporizador está activo.
    [Tooltip("Evento que se dispara cuando el temporizador llega a cero.")]
    public UnityEvent OnTimerEnd; // Evento para notificar a otros sistemas que el tiempo ha terminado.

    /// <summary>
    /// Se llama una vez cuando el script es cargado.
    /// Implementa la lógica Singleton y DontDestroyOnLoad para persistencia.
    /// Inicializa el temporizador y los eventos.
    /// </summary>
    private void Awake()
    {
        // Lógica Singleton para asegurar una única instancia persistente.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destruye duplicados.
            return;
        }
        Instance = this; // Establece esta como la instancia Singleton.
        DontDestroyOnLoad(gameObject); // Hace que este GameManager persista entre escenas.
        // Inicializa el estado del temporizador.
        CurrentTime = startTimeInSeconds;
        if (OnTimerEnd == null) OnTimerEnd = new UnityEvent(); // Asegura que el evento esté inicializado.
    }

    /// <summary>
    /// Se llama antes del primer frame de Update, solo si el script está habilitado.
    /// Inicia el temporizador del juego.
    /// </summary>
    private void Start()
    {
        StartTimer(); // Por defecto, el temporizador comienza cuando el GameManager está listo.
    }

    /// <summary>
    /// Se llama una vez por frame.
    /// Actualiza el temporizador si está corriendo y gestiona el fin del tiempo.
    /// </summary>
    private void Update()
    {
        // Actualiza el temporizador solo si está activo.
        if (timerIsRunning)
        {
            if (CurrentTime > 0)
            {
                CurrentTime -= Time.deltaTime; // Resta el tiempo transcurrido en el frame.
            }
            else
            {
                CurrentTime = 0; // Asegura que el tiempo no sea negativo.
                timerIsRunning = false; // Detiene el temporizador.
                OnTimerEnd.Invoke(); // Dispara el evento de fin de tiempo.
            }
        }
    }

    /// <summary>
    /// Inicia o reanuda el temporizador del juego.
    /// </summary>
    public void StartTimer()
    {
        timerIsRunning = true;
    }

    /// <summary>
    /// Pausa el temporizador del juego.
    /// </summary>
    public void PauseTimer()
    {
        timerIsRunning = false;
    }

    /// <summary>
    /// Resetea el temporizador a su valor inicial (startTimeInSeconds).
    /// Opcionalmente, puede detener o iniciar el temporizador al resetear.
    /// </summary>
    public void ResetTimer()
    {
         CurrentTime = startTimeInSeconds;
         timerIsRunning = false; // Por defecto, lo deja pausado después de resetear.
    }

    /// <summary>
    /// Añade una cantidad de segundos al tiempo actual del temporizador.
    /// </summary>
    /// <param name="secondsToAdd">Segundos a añadir (puede ser negativo para restar).</param>
    public void AddTime(float secondsToAdd)
    {
         CurrentTime += secondsToAdd;
         // Asegurar que no se vuelva negativo si se resta mucho tiempo.
         if (CurrentTime < 0 && secondsToAdd < 0) CurrentTime = 0;
    }
}