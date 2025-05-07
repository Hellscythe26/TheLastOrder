using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }
    [Header("Seguimiento del Objetivo")]
    public Transform objective; // El jugador al que sigue
    public float camraVelocity = 1f; // Velocidad de suavizado
    public Vector3 scrolling; // Desplazamiento/offset respecto al objetivo
    [Header("Auto-Find Player (Si 'Objective' es null)")]
    [Tooltip("Tag del objeto Jugador a buscar.")]
    [SerializeField] private string playerTag = "Player";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Se encontró una instancia duplicada de CameraController. Destruyendo duplicado.", gameObject);
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (objective == null)
        {
            FindAndSetPlayerObjective();
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindAndSetPlayerObjective();
    }

    public void FindAndSetPlayerObjective()
    {
        if (objective != null) return;
        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject != null)
        {
            objective = playerObject.transform;
            Debug.Log($"CameraController ({gameObject.name}) asignó al jugador (Tag: {playerTag}) como objetivo.");
        }
        else
        {
            Debug.LogWarning($"CameraController ({gameObject.name}) NO PUDO encontrar un GameObject con el Tag '{playerTag}'. El seguimiento no funcionará hasta que se asigne un objetivo.", this);
        }
    }

    private void LateUpdate()
    {
        if (objective == null) return;
        Vector3 desiredPosition = objective.position + scrolling;
        desiredPosition.z = transform.position.z;
        transform.position = desiredPosition;
    }
}