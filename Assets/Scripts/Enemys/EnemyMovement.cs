using UnityEngine;
using System.Collections.Generic; // Para List<>
// using System.IO; // Ya no se usa
// using System.Globalization; // Ya no se usa directamente aquí
using System; // Necesario para Math y DateTime
using System.Linq; // Necesario para .Average() y .Sum() en las pruebas
using MathNet.Numerics.Distributions; // ¡Necesario para las pruebas!

[RequireComponent(typeof(Rigidbody2D), typeof(EnemyHealth), typeof(Animator))]
public class EnemyMovement : MonoBehaviour
{
    [Header("Movement Stats")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float detectionRadius = 8f;

    // ELIMINADO: Configuración de archivo
    // [Header("Random Walk Config")]
    // [SerializeField] private string riFileName = "guard_random_steps";

    [Header("Random Walk Config")] // Mantenemos Step Duration y Speed
    [Tooltip("Duración en segundos de cada 'paso' aleatorio")]
    [SerializeField] private float stepDuration = 1.5f;
    [Tooltip("Velocidad durante la caminata aleatoria")]
    [SerializeField] private float randomWalkSpeed = 1.5f;

    [Header("LCG Parameters for Random Walk")]
    [Tooltip("Semilla inicial (será aleatorizada en Awake).")]
    public long seed = 12345; // x0 - Valor por defecto, se cambiará en Awake
    [Tooltip("Multiplicador (a) del LCG.")]
    public long multiplier = 1103515245; // a
    [Tooltip("Incremento (c) del LCG.")]
    public long increment = 12345; // c
    [Tooltip("Módulo (m) del LCG (e.g., 2^31 = 2147483648). Debe ser > 0.")]
    public long modulus = 2147483648; // m

    [Header("LCG Test Parameters")]
    [Tooltip("Cuántos números generar y validar para la secuencia.")]
    public int numSamples = 100; // Puedes ajustar esto
    [Tooltip("Nivel de significancia para las pruebas (e.g., 0.05).")]
    public double alpha = 0.05;

    [Header("Component References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private EnemyHealth health;
    [SerializeField] private Animator animator;

    // Referencias Internas
    private Transform playerTransform;
    private IDamageable playerDamageable;
    private bool canMove = true;
    private bool playerDetected = false;

    // Estado Caminata Aleatoria
    private List<float> riValues = new List<float>(); // Almacenará los números generados válidos
    private int currentRiIndex = 0;
    private Vector2 currentMoveDirection = Vector2.zero;
    private float stepTimer = 0f;
    private bool isRandomWalking = false;
    private bool riGenerationComplete = false; // Flag: ¿Se generaron números válidos?
    private Vector2 lastFacingDirection = Vector2.down; // Para animación Idle

    // Música de Batalla
    private bool wasDetectingLastFrame = false;

    // Constantes Animator
    private const string MOVE_X_PARAM = "MoveX";
    private const string MOVE_Y_PARAM = "MoveY";

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<EnemyHealth>();
        animator = GetComponent<Animator>();
        if (animator == null) Debug.LogError("Animator component missing!", this);

        FindPlayer();

        // --- INICIO: Asignar semilla aleatoria para ESTA instancia ---
        // Combinamos tiempo (Ticks) con el ID único del GameObject para alta probabilidad de unicidad
        seed = System.DateTime.Now.Ticks + gameObject.GetInstanceID();
        //Debug.Log($"Enemy {gameObject.name}: Initializing with randomized seed: {seed}", this);
        // --- FIN: Asignar semilla aleatoria ---

        // --- Generar y probar números Ri ---
        GenerateAndTestRiNumbers(); // Intentar generar números válidos

        // Comprobar si la generación falló
        if (!riGenerationComplete || riValues.Count == 0)
        {
            //Debug.LogError($"Enemy {gameObject.name} could not generate valid Ri numbers after attempts. Random walk might be disabled or limited.", this);
            // Opcional: Implementar comportamiento alternativo si falla (ej. quedarse quieto siempre)
        }
        else
        {
            //Debug.Log($"Enemy {gameObject.name}: Successfully generated {riValues.Count} valid Ri numbers.", this);
        }
    }

    void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null) {
            playerTransform = playerObject.transform;
            playerDamageable = playerObject.GetComponent<IDamageable>();
            if (playerDamageable == null) Debug.LogError("Player does not have IDamageable.", this);
        } else {
             // Cambiado a Warning, el jugador podría instanciarse después. FixedUpdate reintentará.
            //Debug.LogWarning("Player not found during Awake. Will retry finding.", this);
        }
    }

