using UnityEngine;
using System.Collections.Generic;

public class RandomWalk
{
    private List<float> riValues;
    private int currentRiIndex = 0;
    private float stepTimer = 0f;
    private float stepDuration;
    private Vector2 currentMoveDirection = Vector2.zero;
    private bool initialized = false;

    public RandomWalk(List<float> randomNumbers, float durationPerStep)
    {
        if (randomNumbers != null && randomNumbers.Count > 0)
        {
            this.riValues = randomNumbers;
            this.initialized = true;
        }
        else
        {
            this.riValues = new List<float>();
            this.initialized = false;
            Debug.LogWarning("RandomWalker inicializado sin números aleatorios válidos. La caminata aleatoria no funcionará como se espera.");
        }
        this.stepDuration = durationPerStep;
        this.stepTimer = 0f;
    }

    public Vector2 UpdateWalk(float deltaTime)
    {
        if (!initialized || riValues.Count == 0)
        {
            return Vector2.zero;
        }

        stepTimer -= deltaTime;
        if (stepTimer <= 0f)
        {
            currentMoveDirection = CalculateNextRandomDirection();
            stepTimer = stepDuration;
        }
        return currentMoveDirection;
    }

    private Vector2 CalculateNextRandomDirection()
    {
        if (riValues.Count == 0) return Vector2.zero;
        float stepValue = riValues[currentRiIndex];
        currentRiIndex = (currentRiIndex + 1) % riValues.Count;
        float threshold = 0.25f;
        Vector2 nextDir = Vector2.zero;
        if (stepValue >= 0 && stepValue < threshold)
        {
            nextDir = Vector2.up;
        }
        else if (stepValue >= threshold && stepValue < 2 * threshold)
        {
            nextDir = Vector2.down;
        }
        else if (stepValue >= 2 * threshold && stepValue < 3 * threshold)
        {
            nextDir = Vector2.right;
        }
        else if (stepValue >= 3 * threshold && stepValue <= 1.0f)
        {
            nextDir = Vector2.left;
        }
        return nextDir;
    }

    public void Reset()
    {
        currentRiIndex = 0;
        stepTimer = 0f;
        currentMoveDirection = Vector2.zero;
    }

    public bool IsInitialized()
    {
        return initialized && riValues.Count > 0;
    }
}