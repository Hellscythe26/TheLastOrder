using UnityEngine; // Para Debug.LogWarning, Mathf.Approximately
using System.Collections.Generic; // Para Dictionary
using System; // Para System.Enum, ArgumentException, ArgumentNullException
using System.Linq; // Para .Reverse(), .Keys.LastOrDefault()

/// <summary>
/// Implementa una Cadena de Markov genérica de primer orden.
/// Permite modelar sistemas que transicionan entre un conjunto finito de estados
/// donde la probabilidad de la siguiente transición depende únicamente del estado actual.
/// </summary>
/// <typeparam name="TState">El tipo de enumeración que define los estados de la cadena.</typeparam>
public class Markov<TState> where TState : System.Enum // Restringe TState a ser un tipo Enum.
{
    private TState currentState; // Almacena el estado actual de la cadena.
    // Matriz de transición: Diccionario donde la clave es el estado origen (from),
    // y el valor es otro diccionario con los estados destino (to) y sus probabilidades.
    private Dictionary<TState, Dictionary<TState, float>> transitionMatrix;
    // Delegado funcional que provee un número aleatorio en el rango [0,1) para las decisiones de transición.
    // Se usa el generador de números (LCGManager) a través de este proveedor.
    private System.Func<float> randomNumberProvider;

    /// <summary>
    /// Constructor para la Cadena de Markov.
    /// </summary>
    /// <param name="initialState">El estado con el que inicia la cadena.</param>
    /// <param name="matrix">La matriz de probabilidades de transición entre estados.</param>
    /// <param name="rngProvider">Una función (delegado) que debe devolver un float aleatorio en [0,1)
    ///                          (usualmente obtenido del LCGManager).</param>
    public Markov(TState initialState, Dictionary<TState, Dictionary<TState, float>> matrix, System.Func<float> rngProvider)
    {
        // Validación de argumentos.
        if (matrix == null || matrix.Count == 0)
        {
            throw new ArgumentException("La matriz de transición no puede ser nula o vacía.", nameof(matrix));
        }
        if (rngProvider == null)
        {
            throw new ArgumentNullException(nameof(rngProvider), "Se debe proveer un generador de números aleatorios.");
        }
        this.currentState = initialState;
        this.transitionMatrix = matrix;
        this.randomNumberProvider = rngProvider; // Almacena el proveedor de números aleatorios.
        ValidateMatrix(); // Valida la consistencia de la matriz de transición.
    }

    /// <summary>
    /// Valida que la suma de las probabilidades de transición salientes de cada estado sea aproximadamente 1.0.
    /// Emite advertencias si no se cumple.
    /// </summary>
    private void ValidateMatrix()
    {
        foreach (var fromStateEntry in transitionMatrix) // Itera sobre cada estado origen en la matriz.
        {
            // Comprueba si el estado tiene transiciones definidas.
            if (fromStateEntry.Value == null || fromStateEntry.Value.Count == 0)
            {
                 Debug.LogWarning($"Markov Class: El estado '{fromStateEntry.Key}' no tiene transiciones definidas o su diccionario de transiciones está vacío en la matriz.");
                 continue; // Salta al siguiente estado.
            }
            float sumOfProbs = 0f;
            // Suma todas las probabilidades de transición para el estado actual.
            foreach (var prob in fromStateEntry.Value.Values)
            {
                sumOfProbs += prob;
            }
            // Comprueba si la suma es aproximadamente 1.0.
            if (!Mathf.Approximately(sumOfProbs, 1.0f))
            {
                Debug.LogWarning($"Markov Class: Las probabilidades para el estado '{fromStateEntry.Key}' no suman aproximadamente 1.0 (Suman: {sumOfProbs}). El comportamiento de GetNextState() puede ser inesperado.");
            }
        }
    }

