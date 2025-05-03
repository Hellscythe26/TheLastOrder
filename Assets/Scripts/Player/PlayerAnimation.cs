using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerCombat combat;
    private Vector2 lastMoveDirection;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (combat == null)
        {
            combat = GetComponent<PlayerCombat>();
        }
        if (lastMoveDirection == Vector2.zero) {
            lastMoveDirection = Vector2.down;
        }
    }

    public void UpdateAnimation(Vector2 moveInput, float moveSpeed)
    {
        animator.SetFloat("MoveX", moveInput.x);
        animator.SetFloat("MoveY", moveInput.y);
        animator.SetBool("IsMoving", moveInput.magnitude > 0);
        if (combat == null || !combat.IsAttacking()) // Añadir null check por seguridad
        {
            // Actualiza la dirección guardada si hay input de movimiento
            if (moveInput != Vector2.zero)
            {
                lastMoveDirection = moveInput.normalized;
            }
            // Actualiza los parámetros del Animator para la dirección de mirar
            animator.SetFloat("LastMoveX", lastMoveDirection.x);
            animator.SetFloat("LastMoveY", lastMoveDirection.y);
        }
    }

    public void PlayAttackAnimation()
    {
        animator.SetTrigger("Attack");
    }

    public void PlayDeathAnimation()
    {
        if (animator == null) {
            Debug.LogError("Animator no encontrado en PlayDeathAnimation!");
            return;
        }
        // Decide qué animación reproducir basado en lastMoveDirection
        // Lógica: Derecha (x+) o Abajo (y-) -> DieRightD
        // Lógica: Izquierda (x-) y Arriba (y+) -> DieLeftUp
        if (lastMoveDirection.x > 0 || lastMoveDirection.y < 0)
        {
            animator.SetTrigger("DieRightDown");
        }
        else
        {
            animator.SetTrigger("DieLeftUp");
        }
    }

    public Vector2 GetLastMoveDirection()
    {
        // Devuelve la dirección guardada.
        if (lastMoveDirection == Vector2.zero) {
            return Vector2.down; // Dirección por defecto
        }
        return lastMoveDirection;
    }
}