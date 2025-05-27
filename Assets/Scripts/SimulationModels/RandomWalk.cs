using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gestiona la lógica de una caminata aleatoria simple en 2D (arriba, abajo, izquierda, derecha).
/// Utiliza una secuencia pre-generada de números pseudoaleatorios (Ri) para determinar
/// la dirección en cada paso y mantiene esa dirección por una duración específica.
/// Este es un componente del modelo de simulación de comportamiento del enemigo.
/// </summary>
public class RandomWalk
{
    private List<float> riValues; // Lista de números Ri (entre 0 y 1) para determinar la dirección.
    private int currentRiIndex = 0; // Índice del próximo número Ri a usar.
    private float stepTimer = 0f; // Temporizador para la duración del paso actual.
    private float stepDuration; // Cuánto tiempo se mantiene cada dirección de caminata.
    private Vector2 currentMoveDirection = Vector2.zero; // Dirección de movimiento actual.
    private bool initialized = false; // Indica si el caminador fue inicializado correctamente.

    /// <summary>
    /// Constructor para el RandomWalk.
    /// </summary>
    /// <param name="randomNumbers">Lista de números pseudoaleatorios (Ri) validados, en el rango [0,1].
    ///                             Se usa el generador de números (LCGManager) para obtener esta lista.</param>
    /// <param name="durationPerStep">Cuánto tiempo (en segundos) se mantendrá cada dirección de caminata antes de elegir una nueva.</param>
    public RandomWalk(List<float> randomNumbers, float durationPerStep)
    {
        // Valida que se proporcionen números aleatorios.
        if (randomNumbers != null && randomNumbers.Count > 0)
        {
            this.riValues = randomNumbers;
            this.initialized = true;
        }
        else
        {
            this.riValues = new List<float>(); // Inicializa con lista vacía para evitar NullReferenceException.
            this.initialized = false;
            Debug.LogWarning("RandomWalker inicializado sin números aleatorios válidos. La caminata aleatoria no funcionará como se espera.");
        }
        this.stepDuration = Mathf.Max(0.1f, durationPerStep); // Asegura una duración mínima para el paso.
        this.stepTimer = 0f; // Inicia listo para calcular la primera dirección o esperar 'stepDuration'.
    }

    /// <summary>
    /// Actualiza el estado de la caminata aleatoria basado en el tiempo transcurrido (deltaTime).
    /// Determina si es momento de cambiar de dirección y calcula la nueva dirección.
    /// </summary>
    /// <param name="deltaTime">El tiempo transcurrido desde la última actualización (Time.fixedDeltaTime).</param>
    /// <returns>El Vector2 de la dirección de movimiento actual para este frame.</returns>
    public Vector2 UpdateWalk(float deltaTime)
    {
        // Si no está inicializado o no hay números Ri, no se puede mover.
        if (!initialized || riValues.Count == 0)
        {
            return Vector2.zero;
        }
        stepTimer -= deltaTime; // Decrementa el temporizador del paso actual.
        // Si el temporizador se agotó, es momento de elegir una nueva dirección.
        if (stepTimer <= 0f)
        {
            currentMoveDirection = CalculateNextRandomDirection(); // Calcula la nueva dirección.
            stepTimer = stepDuration; // Resetea el temporizador para el nuevo paso.
        }
        return currentMoveDirection; // Devuelve la dirección de movimiento actual.
    }

    /// <summary>
    /// Calcula la siguiente dirección de movimiento (Arriba, Abajo, Izquierda, Derecha)
    /// basándose en el siguiente número Ri de la secuencia.
    /// </summary>
    /// <returns>Un Vector2 representando la dirección elegida.</returns>
    private Vector2 CalculateNextRandomDirection()
    {
        // Se usa el generador de números (LCGManager) a través de la lista riValues.
        if (riValues.Count == 0) return Vector2.zero;
        float stepValue = riValues[currentRiIndex]; // Obtiene el siguiente número Ri.
        currentRiIndex = (currentRiIndex + 1) % riValues.Count; // Avanza el índice, ciclando si llega al final.
        // Define umbrales para dividir el rango [0,1] en 4 partes iguales para las 4 direcciones.
        float threshold = 0.25f;
        Vector2 nextDir = Vector2.zero;
        if (stepValue >= 0 && stepValue < threshold) // Rango [0, 0.25)
        {
            nextDir = Vector2.up;
        }
        else if (stepValue >= threshold && stepValue < 2 * threshold) // Rango [0.25, 0.50)
        {
            nextDir = Vector2.down;
        }
        else if (stepValue >= 2 * threshold && stepValue < 3 * threshold) // Rango [0.50, 0.75)
        {
            nextDir = Vector2.right;
        }
        else if (stepValue >= 3 * threshold && stepValue <= 1.0f) // Rango [0.75, 1.0]
        {
            nextDir = Vector2.left;
        }
        return nextDir;
    }

    /// <summary>
    /// Opcional: Resetea el estado interno del RandomWalk, haciendo que comience
    /// su secuencia de caminata desde el principio de la lista de números Ri.
    /// </summary>
    public void Reset()
    {
        currentRiIndex = 0;
        stepTimer = 0f; // Podría iniciar un nuevo paso inmediatamente o esperar 'stepDuration'.
        currentMoveDirection = Vector2.zero;
    }

    /// <summary>
    /// Devuelve true si el RandomWalk fue inicializado correctamente con una lista válida de números Ri.
    /// </summary>
    public bool IsInitialized()
    {
        return initialized && riValues.Count > 0;
    }
}