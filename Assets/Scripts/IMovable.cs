using UnityEngine; // Necesario para Vector2.

/// <summary>
/// Interfaz para objetos que pueden ser controlados para moverse.
/// Define un contrato para la lógica de movimiento.
/// </summary>
public interface IMovable
{
    /// <summary>
    /// Aplica movimiento al objeto en una dirección y velocidad específicas.
    /// </summary>
    /// <param name="direction">El vector de dirección del movimiento (usualmente normalizado).</param>
    /// <param name="speed">La magnitud de la velocidad del movimiento.</param>
    void Move(Vector2 direction, float speed);
}