    // ELIMINADO: void LoadRiValues() { ... }

    void FixedUpdate()
    {
        // Reintentar encontrar al jugador si no se encontró inicialmente
        if (playerTransform == null) FindPlayer();

        if (!canMove || !health.IsAlive()) {
            StopMovementAndAnimation();
            CheckAndNotifyMusicManager(false);
            return;
        }

        // --- Detección ---
        DetectPlayer();

        // --- Máquina de Estados y Movimiento ---
        if (playerDetected && playerTransform != null) {
            // ESTADO: Chasing
            isRandomWalking = false;
            stepTimer = 0f;
            currentMoveDirection = (playerTransform.position - transform.position).normalized;
            MoveEnemy(currentMoveDirection, moveSpeed);
        } else {
            // ESTADO: Random Walk (SOLO si la generación de Ri fue exitosa)
            isRandomWalking = true; // Intentará caminar aleatoriamente...
            if (riGenerationComplete && riValues.Count > 0) // ...pero solo si tiene números válidos
            {
                stepTimer -= Time.fixedDeltaTime;
                if (stepTimer <= 0f) {
                    currentMoveDirection = CalculateNextRandomDirection();
                    stepTimer = stepDuration;
                }
                if (currentMoveDirection != Vector2.zero) {
                    MoveEnemy(currentMoveDirection, randomWalkSpeed);
                } else {
                    StopRigidbody();
                }
            } else {
                 // No hay números Ri válidos (generación falló o vacía), quedarse quieto
                 currentMoveDirection = Vector2.zero;
                 StopRigidbody();
                 // isRandomWalking podría quedarse true aquí, pero no tendrá efecto sin riValues
            }
        }

        // --- Actualizar Animator y Música ---
        UpdateAnimatorParameters(currentMoveDirection);
        CheckAndNotifyMusicManager(playerDetected);
    }

    void DetectPlayer()
    {
        playerDetected = false;
        if (playerTransform != null && playerDamageable != null && playerDamageable.IsAlive()) {
            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
            playerDetected = (distanceToPlayer <= detectionRadius);
        }
    }

    Vector2 CalculateNextRandomDirection()
    {
        // Esta función asume que riValues tiene elementos porque se llama
        // sólo si riGenerationComplete es true y riValues.Count > 0
        if (riValues.Count == 0) return Vector2.zero; // Salvaguarda extra

        float step = riValues[currentRiIndex];
        currentRiIndex = (currentRiIndex + 1) % riValues.Count;

        float threshold = 0.25f;
        Vector2 nextDir = Vector2.zero;

        if (step > 0 && step <= threshold) nextDir = Vector2.up;
        else if (step > threshold && step <= 2 * threshold) nextDir = Vector2.down;
        else if (step > 2 * threshold && step <= 3 * threshold) nextDir = Vector2.right;
        else if (step > 3 * threshold && step <= 1) nextDir = Vector2.left; // Asegurarse que <= 1

        return nextDir;
    }

    void MoveEnemy(Vector2 direction, float speed)
    {
        rb.MovePosition(rb.position + direction * speed * Time.fixedDeltaTime);
        // Guardar la dirección si nos estamos moviendo (para animación idle)
        if (direction.magnitude > 0.1f) {
             lastFacingDirection = direction.normalized;
        }
    }

     void StopRigidbody()
    {
         rb.linearVelocity = Vector2.zero;
         rb.angularVelocity = 0f;
    }

    void StopMovementAndAnimation()
    {
        // canMove = false; // No es necesario setear aquí, la condición de FixedUpdate lo maneja
        StopRigidbody();
        UpdateAnimatorParameters(Vector2.zero); // Poner animación en Idle (usará lastFacingDirection)
    }

