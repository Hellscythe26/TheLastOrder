using UnityEngine;

/// <summary>
/// Controla las animaciones del jugador basándose en su movimiento y acciones de combate.
/// Actualiza los parámetros del Animator del jugador.
/// </summary>
public class PlayerAnimation : MonoBehaviour
{
    [Tooltip("Referencia al componente Animator del jugador.")]
    [SerializeField] private Animator animator;
    [Tooltip("Referencia al script PlayerCombat para saber si está atacando.")]
    [SerializeField] private PlayerCombat combat;
    // Guarda la última dirección en la que el jugador se movió, para las animaciones de idle.
    private Vector2 lastMoveDirection;

    /// <summary>
    /// Se llama una vez cuando el script es cargado.
    /// Obtiene referencias a los componentes Animator y PlayerCombat.
    /// Inicializa lastMoveDirection.
    /// </summary>
    private void Awake()
    {
        // Obtiene el componente Animator si no está asignado.
        if (animator == null) animator = GetComponent<Animator>();
        // Obtiene el componente PlayerCombat si no está asignado.
        if (combat == null) combat = GetComponent<PlayerCombat>();
        // Inicializa lastMoveDirection a 'abajo' por defecto si es cero.
        if (lastMoveDirection == Vector2.zero) {
            lastMoveDirection = Vector2.down;
        }
    }

    /// <summary>
    /// Actualiza los parámetros del Animator basados en la entrada de movimiento y la velocidad.
    /// También actualiza 'lastMoveDirection' si el jugador no está atacando.
    /// </summary>
    /// <param name="moveInput">El vector de entrada de movimiento normalizado del jugador.</param>
    /// <param name="moveSpeed">La velocidad actual de movimiento (no se usa directamente aquí pero podría ser útil).</param>
    public void UpdateAnimation(Vector2 moveInput, float moveSpeed) // moveSpeed no se usa actualmente aquí, pero se mantiene por si se necesita
    {
        if (animator == null) return; // No hacer nada si no hay Animator.
        // Establece los parámetros "MoveX" y "MoveY" para las animaciones de Blend Tree de movimiento.
        animator.SetFloat("MoveX", moveInput.x);
        animator.SetFloat("MoveY", moveInput.y);
        // Establece el booleano "IsMoving" si la magnitud del input es mayor que cero.
        animator.SetBool("IsMoving", moveInput.magnitude > 0.01f); // Un pequeño umbral para evitar "temblores"
        // Solo actualiza la dirección de "mirar" (lastMoveDirection) si no está atacando.
        // Esto evita que el sprite cambie de dirección a mitad de una animación de ataque.
        if (combat == null || !combat.IsAttacking())
        {
            // Si hay input de movimiento, actualiza la última dirección conocida.
            if (moveInput != Vector2.zero)
            {
                lastMoveDirection = moveInput.normalized;
            }
            // Actualiza los parámetros del Animator para la dirección de "mirar" en idle.
            animator.SetFloat("LastMoveX", lastMoveDirection.x);
            animator.SetFloat("LastMoveY", lastMoveDirection.y);
        }
    }

    /// <summary>
    /// Dispara el trigger "Attack" en el Animator para reproducir la animación de ataque.
    /// La dirección del ataque se basa en 'lastMoveDirection'.
    /// </summary>
    public void PlayAttackAnimation()
    {
        if (animator == null) return;
        // El Animator usará LastMoveX y LastMoveY para determinar la dirección del ataque.
        animator.SetTrigger("Attack");
    }

    /// <summary>
    /// Dispara la animación de muerte apropiada basándose en la última dirección de movimiento.
    /// </summary>
    public void PlayDeathAnimation()
    {
        if (animator == null) {
            Debug.LogError("PlayerAnimation: Animator no encontrado en PlayDeathAnimation!", this);
            return;
        }
        // Lógica para decidir qué animación de muerte usar (caer hacia adelante o atrás).
        // Aquí se asume una lógica simple basada en la última dirección.
        // Si la componente Y de lastMoveDirection es principalmente hacia arriba o
        // la componente X es principalmente hacia la izquierda, usa "DieLeftUp".
        // De lo contrario (principalmente abajo o derecha), usa "DieRightDown".
        // Ajusta esta lógica según tus animaciones específicas.
        if (lastMoveDirection.y > 0.1f || (Mathf.Abs(lastMoveDirection.y) < 0.1f && lastMoveDirection.x < -0.1f) )
        {
            animator.SetTrigger("DieLeftUp"); // Asumiendo triggers con estos nombres.
        }
        else
        {
            animator.SetTrigger("DieRightDown"); // Asumiendo triggers con estos nombres.
        }
    }

    /// <summary>
    /// Devuelve la última dirección en la que el jugador se movió o miró.
    /// Útil para determinar la dirección de un ataque u otras acciones.
    /// </summary>
    /// <returns>Un Vector2 normalizado representando la última dirección.</returns>
    public Vector2 GetLastMoveDirection()
    {
        // Devuelve la dirección guardada. Si es cero (al inicio), devuelve 'abajo' por defecto.
        if (lastMoveDirection == Vector2.zero) {
            return Vector2.down;
        }
        return lastMoveDirection;
    }
}