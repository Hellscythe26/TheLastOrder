/// <summary>
/// Interfaz para objetos que pueden recibir daño y tienen un estado de "vida".
/// Define un contrato común para la interacción de combate.
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// Aplica una cantidad de daño al objeto.
    /// </summary>
    /// <param name="damage">La cantidad de daño a infligir.</param>
    void TakeDamage(float damage);

    /// <summary>
    /// Comprueba si el objeto todavía está "vivo" o funcional.
    /// </summary>
    /// <returns>True si está vivo, false en caso contrario.</returns>
    bool IsAlive();
}