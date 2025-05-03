using UnityEngine;
using System.Collections.Generic; // Para la lista de enemigos golpeados

public class AttackHitbox : MonoBehaviour
{
    // Podrías obtener el daño del PlayerCombat o ponerlo aquí si varía por hitbox
    // Para obtenerlo del PlayerCombat, necesitarías una referencia:
    private PlayerCombat playerCombat;
    private float damage;

    // Lista para evitar golpear al mismo enemigo varias veces en un solo ataque
    private List<Collider2D> hitEnemiesThisAttack;

    private void Awake()
    {
        // Obtener referencia al script principal de combate en el objeto padre (Player)
        playerCombat = GetComponentInParent<PlayerCombat>();
        if (playerCombat != null)
        {
             damage = playerCombat.GetDamagePerAttack();
        }
        else
        {
            Debug.LogError("AttackHitbox no pudo encontrar PlayerCombat en el padre!", this);
            damage = 1f; // Valor por defecto si falla
        }

        // Asegurarse de que el collider sea Trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col == null || !col.isTrigger)
        {
             Debug.LogWarning($"AttackHitbox en {gameObject.name} necesita un Collider2D configurado como 'Is Trigger'.", this);
        }
    }

    // Se llama cuando este hitbox se activa
    private void OnEnable()
    {
        // Limpiar la lista de enemigos golpeados cada vez que el hitbox se activa
        if (hitEnemiesThisAttack == null)
        {
             hitEnemiesThisAttack = new List<Collider2D>();
        }
        hitEnemiesThisAttack.Clear();
    }


    // Se llama automáticamente por Unity cuando otro Collider2D entra en este Trigger
    private void OnTriggerEnter2D(Collider2D otherCollider)
    {
        // Comprobar si ya hemos golpeado a este enemigo en este ataque
        if (hitEnemiesThisAttack.Contains(otherCollider))
        {
            return; // Ya golpeado, no hacer nada
        }

        // Comprobar si el objeto que entró tiene la interfaz IDamageable
        IDamageable damageable = otherCollider.GetComponent<IDamageable>();
         // Opcional: Comprobar también si está en la capa correcta (si PlayerCombat no tuviera LayerMask)
         // bool isEnemy = enemyLayers == (enemyLayers | (1 << otherCollider.gameObject.layer));


        if (damageable != null)
        {
            Debug.Log($"{gameObject.name} detectó IDamageable en {otherCollider.gameObject.name}");
            damageable.TakeDamage(damage);

            // Añadir a la lista para no volver a golpearlo en este mismo ataque
            hitEnemiesThisAttack.Add(otherCollider);

            // Opcional: Añadir efecto de golpe aquí (partículas, sonido)
        }
         // else { Debug.Log($"{gameObject.name} colisionó con {otherCollider.gameObject.name} pero no tiene IDamageable."); }

    }

     // Opcional: OnDisable para limpieza si fuera necesario
     // private void OnDisable() { }
}