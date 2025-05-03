using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Para usar HashSet

public class BattleMusicManager : MonoBehaviour
{
    public static BattleMusicManager Instance { get; private set; } // Singleton Instance

    [SerializeField] private AudioSource battleAudioSource; // Asigna el AudioSource en el Inspector
    [SerializeField] private float fadeDuration = 5.0f; // Duración del fundido para detenerse

    // Un conjunto para llevar la cuenta de los enemigos activos
    private HashSet<GameObject> activeEnemies = new HashSet<GameObject>();

    private Coroutine fadeOutCoroutine = null; // Para controlar la corutina de fade out

    private void Awake()
    {
        // --- Configuración Singleton Sencilla ---
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destruye duplicados
            return;
        }
        Instance = this;
        // DontDestroyOnLoad(gameObject); // Descomenta si necesitas que persista entre escenas

        // --- Obtener AudioSource si no está asignado ---
        if (battleAudioSource == null)
        {
            battleAudioSource = GetComponent<AudioSource>();
        }
        if (battleAudioSource == null)
        {
             Debug.LogError("¡AudioSource no encontrado o asignado en BattleMusicManager!", this);
             enabled = false; // Desactivar script si no hay audio source
             return;
        }
        battleAudioSource.volume = 0; // Empezar con volumen 0
        battleAudioSource.Stop();    // Asegurarse que no esté sonando
    }

    // Llamado por el enemigo cuando empieza a detectar/atacar
    public void RequestBattleMusic(GameObject enemy)
    {
        if (enemy == null || !activeEnemies.Add(enemy)) // Intenta añadir, si ya estaba, no hagas nada
        {
             return; // Ya estaba registrado o es nulo
        }

        Debug.Log($"Enemy {enemy.name} requesting battle music. Active count: {activeEnemies.Count}");

        // Si este es el PRIMER enemigo activo y la música no está sonando/fading in
        if (activeEnemies.Count == 1 && (fadeOutCoroutine != null || !battleAudioSource.isPlaying))
        {
            Debug.Log("Starting Battle Music...");
            // Detener cualquier fade out que estuviera en progreso
            if (fadeOutCoroutine != null)
            {
                StopCoroutine(fadeOutCoroutine);
                fadeOutCoroutine = null;
            }
            // Iniciar fade in (o simplemente play si no quieres fade in)
            // Aquí podrías llamar a una corutina StartCoroutine(FadeIn(fadeDuration));
            // O simplemente empezar a sonar:
            battleAudioSource.volume = 1f; // O el volumen máximo deseado
            battleAudioSource.Play();
        }
    }

    // Llamado por el enemigo cuando deja de detectar, muere, o el jugador muere
    public void ReleaseBattleMusic(GameObject enemy)
    {
        if (enemy == null || !activeEnemies.Remove(enemy)) // Intenta quitar, si no estaba, no hagas nada
        {
            return; // No estaba registrado o es nulo
        }

        Debug.Log($"Enemy {enemy.name} releasing battle music. Active count: {activeEnemies.Count}");

        CheckStopCondition();
    }

    // Llamado por el jugador cuando muere
    public void PlayerDied()
    {
        Debug.Log("Player died, stopping battle music.");
        activeEnemies.Clear(); // Limpiar todos los enemigos activos
        CheckStopCondition();
    }

    // Comprueba si la música debe detenerse
    private void CheckStopCondition()
    {
         // Si ya NO quedan enemigos activos y la música está sonando
        if (activeEnemies.Count == 0 && battleAudioSource.isPlaying && fadeOutCoroutine == null)
        {
            Debug.Log("Last enemy released. Starting fade out...");
            fadeOutCoroutine = StartCoroutine(FadeOutAndStop(fadeDuration));
        }
    }

    // Corutina para bajar el volumen gradualmente y detener
    private IEnumerator FadeOutAndStop(float duration)
    {
        float startVolume = battleAudioSource.volume;
        float timer = 0f;

        while (timer < duration)
        {
            // Calcular nuevo volumen
            battleAudioSource.volume = Mathf.Lerp(startVolume, 0f, timer / duration);
            timer += Time.deltaTime;
            yield return null; // Esperar al siguiente frame
        }

        // Asegurar volumen 0 y detener
        battleAudioSource.volume = 0f;
        battleAudioSource.Stop();
        Debug.Log("Battle Music Stopped after fade out.");
        fadeOutCoroutine = null; // Marcar corutina como terminada
    }

     // Opcional: Corutina para subir el volumen gradualmente
    // private IEnumerator FadeIn(float duration) { ... }

    // Asegúrate de liberar enemigos si se destruyen inesperadamente
    private void OnDestroy() {
        if (Instance == this) {
            Instance = null;
        }
    }
}