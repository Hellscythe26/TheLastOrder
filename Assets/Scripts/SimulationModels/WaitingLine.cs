using UnityEngine;
using System.Collections; // Para IEnumerator (Corutinas)
using System.Collections.Generic; // Para Queue y List

/// <summary>
/// Gestiona una cola de GameObjects (enemigos) para activarlos secuencialmente
/// con un retraso específico entre cada activación.
/// Este es un modelo de simulación de Línea de Espera para la activación de entidades.
/// </summary>
public class WaitingLine
{
    // MonoBehaviour que ejecutará las corutinas (el RoomController).
    private MonoBehaviour coroutineRunner;
    // Cola interna para almacenar los GameObjects que esperan ser activados.
    private Queue<GameObject> activationQueue;
    // Retraso en segundos entre la activación de cada GameObject en la cola.
    private float delayBetweenActivations;
    // Delegado a un método externo que sabe cómo activar/desactivar un GameObject específico (habilitar sus scripts de IA).
    // Se usa el generador de números (LCGManager) indirectamente si el 'activationAction' lo utiliza.
    private System.Action<GameObject, bool> setEnemyActiveAction; // Parámetros: (GameObject a activar, bool esActivo)
    // Referencia a la corutina de procesamiento actual para poder detenerla.
    private Coroutine currentActivationCoroutine;

    /// <summary>
    /// Constructor para la línea de espera de activación.
    /// </summary>
    /// <param name="runner">El MonoBehaviour que puede iniciar y detener corutinas.</param>
    /// <param name="delay">El tiempo de espera en segundos entre la activación de cada entidad.</param>
    /// <param name="activationAction">El método (delegado) a llamar para activar una entidad.
    ///                              Debe aceptar un GameObject y un booleano (para activar/desactivar).</param>
    public WaitingLine(MonoBehaviour runner, float delay, System.Action<GameObject, bool> activationAction)
    {
        // Validación de argumentos.
        if (runner == null) throw new System.ArgumentNullException(nameof(runner), "El coroutineRunner no puede ser nulo.");
        if (activationAction == null) throw new System.ArgumentNullException(nameof(activationAction), "La acción de activación (activationAction) no puede ser nula.");
        this.coroutineRunner = runner;
        this.delayBetweenActivations = Mathf.Max(0, delay); // Asegura que el delay no sea negativo.
        this.setEnemyActiveAction = activationAction;
        this.activationQueue = new Queue<GameObject>(); // Inicializa la cola vacía.
    }

    /// <summary>
    /// Añade una lista de GameObjects a la cola de activación.
    /// </summary>
    /// <param name="enemies">Lista de GameObjects a encolar.</param>
    public void AddEnemiesToQueue(List<GameObject> enemies)
    {
        if (enemies == null) return; // No hacer nada si la lista es nula.
        foreach (GameObject enemy in enemies)
        {
            if (enemy != null) // Solo encolar GameObjects válidos.
            {
                activationQueue.Enqueue(enemy);
            }
        }
    }

    /// <summary>
    /// Inicia el proceso de activación secuencial de los GameObjects en la cola.
    /// Si ya se está procesando o la cola está vacía, no hace nada.
    /// </summary>
    public void StartProcessingQueue()
    {
        // Si ya hay una corutina de activación en curso, no iniciar otra.
        if (IsProcessing())
        {
            return;
        }
        // Solo iniciar si hay elementos en la cola y el coroutineRunner está activo.
        if (activationQueue.Count > 0 && coroutineRunner != null && coroutineRunner.gameObject.activeInHierarchy)
        {
            currentActivationCoroutine = coroutineRunner.StartCoroutine(ProcessActivationCoroutine());
        }
    }

    /// <summary>
    /// Detiene la corutina de activación actual, si hay una en curso.
    /// Los GameObjects que no se hayan activado permanecerán en la cola.
    /// </summary>
    public void StopProcessingQueue()
    {
        if (currentActivationCoroutine != null && coroutineRunner != null)
        {
            coroutineRunner.StopCoroutine(currentActivationCoroutine);
            currentActivationCoroutine = null; // Limpiar la referencia a la corutina.
        }
    }

    /// <summary>
    /// Comprueba si la línea de espera está actualmente procesando la activación de GameObjects.
    /// </summary>
    /// <returns>True si hay una corutina de activación en curso, false en caso contrario.</returns>
    public bool IsProcessing()
    {
        return currentActivationCoroutine != null;
    }

    /// <summary>
    /// Obtiene el número de GameObjects que actualmente están en la cola esperando ser activados.
    /// </summary>
    /// <returns>La cantidad de GameObjects en la cola.</returns>
    public int GetEnemiesRemainingInQueue()
    {
        return activationQueue.Count;
    }

    /// <summary>
    /// Corutina que procesa la cola, activando cada GameObject secuencialmente con un retraso.
    /// </summary>
    private IEnumerator ProcessActivationCoroutine()
    {
        // Bucle mientras haya GameObjects en la cola.
        while (activationQueue.Count > 0)
        {
            GameObject enemyToActivate = activationQueue.Dequeue(); // Saca el siguiente GameObject de la cola.

            if (enemyToActivate != null) // Comprueba si el GameObject aún existe.
            {
                setEnemyActiveAction(enemyToActivate, true); // Llama al método delegado para activar el GameObject.
                // Espera el 'delayBetweenActivations' si hay más elementos en la cola y el delay es positivo.
                if (activationQueue.Count > 0 && delayBetweenActivations > 0)
                {
                    yield return new WaitForSeconds(delayBetweenActivations);
                }
                else if (delayBetweenActivations > 0)
                {
                     yield return new WaitForSeconds(delayBetweenActivations * 0.5f);
                }
            }
            // Si el enemyToActivate es null (fue destruido mientras esperaba), simplemente se ignora.
        }
        currentActivationCoroutine = null; // Marca la corutina como finalizada.
    }
}