// Agents.cs
using UnityEngine; // Necesario si usas tipos de Unity como Vector2, o para Debug

public class Agents // Anteriormente EnemyAgentActions
{
    // Definición de posibles acciones que el agente puede decidir tomar
    public enum Action
    {
        Idle_Or_RandomWalk, // Quedarse quieto o caminar aleatoriamente
        ChasePlayer,        // Perseguir al jugador
        AttackPlayer        // Atacar al jugador
    }

    // Estado del agente/observaciones
    private bool isPlayerDetected;
    private bool isPlayerInAttackRange;
    private bool isAttackOffCooldown;
    private bool canAgentPerformActions; // Si el agente (enemigo) está en condiciones de actuar

    /// <summary>
    /// Actualiza las observaciones del agente sobre su entorno y estado.
    /// </summary>
    /// <param name="detected">True si el jugador está detectado en el radio mayor.</param>
    /// <param name="inRangeToAttack">True si el jugador está dentro del rango de ataque.</param>
    /// <param name="attackReady">True si el cooldown de ataque ha terminado.</param>
    /// <param name="canAct">True si el agente (enemigo) está vivo y puede realizar acciones.</param>
    public void UpdateObservations(bool detected, bool inRangeToAttack, bool attackReady, bool canAct)
    {
        isPlayerDetected = detected;
        isPlayerInAttackRange = inRangeToAttack;
        isAttackOffCooldown = attackReady;
        canAgentPerformActions = canAct;
    }

    /// <summary>
    /// Define la política del agente y decide la siguiente acción
    /// basada en las observaciones actuales.
    /// </summary>
    /// <returns>La acción decidida por el agente.</returns>
    public Action DecideNextAction()
    {
        if (!canAgentPerformActions)
        {
            // Si el agente no puede actuar (ej. está muerto o incapacitado),
            // por defecto no hace nada o vuelve a un estado base.
            return Action.Idle_Or_RandomWalk;
        }

        // Política de decisión:
        if (isPlayerDetected)
        {
            if (isPlayerInAttackRange && isAttackOffCooldown)
            {
                // Prioridad 1: Atacar si el jugador está detectado, en rango, y el ataque está listo.
                return Action.AttackPlayer;
            }
            else
            {
                // Prioridad 2: Perseguir si el jugador está detectado pero no se cumplen las condiciones para atacar.
                return Action.ChasePlayer;
            }
        }
        else
        {
            // Prioridad 3: Si el jugador no está detectado, realizar caminata aleatoria o estar inactivo.
            return Action.Idle_Or_RandomWalk;
        }
    }
}