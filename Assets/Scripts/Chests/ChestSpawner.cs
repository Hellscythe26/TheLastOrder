using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ChestSpawner : MonoBehaviour
{
    [Header("Configuración General")]
    [SerializeField] private Transform[] potentialSpawnPoints;
    [SerializeField] private GameObject chestPrefab;
    [SerializeField] private int minChests = 3;
    [SerializeField] private int maxChests = 7;
    [Header("Contenido de Cofres")]
    [SerializeField] private GameObject heartContainerPrefab;
    [SerializeField] private GameObject applePrefab;
    [Header("Probabilidades de Transición de Contenido (Markov)")]
    [Range(0f, 1f)] public float probAppleGivenApple = 0.7f;
    [Range(0f, 1f)] public float probContainerGivenApple = 0.3f;
    [Range(0f, 1f)] public float probAppleGivenContainer = 0.9f;
    [Range(0f, 1f)] public float probContainerGivenContainer = 0.1f;
    [Range(0f, 1f)] public float probAppleGivenNone = 0.6f;
    [Range(0f, 1f)] public float probContainerGivenNone = 0.4f;
    [Header("Configuración LCG para ChestSpawner")]
    [SerializeField] private long baseSeedForLCG = 11223;
    [SerializeField] private long lcgMultiplier = 1664525;
    [SerializeField] private long lcgIncrement = 1013904223;
    [SerializeField] private long lcgModulus = 2147483647;
    [SerializeField] private int numSamplesForLCG = 50;
    [SerializeField] private double lcgAlphaTestLevel = 0.05;
    public enum ItemChestState { None, Apple, HeartContainer }
    private LCGManager lcgManager;
    private List<float> availableRandomNumbers;
    private int currentRandomNumberIndex = 0;
    private bool lcgForSpawnerInitialized = false;
    private Markov<ItemChestState> itemMarkovChain;

    void Awake()
    {
        long instanceSeed = System.DateTime.Now.Ticks + gameObject.GetInstanceID() + baseSeedForLCG;
        lcgManager = new LCGManager(instanceSeed, lcgMultiplier, lcgIncrement, lcgModulus, lcgAlphaTestLevel);
        availableRandomNumbers = lcgManager.GetValidatedRiNumbers(numSamplesForLCG, out lcgForSpawnerInitialized);
        if (!lcgForSpawnerInitialized || availableRandomNumbers.Count == 0)
        {
            Debug.LogError("ChestSpawner: Falló la inicialización del LCG interno. Las decisiones aleatorias usarán Random.value() como fallback.", this);
        }

        InitializeItemMarkovChain();
    }

    void Start()
    {
        ValidateProbabilities();
        SpawnChests();
    }

    private float GetNextLCGNumberForMarkov()
    {
        if (lcgForSpawnerInitialized && availableRandomNumbers.Count > 0)
        {
            float num = availableRandomNumbers[currentRandomNumberIndex];
            currentRandomNumberIndex = (currentRandomNumberIndex + 1) % availableRandomNumbers.Count;
            return num;
        }
        Debug.LogWarning("ChestSpawner: LCG interno no disponible para Markov, usando Random.value().");
        return Random.value;
    }

    void InitializeItemMarkovChain()
    {
        var transitionMatrix = new Dictionary<ItemChestState, Dictionary<ItemChestState, float>>
        {
            {
                ItemChestState.None, new Dictionary<ItemChestState, float>
                {
                    { ItemChestState.Apple, probAppleGivenNone },
                    { ItemChestState.HeartContainer, probContainerGivenNone }
                }
            },
            {
                ItemChestState.Apple, new Dictionary<ItemChestState, float>
                {
                    { ItemChestState.Apple, probAppleGivenApple },
                    { ItemChestState.HeartContainer, probContainerGivenApple }
                }
            },
            {
                ItemChestState.HeartContainer, new Dictionary<ItemChestState, float>
                {
                    { ItemChestState.Apple, probAppleGivenContainer },
                    { ItemChestState.HeartContainer, probContainerGivenContainer }
                }
            }
        };
        itemMarkovChain = new Markov<ItemChestState>(ItemChestState.None, transitionMatrix, GetNextLCGNumberForMarkov);
    }

    void ValidateProbabilities()
    {
        if (!Mathf.Approximately(probAppleGivenNone + probContainerGivenNone, 1.0f))
            Debug.LogWarning("Probabilidades Markov para estado 'None' no suman 1.0!");
        if (!Mathf.Approximately(probAppleGivenApple + probContainerGivenApple, 1.0f))
            Debug.LogWarning("Probabilidades Markov para estado 'Apple' no suman 1.0!");
        if (!Mathf.Approximately(probAppleGivenContainer + probContainerGivenContainer, 1.0f))
            Debug.LogWarning("Probabilidades Markov para estado 'HeartContainer' no suman 1.0!");
    }

    private float GetNextGeneralRandomNumber()
    {
        if (lcgForSpawnerInitialized && availableRandomNumbers.Count > 0)
        {
            float num = availableRandomNumbers[currentRandomNumberIndex];
            currentRandomNumberIndex = (currentRandomNumberIndex + 1) % availableRandomNumbers.Count;
            return num;
        }
        Debug.LogWarning("ChestSpawner: LCG interno no disponible, usando Random.value() para decisión general.");
        return Random.value;
    }

    void SpawnChests()
    {
        if (potentialSpawnPoints.Length == 0 || chestPrefab == null || heartContainerPrefab == null || applePrefab == null)
        {
            Debug.LogError("Faltan referencias en ChestSpawner. No se generarán cofres.");
            return;
        }
        float randomForCount = GetNextGeneralRandomNumber();
        int numChestsToSpawn = minChests + Mathf.FloorToInt(randomForCount * (maxChests - minChests + 1));
        numChestsToSpawn = Mathf.Clamp(numChestsToSpawn, minChests, maxChests);
        numChestsToSpawn = Mathf.Min(numChestsToSpawn, potentialSpawnPoints.Length);
        Debug.Log($"Intentando generar {numChestsToSpawn} cofres...");
        List<Transform> chosenSpawnPoints = SelectRandomUniqueSpawnPoints(numChestsToSpawn);
        foreach (Transform spawnPoint in chosenSpawnPoints)
        {
            ItemChestState nextItemState = itemMarkovChain.GetNextState();
            GameObject itemToContain = null;
            switch (nextItemState)
            {
                case ItemChestState.Apple:
                    itemToContain = applePrefab;
                    break;
                case ItemChestState.HeartContainer:
                    itemToContain = heartContainerPrefab;
                    break;
                default:
                    Debug.LogError("Estado de item Markov inesperado o no manejado.");
                    itemToContain = applePrefab;
                    break;
            }
            GameObject chestInstance = Instantiate(chestPrefab, spawnPoint.position, spawnPoint.rotation);
            ChestController chestController = chestInstance.GetComponent<ChestController>();
            if (chestController != null)
            {
                chestController.SetContainedItem(itemToContain);
            }
            else
            {
                Debug.LogError($"El prefab del cofre ({chestPrefab.name}) no tiene el script ChestController!");
                Destroy(chestInstance);
            }
        }
        Debug.Log($"Generados {chosenSpawnPoints.Count} cofres.");
    }

    private List<Transform> SelectRandomUniqueSpawnPoints(int count)
    {
        List<Transform> allPoints = new List<Transform>(potentialSpawnPoints);
        List<Transform> chosenPoints = new List<Transform>();
        for (int i = 0; i < count; i++)
        {
            if (allPoints.Count == 0) break;
            float randomForIndex = GetNextGeneralRandomNumber();
            int randomIndex = Mathf.FloorToInt(randomForIndex * allPoints.Count);
            randomIndex = Mathf.Clamp(randomIndex, 0, allPoints.Count - 1);
            chosenPoints.Add(allPoints[randomIndex]);
            allPoints.RemoveAt(randomIndex);
        }
        return chosenPoints;
    }
}