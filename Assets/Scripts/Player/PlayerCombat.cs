using UnityEngine;
using UnityEngine.Events; // Para UnityEvent
using System.Collections;   // Necesario para Corutinas

/// <summary>
/// Gestiona la lógica de combate del jugador, incluyendo la activación de hitboxes de ataque,
/// el manejo del estado de ataque y la invocación de eventos relacionados con el combate.
/// </summary>
public class PlayerCombat : MonoBehaviour
{
    [Header("Stats")]
    [Tooltip("Daño base infligido por cada ataque del jugador.")]
    [SerializeField] private float damagePerAttack = 1f;
    [Header("Components")]
    [Tooltip("Referencia al script PlayerAnimation para obtener la dirección del ataque y reproducir animaciones.")]
    [SerializeField] private PlayerAnimation playerAnimation;
    [Header("Attack Hitboxes (Colliders)")]
    [Tooltip("El Collider2D del hitbox para atacar hacia arriba.")]
    [SerializeField] private Collider2D hitboxUp;
    [Tooltip("El Collider2D del hitbox para atacar hacia abajo.")]
    [SerializeField] private Collider2D hitboxDown;
    [Tooltip("El Collider2D del hitbox para atacar hacia la izquierda.")]
    [SerializeField] private Collider2D hitboxLeft;
    [Tooltip("El Collider2D del hitbox para atacar hacia la derecha.")]
    [SerializeField] private Collider2D hitboxRight;
    [Header("State & Events")]
    [Tooltip("Indica si el jugador está actualmente en medio de una secuencia de ataque.")]
    [SerializeField] private bool isAttacking = false; // Estado actual de ataque.
    [Tooltip("Evento que se dispara cuando el jugador inicia un ataque.")]
    public UnityEvent OnAttack;
    [Tooltip("Evento que se dispara cuando la secuencia de ataque del jugador termina.")]
    public UnityEvent OnAttackEnd;

    /// <summary>
    /// Se llama una vez cuando el script es cargado.
    /// Inicializa referencias y desactiva los hitboxes.
    /// </summary>
    private void Awake()
    {
        // Obtiene el componente PlayerAnimation si no está asignado.
        if (playerAnimation == null) playerAnimation = GetComponent<PlayerAnimation>();
        // Inicializa los UnityEvents si son nulos.
        if (OnAttack == null) OnAttack = new UnityEvent();
        if (OnAttackEnd == null) OnAttackEnd = new UnityEvent();
        // Asegura que todos los hitboxes estén desactivados al inicio.
        DisableAllHitboxes();
    }

    /// <summary>
    /// Inicia la secuencia de ataque del jugador.
    /// Determina la dirección del ataque, activa el hitbox correspondiente,
    /// reproduce la animación de ataque e invoca eventos.
    /// </summary>
    public void Attack()
    {
        // No permitir un nuevo ataque si ya está atacando o si falta PlayerAnimation.
        if (isAttacking) return;
        if (playerAnimation == null) { Debug.LogError("PlayerCombat: PlayerAnimation no está asignado. No se puede atacar.", this); return; }
        isAttacking = true; // Establece el estado a "atacando".
        // Obtiene la última dirección de movimiento/mirada del jugador para dirigir el ataque.
        Vector2 attackDirection = playerAnimation.GetLastMoveDirection();
        Collider2D activeCollider = null; // Collider que se activará para este ataque.
        // Determina qué hitbox activar basado en la dirección del ataque.
        if (Mathf.Abs(attackDirection.x) > Mathf.Abs(attackDirection.y)) // Ataque más horizontal
        {
            activeCollider = (attackDirection.x > 0) ? hitboxRight : hitboxLeft;
        }
        else // Ataque más vertical (o igual)
        {
            activeCollider = (attackDirection.y > 0) ? hitboxUp : hitboxDown;
        }
        // Si se determinó un hitbox válido.
        if (activeCollider != null)
        {
            // Activa el GameObject al que pertenece el Collider del hitbox.
            activeCollider.gameObject.SetActive(true);
            // Inicia una corutina para desactivar este hitbox después de un breve retraso.
            StartCoroutine(DisableHitboxAfterDelay(activeCollider, 0.2f)); // 0.2f segundos de duración del hitbox.
        }

        // Reproduce la animación de ataque a través de PlayerAnimation.
        playerAnimation.PlayAttackAnimation();
        OnAttack.Invoke(); // Dispara el evento de inicio de ataque.
        // Programa el fin del estado de ataque general después de un tiempo.
        // Este tiempo debe ser suficiente para que la animación y el hitbox hagan su efecto.
        Invoke(nameof(EndAttack), 0.5f); // 0.5f segundos de duración total del estado de ataque.
    }

    /// <summary>
    /// Corutina que desactiva el GameObject de un hitbox después de un retraso especificado.
    /// </summary>
    /// <param name="hitboxCollider">El Collider2D del hitbox a desactivar.</param>
    /// <param name="delay">Tiempo en segundos antes de desactivar el hitbox.</param>
    private IEnumerator DisableHitboxAfterDelay(Collider2D hitboxCollider, float delay)
    {
        yield return null; // Espera un frame para asegurar que OnEnable en AttackHitbox se ejecute primero.
        yield return new WaitForSeconds(delay); // Espera el tiempo de actividad del hitbox.
        if (hitboxCollider != null)
        {
            // Desactiva el GameObject al que pertenece el Collider.
            hitboxCollider.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Finaliza el estado de ataque del jugador.
    /// Se llama mediante Invoke desde el método Attack().
    /// </summary>
    private void EndAttack()
    {
        isAttacking = false; // Restablece el estado de ataque.
        OnAttackEnd.Invoke(); // Dispara el evento de fin de ataque.
    }

    /// <summary>
    /// Método de utilidad para desactivar todos los GameObjects de los hitboxes.
    /// Útil para la inicialización o para asegurar un estado limpio.
    /// </summary>
    private void DisableAllHitboxes()
    {
         if(hitboxUp != null) hitboxUp.gameObject.SetActive(false);
         if(hitboxDown != null) hitboxDown.gameObject.SetActive(false);
         if(hitboxLeft != null) hitboxLeft.gameObject.SetActive(false);
         if(hitboxRight != null) hitboxRight.gameObject.SetActive(false);
    }

    /// <summary>
    /// Devuelve si el jugador está actualmente en una secuencia de ataque.
    /// </summary>
    /// <returns>True si está atacando, false en caso contrario.</returns>
    public bool IsAttacking()
    {
        return isAttacking;
    }

    /// <summary>
    /// Devuelve el daño base por ataque del jugador.
    /// Usado por AttackHitbox para saber cuánto daño aplicar.
    /// </summary>
    /// <returns>El valor del daño.</returns>
    public float GetDamagePerAttack()
    {
        return damagePerAttack;
    }
}