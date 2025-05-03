using UnityEngine;
using UnityEngine.Events;
using System.Collections; // Necesario para Corutinas

public class PlayerCombat : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float damagePerAttack = 1f;

    [Header("Components")]
    [SerializeField] private PlayerAnimation playerAnimation;

    [Header("Attack Hitboxes (Colliders)")] // Cambiado el encabezado
    [Tooltip("El Collider2D del hitbox para atacar hacia arriba.")]
    [SerializeField] private Collider2D hitboxUp; // <-- Cambiado a Collider2D
    [Tooltip("El Collider2D del hitbox para atacar hacia abajo.")]
    [SerializeField] private Collider2D hitboxDown; // <-- Cambiado a Collider2D
    [Tooltip("El Collider2D del hitbox para atacar hacia la izquierda.")]
    [SerializeField] private Collider2D hitboxLeft; // <-- Cambiado a Collider2D
    [Tooltip("El Collider2D del hitbox para atacar hacia la derecha.")]
    [SerializeField] private Collider2D hitboxRight; // <-- Cambiado a Collider2D

    [Header("State & Events")]
    [SerializeField] private bool isAttacking = false;
    public UnityEvent OnAttack;
    public UnityEvent OnAttackEnd;

    private void Awake()
    {
        if (playerAnimation == null) playerAnimation = GetComponent<PlayerAnimation>();
        if (OnAttack == null) OnAttack = new UnityEvent();
        if (OnAttackEnd == null) OnAttackEnd = new UnityEvent();
        // Es buena idea desactivar todos los hitboxes al inicio por si acaso
        DisableAllHitboxes();
    }

    public void Attack()
    {
        if (isAttacking) return;
        if (playerAnimation == null) { Debug.LogError("PlayerAnimation no está asignado."); return; }

        isAttacking = true;
        Vector2 attackDirection = playerAnimation.GetLastMoveDirection();
        Collider2D activeCollider = null; // <-- Variable para guardar el Collider activo

        // Determinar qué collider activar
        if (Mathf.Abs(attackDirection.x) > Mathf.Abs(attackDirection.y))
        {
            activeCollider = (attackDirection.x > 0) ? hitboxRight : hitboxLeft;
        }
        else
        {
            activeCollider = (attackDirection.y > 0) ? hitboxUp : hitboxDown;
        }

        if (activeCollider != null)
        {
            // Activar el GameObject al que pertenece el Collider
            activeCollider.gameObject.SetActive(true);
            // Iniciar corutina para desactivarlo después
            StartCoroutine(DisableHitboxAfterDelay(activeCollider, 0.2f)); // Pasar el Collider2D
        }

        // Reproducir animación y manejar estado/eventos
        if(playerAnimation != null) playerAnimation.PlayAttackAnimation();
        OnAttack.Invoke();

        // Terminar el estado de ataque general
        Invoke(nameof(EndAttack), 0.5f);
    }

    // Corutina ahora acepta un Collider2D
    private IEnumerator DisableHitboxAfterDelay(Collider2D hitboxCollider, float delay)
    {
        yield return null; // Esperar un frame
        yield return new WaitForSeconds(delay);

        if (hitboxCollider != null)
        {
            // Desactivar el GameObject al que pertenece el Collider
            hitboxCollider.gameObject.SetActive(false);
        }
    }

    private void EndAttack()
    {
        isAttacking = false;
        OnAttackEnd.Invoke();
        // No necesitamos desactivar explícitamente aquí si la corutina lo hace,
        // pero podría ser un seguro extra llamar a DisableAllHitboxes() aquí si hubiera problemas.
        // DisableAllHitboxes();
    }

     // Método helper para asegurar que todos estén desactivados al inicio o al final
    private void DisableAllHitboxes()
    {
         if(hitboxUp != null) hitboxUp.gameObject.SetActive(false);
         if(hitboxDown != null) hitboxDown.gameObject.SetActive(false);
         if(hitboxLeft != null) hitboxLeft.gameObject.SetActive(false);
         if(hitboxRight != null) hitboxRight.gameObject.SetActive(false);
    }

    public bool IsAttacking()
    {
        return isAttacking;
    }

    public float GetDamagePerAttack()
    {
        return damagePerAttack;
    }
}