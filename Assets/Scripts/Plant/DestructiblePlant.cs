using UnityEngine;
using System.Collections.Generic;

public class DestructiblePlant : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    [Tooltip("Cuántos golpes necesita para destruirse (o cuánta vida tiene)")]
    [SerializeField] private float health = 1f;
    [Header("Drops")]
    [Tooltip("Arrastra aquí el Prefab del HeartPickup")]
    [SerializeField] private GameObject heartPickupPrefab;
    [Tooltip("Probabilidad (0.0 a 1.0) de soltar el corazón al destruirse")]
    [Range(0f, 1f)]
    [SerializeField] private float heartDropChance = 0.5f;
    [Header("LCG Parameters (para la decisión de drop)")]
    [Tooltip("Semilla base, se aleatorizará si es necesario para cada instancia.")]
    [SerializeField] private long baseSeedForLCG = 54321;
    [SerializeField] private long lcgMultiplier = 1664525;
    [SerializeField] private long lcgIncrement = 1013904223;
    [SerializeField] private long lcgModulus = 2147483647;
    [Tooltip("Cuántos números Ri generar y probar. Para una sola decisión de drop, 1 es técnicamente suficiente si confías en los parámetros, o un número mayor (ej. 10) para asegurar que las pruebas del LCGManager pasen si las usas estrictamente.")]
    [SerializeField] private int numSamplesForLCG = 10;
    [Tooltip("Nivel Alpha para las pruebas estadísticas (ej: 0.05).")]
    [SerializeField] private double lcgAlphaTestLevel = 0.05;
    private bool isAlive = true;
    private LCGManager lcgManager;
    private List<float> availableRandomNumbers;
    private int currentRandomNumberIndex = 0;
    private bool lcgInitialized = false;

    private void Awake()
    {
        long instanceSeed = System.DateTime.Now.Ticks + gameObject.GetInstanceID() + baseSeedForLCG;
        lcgManager = new LCGManager(
            instanceSeed,
            lcgMultiplier,
            lcgIncrement,
            lcgModulus,
            lcgAlphaTestLevel
        );
        availableRandomNumbers = lcgManager.GetValidatedRiNumbers(
            numSamplesForLCG,
            out bool generationSucceeded
        );
        if (generationSucceeded && availableRandomNumbers != null && availableRandomNumbers.Count > 0)
        {
            lcgInitialized = true;
        }
        else
        {
            lcgInitialized = false;
            Debug.LogError($"DestructiblePlant {gameObject.name}: Falló la inicialización del LCG. La decisión de drop usará Random.value() como fallback.", this);
        }
    }

    private float GetNextLCGRandomNumber()
    {
        if (!lcgInitialized || availableRandomNumbers == null || availableRandomNumbers.Count == 0)
        {
            Debug.LogWarning($"DestructiblePlant {gameObject.name}: LCG no inicializado o sin números. Usando Random.value() como fallback.");
            return Random.value;
        }
        float randomNumber = availableRandomNumbers[currentRandomNumberIndex];
        currentRandomNumberIndex = (currentRandomNumberIndex + 1) % availableRandomNumbers.Count;
        return randomNumber;
    }

    public void TakeDamage(float damage)
    {
        if (!isAlive) return;
        health -= damage;
        if (health <= 0)
        {
            Die();
        }
    }

    public bool IsAlive()
    {
        return isAlive;
    }

    private void Die()
    {
        if (!isAlive) return;
        isAlive = false;
        if (heartPickupPrefab != null) //
        {
            float randomValue = GetNextLCGRandomNumber();
            if (randomValue <= heartDropChance) //
            {
                Instantiate(heartPickupPrefab, transform.position, Quaternion.identity); //
            }
        }
        Destroy(gameObject);
    }
}