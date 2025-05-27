using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Para usar HashSet

/// <summary>
/// Gestiona la reproducción de la música de batalla.
/// La música comienza cuando un enemigo la solicita (al detectar al jugador)
/// y se detiene con un fundido cuando ya no hay enemigos activos solicitándola.
/// Utiliza un patrón Singleton para acceso global.
/// </summary>
public class BattleMusicManager : MonoBehaviour
{
    /// <summary>
    /// Instancia estática Singleton de BattleMusicManager.
    /// </summary>
    public static BattleMusicManager Instance { get; private set; }
    [Tooltip("El componente AudioSource que reproducirá la música de batalla.")]
    [SerializeField] private AudioSource battleAudioSource;
    [Tooltip("Duración en segundos del fundido de entrada/salida de la música.")]
    [SerializeField] private float fadeDuration = 1.0f; // Ajustado a 1.0s como en la versión previa del script.
    // Un conjunto para llevar la cuenta de los GameObjects enemigos que actualmente
    // requieren que la música de batalla esté sonando.
    private HashSet<GameObject> activeEnemies = new HashSet<GameObject>();
    // Referencias a las corutinas de fundido para poder detenerlas si es necesario.
    private Coroutine fadeOutCoroutine = null;
    private Coroutine fadeInCoroutine = null;
    private bool isRoomBattleActive = false;

    /// <summary>
    /// Se llama una vez cuando el script es cargado.
    /// Implementa la lógica Singleton y configura el AudioSource.
    /// </summary>
    private void Awake()
    {
        // Configuración Singleton.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destruye esta instancia si ya existe otra.
            return;
        }
        Instance = this;
        // DontDestroyOnLoad(gameObject); // Descomentar si se necesita que persista entre escenas.
                                        // En el script original estaba comentado.

