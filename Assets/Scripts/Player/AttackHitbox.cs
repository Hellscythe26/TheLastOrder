using UnityEngine;
using System.Collections.Generic; // Necesario para List<Collider2D>

/// <summary>
/// Controla el comportamiento de un hitbox de ataque del jugador.
/// Detecta colisiones con objetos que implementan la interfaz IDamageable
/// y les aplica daño. Evita aplicar daño múltiple al mismo objetivo en un solo ataque.
/// </summary>
public class AttackHitbox : MonoBehaviour
{
    // Referencia al script PlayerCombat para obtener el valor del daño.
    private PlayerCombat playerCombat;
    // Daño a aplicar, obtenido de PlayerCombat.
    private float damage;
    // Lista para rastrear los enemigos ya golpeados durante la activación actual de este hitbox,
    // para prevenir daño múltiple en un solo swing/ataque.
    private List<Collider2D> hitEnemiesThisAttack;

    /// <summary>
    /// Se llama una vez cuando el script es cargado.
    /// Inicializa la referencia a PlayerCombat, obtiene el daño del ataque,
    /// y se asegura de que el Collider2D de este hitbox esté configurado como Trigger.
    /// </summary>
    private void Awake()
    {
        // Intenta obtener el componente PlayerCombat del objeto padre (el Jugador).
        playerCombat = GetComponentInParent<PlayerCombat>();
        if (playerCombat != null)
        {
            // Obtiene el valor de daño desde PlayerCombat.
            damage = playerCombat.GetDamagePerAttack();
        }
        else
        {
            Debug.LogError("AttackHitbox no pudo encontrar PlayerCombat en el padre! Usando daño por defecto.", this);
            damage = 1f; // Valor de daño por defecto si no se encuentra PlayerCombat.
        }

        // Verifica y advierte si el Collider2D no está configurado como Trigger.
        Collider2D col = GetComponent<Collider2D>();
        if (col == null || !col.isTrigger)
        {
             Debug.LogWarning($"AttackHitbox en {gameObject.name} necesita un Collider2D configurado como 'Is Trigger' para funcionar correctamente.", this);
        }
    }

    /// <summary>
    /// Se llama cada vez que el GameObject de este hitbox se activa.
    /// Prepara la lista 'hitEnemiesThisAttack' para un nuevo barrido de ataque.
    /// </summary>
    private void OnEnable()
    {
        // Inicializa la lista si es la primera vez o la resetea.
        if (hitEnemiesThisAttack == null)
        {
             hitEnemiesThisAttack = new List<Collider2D>();
        }
        hitEnemiesThisAttack.Clear(); // Limpia la lista de enemigos golpeados del ataque anterior.
    }

    /// <summary>
    /// Se llama automáticamente por Unity cuando otro Collider2D entra en el Trigger de este hitbox.
    /// Comprueba si el objeto colisionado es dañable y le aplica daño si no ha sido golpeado aún en este ataque.
    /// </summary>
    /// <param name="otherCollider">El Collider2D del objeto que entró en el trigger.</param>
    private void OnTriggerEnter2D(Collider2D otherCollider)
    {
        // Si el 'otherCollider' ya fue golpeado en esta activación del hitbox, ignóralo.
        if (hitEnemiesThisAttack.Contains(otherCollider))
        {
            return;
        }
        // Intenta obtener la interfaz IDamageable del objeto colisionado.
        IDamageable damageable = otherCollider.GetComponent<IDamageable>();
        // Si el objeto es dañable (implementa IDamageable).
        if (damageable != null)
        {
            // Debug.Log($"{gameObject.name} detectó IDamageable en {otherCollider.gameObject.name}");
            // Llama al método TakeDamage del objeto dañable.
            damageable.TakeDamage(damage);

            // Añade el collider del enemigo a la lista para no volver a golpearlo en este mismo ataque.
            hitEnemiesThisAttack.Add(otherCollider);
            // Aquí se podrían instanciar efectos visuales/sonoros de golpe.
        }
    }
}