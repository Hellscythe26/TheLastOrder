using UnityEngine; // Para Debug si se añadiera en el futuro.

/// <summary>
/// Define la lógica de decisión para un agente.
/// Recibe observaciones del entorno y, basándose en una política interna,
/// decide la acción a tomar. Este es un modelo de simulación basado en agentes.
/// </summary>
public class Agents
{
    /// <summary>
    /// Define las posibles acciones generales que un agente puede decidir realizar.
    /// </summary>
    public enum Action
    {
        Idle_Or_RandomWalk, // Acción de estar inactivo o realizar una caminata aleatoria.
        ChasePlayer,        // Acción de perseguir al jugador.
        AttackPlayer        // Acción de atacar al jugador.
    }
    // Variables internas para almacenar el estado observado del entorno del agente.
    private bool isPlayerDetected;      // True si el jugador ha sido detectado en un radio mayor.
    private bool isPlayerInAttackRange; // True si el jugador está dentro del rango de ataque.
    private bool isAttackOffCooldown;   // True si la habilidad de ataque del agente no está en cooldown.
    private bool canAgentPerformActions;// True si el agente está en condiciones de realizar acciones (está vivo).

    /// <summary>
    /// Actualiza las observaciones del agente con la información más reciente del entorno y su propio estado.
    /// Llamado externamente antes de pedir una decisión.
    /// </summary>
    /// <param name="detected">True si el jugador está actualmente detectado.</param>
    /// <param name="inRangeToAttack">True si el jugador está actualmente dentro del rango de ataque.</param>
    /// <param name="attackReady">True si la capacidad de ataque del agente está lista (fuera de cooldown).</param>
    /// <param name="canAct">True si el agente está en condiciones generales de actuar (ej. vivo, no aturdido).</param>
    public void UpdateObservations(bool detected, bool inRangeToAttack, bool attackReady, bool canAct)
    {
        isPlayerDetected = detected;
        isPlayerInAttackRange = inRangeToAttack;
        isAttackOffCooldown = attackReady;
        canAgentPerformActions = canAct;
    }

    /// <summary>
    /// Determina la siguiente acción a tomar por el agente basándose en las observaciones actuales.
    /// Esta función implementa la "política" de decisión del agente.
    /// </summary>
    /// <returns>La enumeración 'Action' que representa la acción decidida.</returns>
    public Action DecideNextAction()
    {
        // Si el agente no puede realizar acciones (está muerto), retorna una acción base.
        if (!canAgentPerformActions)
        {
            return Action.Idle_Or_RandomWalk;
        }
        // Implementación de la política de decisión:
        if (isPlayerDetected) // Si el jugador ha sido detectado...
        {
            // ...y está en rango de ataque, y el ataque está listo...
            if (isPlayerInAttackRange && isAttackOffCooldown)
            {
                // ...entonces la acción prioritaria es atacar.
                return Action.AttackPlayer;
            }
            else
            {
                // ...si está detectado pero no puede atacar (fuera de rango o en cooldown), entonces persigue.
                return Action.ChasePlayer;
            }
        }
        else // Si el jugador no está detectado...
        {
            // ...entonces el agente realiza su comportamiento por defecto (caminata aleatoria o idle).
            return Action.Idle_Or_RandomWalk;
        }
    }
}