    void UpdateAnimatorParameters(Vector2 direction)
    {
        // Usa lastFacingDirection para el estado Idle
         if (animator != null)
        {
            Vector2 animDir;
            bool isMoving = direction.magnitude > 0.1f;

            if (isMoving) {
                 animDir = direction.normalized; // Usa dirección actual si se mueve
                 // Actualizamos lastFacingDirection sólo si nos movemos
                 lastFacingDirection = animDir;
            } else {
                animDir = lastFacingDirection; // Usa la última dirección si está quieto
            }

            // Actualizar parámetros del Animator
            // (Ajusta esto si tu Blend Tree 'Idle' no está en el centro o depende de dirección)
            if(isMoving) {
                 animator.SetFloat(MOVE_X_PARAM, animDir.x);
                 animator.SetFloat(MOVE_Y_PARAM, animDir.y);
            } else {
                 // Opción: Poner valores pequeños para ir a un estado Idle central si existe
                 // animator.SetFloat(MOVE_X_PARAM, 0f);
                 // animator.SetFloat(MOVE_Y_PARAM, 0f);
                 // Opción: Mantener la dirección idle visualmente (requiere Blend Tree adecuado)
                 animator.SetFloat(MOVE_X_PARAM, animDir.x); // Mantiene la dirección
                 animator.SetFloat(MOVE_Y_PARAM, animDir.y); // Mantiene la dirección
                 // Podrías necesitar un parámetro bool "IsMoving" adicional en tu Animator
                 // para distinguir entre moverse en una dirección y estar idle mirando esa dirección.
            }
        }
    }

    // === Métodos Música y Parada (sin cambios en su lógica interna) ===
     private void CheckAndNotifyMusicManager(bool isCurrentlyDetecting) {
        // ... (código igual que antes) ...
         playerDetected = isCurrentlyDetecting;
        if (playerDetected && !wasDetectingLastFrame) {
            if(BattleMusicManager.Instance != null) BattleMusicManager.Instance.RequestBattleMusic(gameObject);
        } else if (!playerDetected && wasDetectingLastFrame) {
            if(BattleMusicManager.Instance != null) BattleMusicManager.Instance.ReleaseBattleMusic(gameObject);
        }
        wasDetectingLastFrame = playerDetected;
    }

    public void StopMovement() {
         // ... (código igual que antes) ...
         bool wasDetectingBeforeStop = playerDetected || wasDetectingLastFrame;
         playerDetected = false;
         wasDetectingLastFrame = false;
         canMove = false; // Detener futuros intentos de movimiento
         StopMovementAndAnimation();
        if (wasDetectingBeforeStop && BattleMusicManager.Instance != null) {
             BattleMusicManager.Instance.ReleaseBattleMusic(gameObject);
        }
    }

    private void OnDestroy() {
         // ... (código igual que antes) ...
        if(wasDetectingLastFrame && BattleMusicManager.Instance != null) {
             BattleMusicManager.Instance.ReleaseBattleMusic(gameObject);
        }
    }

    private void OnDrawGizmosSelected() {
         // ... (código igual que antes) ...
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }


    // ==================================================
    // --- MÉTODOS LCG Y PRUEBAS (Integrados aquí) ---
    // ==================================================

    // --- Implementación de funciones estadísticas (requiere MathNet.Numerics) ---
    private double InverseNormalCDF(double p)
    {
        if (p <= 0 || p >= 1) {
            //Debug.LogError($"InverseNormalCDF: Probability p={p} out of range (0, 1).", this);
            return double.NaN;
        }
        return Normal.InvCDF(0, 1, p); // Media 0, Desv. Est. 1 (Normal Estándar)
    }

    private double InverseChiSquareCDF(double probability, int degreesOfFreedom)
    {
        if (probability < 0 || probability >= 1 || degreesOfFreedom <= 0) { // probability == 1 es problemático
             //Debug.LogError($"InverseChiSquareCDF: Invalid inputs - probability={probability}, degreesOfFreedom={degreesOfFreedom}.", this);
             return double.NaN;
        }
         // Añadir un pequeño epsilon si la probabilidad es muy cercana a 1 para evitar errores en algunas implementaciones
        if (probability > 0.999999999) probability = 0.999999999;

        return ChiSquared.InvCDF(degreesOfFreedom, probability);
    }

