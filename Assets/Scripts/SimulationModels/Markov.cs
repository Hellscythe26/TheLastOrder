using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class Markov<TState> where TState : System.Enum
{
    private TState currentState;
    private Dictionary<TState, Dictionary<TState, float>> transitionMatrix;
    private System.Func<float> randomNumberProvider;

    public Markov(TState initialState, Dictionary<TState, Dictionary<TState, float>> matrix, System.Func<float> rngProvider)
    {
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
        this.randomNumberProvider = rngProvider;
        ValidateMatrix();
    }
    private void ValidateMatrix()
    {
        foreach (var fromStateEntry in transitionMatrix)
        {
            if (fromStateEntry.Value == null || fromStateEntry.Value.Count == 0)
            {
                 Debug.LogWarning($"Markov Class: El estado '{fromStateEntry.Key}' no tiene transiciones definidas o su diccionario de transiciones está vacío.");
                 continue;
            }
            float sumOfProbs = 0f;
            foreach (var prob in fromStateEntry.Value.Values)
            {
                sumOfProbs += prob;
            }

            if (!Mathf.Approximately(sumOfProbs, 1.0f))
            {
                Debug.LogWarning($"Markov Class: Las probabilidades para el estado '{fromStateEntry.Key}' no suman aproximadamente 1.0 (Suman: {sumOfProbs}). El comportamiento de GetNextState() puede ser inesperado.");
            }
        }
    }

    public TState GetNextState()
    {
        if (!transitionMatrix.ContainsKey(currentState) || transitionMatrix[currentState] == null || transitionMatrix[currentState].Count == 0)
        {
            Debug.LogError($"Markov Class: No hay transiciones definidas o válidas para el estado actual: '{currentState}'. No se puede determinar el siguiente estado. Devolviendo estado actual.");
            return currentState;
        }
        Dictionary<TState, float> transitions = transitionMatrix[currentState];
        float randomValue = randomNumberProvider();
        float cumulativeProbability = 0f;
        foreach (KeyValuePair<TState, float> transition in transitions)
        {
            cumulativeProbability += transition.Value;
            if (randomValue < cumulativeProbability)
            {
                currentState = transition.Key;
                return currentState;
            }
        }
        Debug.LogWarning($"Markov Class: No se pudo determinar el siguiente estado para '{currentState}' con randomValue {randomValue} mediante el proceso normal. " +
                         "Esto puede indicar un problema con la suma de probabilidades (deberían sumar 1.0) o que randomValue fue >= suma_total_prob (ej. 1.0)." +
                         " Intentando fallback al último estado válido.");
        if(transitions.Count > 0) {
            TState fallbackState = transitions.Keys.LastOrDefault();
             if (fallbackState != null) {
                currentState = fallbackState;
                return currentState;
            }
        }
        return currentState;
    }

    public TState GetCurrentState()
    {
        return currentState;
    }
    
    public void ForceSetCurrentState(TState newState)
    {
        if (!transitionMatrix.ContainsKey(newState))
        {
            Debug.LogWarning($"Markov Class: Intentando forzar a un estado '{newState}' que no existe como clave origen en la matriz de transición. El comportamiento futuro de GetNextState() podría ser indefinido si este estado no tiene transiciones de salida definidas.");
        }
        currentState = newState;
    }
}