using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WaitingLine
{
    private MonoBehaviour coroutineRunner;
    private Queue<GameObject> activationQueue;
    private float delayBetweenActivations;
    private System.Action<GameObject, bool> setEnemyActiveAction;
    private Coroutine currentActivationCoroutine;

    public WaitingLine(MonoBehaviour runner, float delay, System.Action<GameObject, bool> activationAction)
    {
        if (runner == null) throw new System.ArgumentNullException(nameof(runner));
        if (activationAction == null) throw new System.ArgumentNullException(nameof(activationAction));
        this.coroutineRunner = runner;
        this.delayBetweenActivations = Mathf.Max(0, delay);
        this.setEnemyActiveAction = activationAction;
        this.activationQueue = new Queue<GameObject>();
    }

    public void AddEnemiesToQueue(List<GameObject> enemies)
    {
        if (enemies == null) return;
        foreach (GameObject enemy in enemies)
        {
            if (enemy != null)
            {
                activationQueue.Enqueue(enemy);
            }
        }
    }

    public void StartProcessingQueue()
    {
        if (IsProcessing())
        {
            return;
        }
        if (activationQueue.Count > 0 && coroutineRunner.gameObject.activeInHierarchy)
        {
            currentActivationCoroutine = coroutineRunner.StartCoroutine(ProcessActivationCoroutine());
        }
        else if (activationQueue.Count == 0)
        {
        }
    }

    public void StopProcessingQueue()
    {
        if (currentActivationCoroutine != null && coroutineRunner != null)
        {
            coroutineRunner.StopCoroutine(currentActivationCoroutine);
            currentActivationCoroutine = null;
        }
    }

    public bool IsProcessing()
    {
        return currentActivationCoroutine != null;
    }

    public int GetEnemiesRemainingInQueue()
    {
        return activationQueue.Count;
    }

    private IEnumerator ProcessActivationCoroutine()
    {
        while (activationQueue.Count > 0)
        {
            GameObject enemyToActivate = activationQueue.Dequeue();
            if (enemyToActivate != null)
            {
                setEnemyActiveAction(enemyToActivate, true); 
                if (activationQueue.Count > 0 && delayBetweenActivations > 0)
                {
                    yield return new WaitForSeconds(delayBetweenActivations);
                }
                else if (delayBetweenActivations > 0)
                {
                     yield return new WaitForSeconds(delayBetweenActivations * 0.5f);
                }
            }
        }
        currentActivationCoroutine = null;
    }
}