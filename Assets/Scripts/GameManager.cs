using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // --- Estado del Temporizador ---
    [Header("Timer Settings")]
    [SerializeField] private float startTimeInSeconds = 120f;
    public float CurrentTime { get; private set; } // El tiempo actual ahora vive aquí
    private bool timerIsRunning = false;
    public UnityEvent OnTimerEnd; // El evento de fin de tiempo ahora está aquí

    // --- Otras variables globales podrían ir aquí (puntuación, etc.) ---


    private void Start() // Puedes usar Start en lugar de depender de GlobalTimer
    {
        StartTimer(); // Inicia el reloj cuando el GameManager está listo
    }

    private void Awake()
    {
        // Singleton y Persistencia
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Inicializar estado
        CurrentTime = startTimeInSeconds;
         if (OnTimerEnd == null) OnTimerEnd = new UnityEvent();
        // No iniciar el timer aquí necesariamente, quizás al empezar el nivel
    }

    private void Update()
    {
        // Actualizar el timer si está corriendo
        if (timerIsRunning)
        {
            if (CurrentTime > 0)
            {
                CurrentTime -= Time.deltaTime;
            }
            else
            {
                CurrentTime = 0;
                timerIsRunning = false;
                Debug.Log("GameManager: ¡El tiempo ha terminado!");
                OnTimerEnd.Invoke();
            }
        }
    }

    // --- Métodos para controlar el timer desde fuera ---
    public void StartTimer()
    {
        timerIsRunning = true;
        Debug.Log("GameManager: Timer iniciado.");
    }

    public void PauseTimer()
    {
        timerIsRunning = false;
        Debug.Log("GameManager: Timer pausado.");
    }

    public void ResetTimer()
    {
         CurrentTime = startTimeInSeconds;
         timerIsRunning = false; // O true si debe empezar al resetear
         Debug.Log("GameManager: Timer reseteado.");
    }

     public void AddTime(float secondsToAdd)
    {
         CurrentTime += secondsToAdd;
         Debug.Log($"GameManager: Tiempo añadido: {secondsToAdd}s. Tiempo actual: {CurrentTime}s");
    }
}