    // --- Generador LCG ---
    private List<double> GenerateLCGNumbers(long currentSeed, int count)
    {
        List<double> generatedNumbers = new List<double>(count);
        long xi = currentSeed;

        if (modulus <= 0) {
            //Debug.LogError("LCG Modulus (m) must be > 0.", this);
            return generatedNumbers; // Lista vacía
        }
        // No es necesario warning para m=1, el chequeo de denominador lo maneja

        // Usar m directamente como denominador si queremos rango [0, 1)
        // Usar m-1 como denominador si queremos rango [0, 1] (como en script original LCG_Tester)
        // Mantendremos m-1 por consistencia con lo anterior.
        double denominator = modulus - 1.0;
        if (denominator <= 0) { // Ocurre si m = 1
             //Debug.LogWarning("LCG Denominator (m-1) <= 0. Results might be invalid (likely all zeros).", this);
             denominator = 1.0; // Evitar división por cero, resultará en xi/1
        }

        for (int i = 0; i < count; i++) {
            // Usar aritmética long para evitar overflow intermedio
            xi = (multiplier * xi + increment);
            // El operador % en C# puede devolver negativo si el dividendo es negativo
            xi %= modulus;
            if (xi < 0) xi += modulus; // Asegurar resultado positivo/cero

            double ri = (double)xi / denominator;
            generatedNumbers.Add(ri);
        }
        return generatedNumbers;
    }

    // --- Prueba de Promedio ---
    private bool RunAverageTest(List<double> numbers, out double calculatedAverage)
    {
        calculatedAverage = 0;
        int n = numbers.Count;
        if (n == 0) {
            //Debug.LogError("Average Test: Cannot run on empty list.", this);
            return false;
        }

        calculatedAverage = numbers.Average(); // Calcula el promedio
        double z_alpha_half = InverseNormalCDF(1.0 - (alpha / 2.0));

        if (double.IsNaN(z_alpha_half)) {
            //Debug.LogError("Average Test: Failed to get Z value. Check InverseNormalCDF implementation and alpha value.", this);
            return false;
        }

        // Fórmula del intervalo de confianza para la media de una distribución U(0,1)
        double limitFactor = z_alpha_half / Math.Sqrt(12.0 * n); // Corregido: No multiplicar por 1.0
        double lowerLimit = 0.5 - limitFactor;
        double upperLimit = 0.5 + limitFactor;
        bool passed = (calculatedAverage >= lowerLimit && calculatedAverage <= upperLimit);

        // Debug.Log($"Average Test (Enemy: {gameObject.name}): Avg={calculatedAverage:F5}, Limits=[{lowerLimit:F5}, {upperLimit:F5}] -> {(passed ? "PASSED" : "FAILED")}", this);
        return passed;
    }

    // --- Prueba de Varianza ---
    private bool RunVarianceTest(List<double> numbers, double precalculatedAverage)
    {
        int n = numbers.Count;
        if (n <= 1) {
            //Debug.LogWarning("Variance Test requires n > 1.", this);
            return false;
        }

        // Calcular Varianza Muestral S^2 = sum( (xi - avg)^2 ) / (n - 1)
        // O Varianza Poblacional = sum( (xi - avg)^2 ) / n
        // Las pruebas estadísticas suelen usar la muestral (n-1), pero el script original LCG_Tester usaba poblacional (n).
        // Usemos la POBLACIONAL por consistencia con el script original.
        double sumOfSquares = numbers.Sum(num => Math.Pow(num - precalculatedAverage, 2));
        double variance = sumOfSquares / n; // Varianza poblacional

        int degreesOfFreedom = n - 1; // Grados de libertad para Chi^2 es n-1

        // Valores críticos de Chi-Cuadrado
        double chi_square_lower_crit = InverseChiSquareCDF(alpha / 2.0, degreesOfFreedom);
        double chi_square_upper_crit = InverseChiSquareCDF(1.0 - (alpha / 2.0), degreesOfFreedom);

         if (double.IsNaN(chi_square_lower_crit) || double.IsNaN(chi_square_upper_crit)) {
            //Debug.LogError("Variance Test: Failed to get Chi-Square critical values. Check InverseChiSquareCDF and parameters.", this);
            return false;
        }

        // Límites para la varianza de una U(0,1) -> Var = 1/12
        double lowerLimit = chi_square_lower_crit / (12.0 * degreesOfFreedom); // OJO: n-1 aquí abajo
        double superiorLimit = chi_square_upper_crit / (12.0 * degreesOfFreedom); // OJO: n-1 aquí abajo

        // Cuidado: La prueba original en LCG_Tester usaba (n) en el denominador de la varianza,
        // pero (n-1) en los grados de libertad y el denominador de los límites.
        // Mantengamos esa inconsistencia por ahora para replicar el comportamiento si es necesario,
        // aunque estadísticamente es más común usar (n-1) en ambos o (n) en ambos.
        // Vamos a usar n-1 grados de libertad y el denominador con n-1 como es estándar.

        if (degreesOfFreedom == 0) { // Evitar división por cero si n=1
            //Debug.LogWarning("Variance Test: Cannot calculate limits with n=1 (DoF=0).", this);
            return false;
        }
         lowerLimit = chi_square_lower_crit / (12.0 * degreesOfFreedom);
         superiorLimit = chi_square_upper_crit / (12.0 * degreesOfFreedom);

        bool passed = (variance >= lowerLimit && variance <= superiorLimit);

        // Debug.Log($"Variance Test (Enemy: {gameObject.name}): Var={variance:F5}, Limits=[{lowerLimit:F5}, {superiorLimit:F5}] (DoF={degreesOfFreedom}) -> {(passed ? "PASSED" : "FAILED")}", this);
        return passed;
    }