    /// <summary>
    /// Determina y transiciona al siguiente estado de la cadena basado en el estado actual
    /// y las probabilidades de la matriz de transición, utilizando un número aleatorio
    /// proporcionado por 'randomNumberProvider'.
    /// </summary>
    /// <returns>El nuevo estado actual de la cadena después de la transición.</returns>
    public TState GetNextState()
    {
        // Verifica que existan transiciones definidas para el estado actual.
        if (!transitionMatrix.ContainsKey(currentState) || 
            transitionMatrix[currentState] == null || 
            transitionMatrix[currentState].Count == 0)
        {
            Debug.LogError($"Markov Class: No hay transiciones definidas o válidas para el estado actual: '{currentState}'. No se puede determinar el siguiente estado. Devolviendo estado actual.");
            return currentState; // Devuelve el estado actual para evitar errores.
        }
        Dictionary<TState, float> transitions = transitionMatrix[currentState]; // Obtiene las transiciones posibles desde el estado actual.
        // Se usa el generador de números (a través de randomNumberProvider): Para decidir la transición.
        float randomValue = randomNumberProvider(); // Obtiene un número aleatorio [0,1).
        float cumulativeProbability = 0f; // Acumulador para las probabilidades.
        // Itera sobre las posibles transiciones y sus probabilidades.
        foreach (KeyValuePair<TState, float> transition in transitions)
        {
            cumulativeProbability += transition.Value; // Suma la probabilidad de la transición actual.
            // Si el número aleatorio es menor que la probabilidad acumulada,
            // se ha seleccionado esta transición.
            if (randomValue < cumulativeProbability)
            {
                currentState = transition.Key; // Actualiza el estado actual de la cadena.
                return currentState; // Devuelve el nuevo estado.
            }
        }

        // --- Fallback ---
        // Este punto solo debería alcanzarse si:
        // 1. Las probabilidades para 'currentState' no suman 1.0.
        // 2. 'randomValue' es exactamente 1.0 (y la suma de probabilidades es 1.0),
        //    lo cual es posible si el randomNumberProvider puede generar 1.0 inclusivo.
        Debug.LogWarning($"Markov Class: No se pudo determinar el siguiente estado para '{currentState}' con randomValue {randomValue} mediante el proceso normal. " +
                         "Verifique que las probabilidades sumen 1.0. Intentando fallback.");
        // Como fallback simple, se devuelve el último estado en la lista de transiciones del estado actual.
        // Esto es arbitrario pero previene que la función no devuelva un estado.
        if(transitions.Count > 0) {
            TState fallbackState = transitions.Keys.LastOrDefault(); // Requiere System.Linq.
            if (!EqualityComparer<TState>.Default.Equals(fallbackState, default(TState)) || transitions.ContainsKey(fallbackState) ) { // Verifica que no sea el valor por defecto de TState si es un tipo de valor, o null para referencia (aunque Enum es valor)
                currentState = fallbackState;
                return currentState;
            }
        }
        // Si todo falla (ej. diccionario de transiciones vacío, aunque ya se validó), se mantiene el estado actual.
        return currentState;
    }

    /// <summary>
    /// Obtiene el estado actual en el que se encuentra la cadena de Markov.
    /// </summary>
    /// <returns>El estado actual.</returns>
    public TState GetCurrentState()
    {
        return currentState;
    }
    
    /// <summary>
    /// Permite forzar el cambio del estado actual de la cadena de Markov a un nuevo estado.
    /// Útil para reseteos o para influenciar la cadena externamente.
    /// </summary>
    /// <param name="newState">El nuevo estado al que se forzará la cadena.</param>
    public void ForceSetCurrentState(TState newState)
    {
        // Advierte si se intenta forzar a un estado que no tiene definidas transiciones de salida en la matriz.
        if (!transitionMatrix.ContainsKey(newState))
        {
            Debug.LogWarning($"Markov Class: Intentando forzar a un estado '{newState}' que no existe como clave origen en la matriz de transición. El comportamiento futuro de GetNextState() podría ser indefinido si este estado no tiene transiciones de salida definidas.");
        }
        currentState = newState; // Establece el nuevo estado actual.
    }
}