        // Obtiene el AudioSource si no está asignado en el Inspector.
        if (battleAudioSource == null)
        {
            battleAudioSource = GetComponent<AudioSource>();
        }
        if (battleAudioSource == null)
        {
             Debug.LogError("¡AudioSource no encontrado o asignado en BattleMusicManager!", this);
             enabled = false; // Desactiva el script si no hay AudioSource.
             return;
        }
        // Inicia con volumen 0 y detenida, para controlarla con fundidos.
        battleAudioSource.volume = 0;
        battleAudioSource.Stop();
    }

    /// <summary>
    /// Llamado por un enemigo cuando comienza a detectar o atacar al jugador,
    /// solicitando que la música de batalla comience o continúe.
    /// </summary>
    /// <param name="enemy">El GameObject del enemigo que solicita la música.</param>
    public void RequestBattleMusic(GameObject enemy)
    {
        // Si el enemigo es nulo o ya está en el conjunto de enemigos activos, no hacer nada.
        if (enemy == null || !activeEnemies.Add(enemy))
        {
             return;
        }
        // Si no hay una batalla de sala forzando la música y este es el primer enemigo en solicitarla,
        // o si la música estaba en proceso de fundido de salida, iniciar música con fundido de entrada.
        if (!isRoomBattleActive && activeEnemies.Count == 1)
        {
            PlayBattleMusicWithFadeIn();
        }
    }

    /// <summary>
    /// Llamado por un enemigo cuando deja de detectar al jugador, muere, o es desactivado.
    /// Libera la solicitud de música de batalla por parte de ese enemigo.
    /// </summary>
    /// <param name="enemy">El GameObject del enemigo que libera la música.</param>
    public void ReleaseBattleMusic(GameObject enemy)
    {
        // Si el enemigo es nulo o no estaba en el conjunto de enemigos activos, no hacer nada.
        if (enemy == null || !activeEnemies.Remove(enemy))
        {
            return;
        }
        // Solo comprobar si se debe detener la música si no hay una batalla de sala activa.
        if (!isRoomBattleActive)
        {
            CheckStopCondition();
        }
    }

    /// <summary>
    /// Llamado por RoomController para indicar que una "batalla de sala" ha comenzado.
    /// Fuerza el inicio de la música de batalla.
    /// </summary>
    public void RoomEntered()
    {
        isRoomBattleActive = true;
        PlayBattleMusicWithFadeIn();
    }

    /// <summary>
    /// Llamado por RoomController para indicar que una "batalla de sala" ha terminado (todos los enemigos derrotados).
    /// Permite que la música se detenga si no hay otras solicitudes.
    /// </summary>
    public void RoomCleared()
    {
        isRoomBattleActive = false;
        // Comprueba si la música debe detenerse (si no hay más enemigos individuales activos).
        CheckStopCondition();
    }
    // --- Fin Métodos para RoomController ---

    /// <summary>
    /// Llamado externamente (por PlayerHealth o Player) cuando el jugador muere.
    /// Detiene la música de batalla.
    /// </summary>
    public void PlayerDied()
    {
        isRoomBattleActive = false; // La batalla de sala termina.
        activeEnemies.Clear(); // Limpia todas las solicitudes de enemigos.
        CheckStopCondition(); // Intenta detener la música.
    }

    /// <summary>
    /// Inicia la reproducción de la música de batalla con un efecto de fundido de entrada.
    /// Detiene cualquier fundido de salida que pudiera estar en progreso.
    /// </summary>
    private void PlayBattleMusicWithFadeIn()
    {
        // Si hay un fundido de salida en progreso, detenerlo.
        if (fadeOutCoroutine != null)
        {
            StopCoroutine(fadeOutCoroutine);
            fadeOutCoroutine = null;
        }
        // Si ya hay un fundido de entrada o la música ya suena a volumen máximo, no hacer nada.
        if (fadeInCoroutine != null || (battleAudioSource.isPlaying && Mathf.Approximately(battleAudioSource.volume, 1f)))
        {
            return;
        }
        // Inicia la corutina de fundido de entrada.
        fadeInCoroutine = StartCoroutine(FadeIn(fadeDuration));
    }

    /// <summary>
    /// Comprueba si se cumplen las condiciones para detener la música de batalla
    /// (ningún enemigo activo Y ninguna batalla de sala activa).
    /// Si se cumplen, inicia el fundido de salida.
    /// </summary>
    private void CheckStopCondition()
    {
        // Si no hay enemigos activos, ni una batalla de sala forzando la música,
        // la música está sonando y no hay ya un fundido de salida en progreso...
        if (activeEnemies.Count == 0 && !isRoomBattleActive && battleAudioSource.isPlaying && fadeOutCoroutine == null)
        {
            // Detener cualquier fundido de entrada que pudiera estar en progreso.
            if (fadeInCoroutine != null)
            {
                StopCoroutine(fadeInCoroutine);
                fadeInCoroutine = null;
            }
            // Inicia la corutina de fundido de salida.
            fadeOutCoroutine = StartCoroutine(FadeOutAndStop(fadeDuration));
        }
    }

    /// <summary>
    /// Corutina para aumentar gradualmente el volumen de la música de batalla hasta el máximo.
    /// </summary>
    /// <param name="duration">Duración del fundido de entrada en segundos.</param>
    private IEnumerator FadeIn(float duration)
    {
        // Si la música no está sonando, la inicia desde volumen 0.
        if (!battleAudioSource.isPlaying)
        {
            battleAudioSource.volume = 0f;
            battleAudioSource.Play();
        }
        float startVolume = battleAudioSource.volume; // Volumen actual desde el que empezar el fundido.
        float timer = 0f;
        // Bucle hasta que se complete la duración del fundido.
        while (timer < duration)
        {
            battleAudioSource.volume = Mathf.Lerp(startVolume, 1f, timer / duration); // Interpola el volumen.
            timer += Time.deltaTime; // Avanza el temporizador.
            yield return null; // Espera al siguiente frame.
        }
        battleAudioSource.volume = 1f; // Asegura que el volumen sea exactamente 1 al final.
        fadeInCoroutine = null; // Limpia la referencia a la corutina.
    }

    /// <summary>
    /// Corutina para disminuir gradualmente el volumen de la música de batalla y detenerla.
    /// </summary>
    /// <param name="duration">Duración del fundido de salida en segundos.</param>
    private IEnumerator FadeOutAndStop(float duration)
    {
        float startVolume = battleAudioSource.volume; // Volumen actual desde el que empezar el fundido.
        float timer = 0f;
        // Bucle hasta que se complete la duración del fundido.
        while (timer < duration)
        {
            battleAudioSource.volume = Mathf.Lerp(startVolume, 0f, timer / duration); // Interpola el volumen.
            timer += Time.deltaTime; // Avanza el temporizador.
            yield return null; // Espera al siguiente frame.
        }

        battleAudioSource.volume = 0f; // Asegura que el volumen sea exactamente 0.
        battleAudioSource.Stop();      // Detiene la reproducción.
        fadeOutCoroutine = null; // Limpia la referencia a la corutina.
    }

    /// <summary>
    /// Se llama cuando el GameObject es destruido.
    /// Limpia la instancia Singleton si esta era la instancia.
    /// </summary>
    private void OnDestroy() {
        // Si esta es la instancia Singleton, la limpia para permitir una nueva si el objeto se recrea.
        if (Instance == this) {
            Instance = null;
        }
    }
}