    // --- Método principal para generar y probar ---
    private void GenerateAndTestRiNumbers()
    {
        int maxAttempts = 100; // Límite para evitar bucles infinitos si los parámetros son malos
        int attempts = 0;
        long currentAttemptSeed = seed; // Usa la semilla (ya aleatorizada en Awake)

        while (attempts < maxAttempts)
        {
            attempts++;
            // Debug.Log($"--- Enemy {gameObject.name} LCG Attempt #{attempts} (Seed: {currentAttemptSeed}) ---", this);

            // 1. Generar números
            List<double> currentDoubleRiNumbers = GenerateLCGNumbers(currentAttemptSeed, numSamples);
             if (currentDoubleRiNumbers.Count != numSamples) {
                //Debug.LogError($"Enemy {gameObject.name}: Failed to generate {numSamples} samples (Attempt {attempts}). Check LCG parameters.", this);
                currentAttemptSeed++; // Intentar con la siguiente semilla
                continue; // Pasar a la siguiente iteración del while
             }

            // 2. Probar Promedio
            bool averagePassed = RunAverageTest(currentDoubleRiNumbers, out double calculatedAverage);

            // 3. Probar Varianza
            // Ejecutar siempre la prueba de varianza para ver ambos resultados si se desea debuggear
            bool variancePassed = RunVarianceTest(currentDoubleRiNumbers, calculatedAverage);

            // 4. Comprobar si AMBAS pasaron
            if (averagePassed && variancePassed)
            {
                // ¡Éxito!
                // Debug.Log($"Enemy {gameObject.name}: LCG Success! Found valid set ({numSamples} numbers) after {attempts} attempts using seed {currentAttemptSeed}.", this);

                // Convertir List<double> a List<float> para usar en el resto del script
                riValues = currentDoubleRiNumbers.ConvertAll(d => (float)d);
                riGenerationComplete = true; // Marcar como completado
                currentRiIndex = 0; // Resetear índice para empezar desde el principio
                return; // Salir del método, ya tenemos los números
            }
            else
            {
                // Al menos una prueba falló, intentar con la siguiente semilla
                // Debug.Log($"Enemy {gameObject.name}: LCG Attempt {attempts} failed tests (Avg: {averagePassed}, Var: {variancePassed}). Trying next seed.", this);
                currentAttemptSeed++; // Incrementar para la siguiente iteración
            }
        }

        // Si salimos del bucle while es porque se superaron los intentos máximos
        //Debug.LogError($"Enemy {gameObject.name}: Failed to generate a valid Ri number set after {maxAttempts} attempts. Random walk disabled.", this);
        riGenerationComplete = false; // Asegurarse de que esté falso
        riValues.Clear(); // Limpiar por si acaso
    }
}