using UnityEngine;

/// <summary>
/// Gestiona la entrada de teclado para el movimiento del jugador y aplica
/// el movimiento físico utilizando un Rigidbody2D.
/// Implementa la interfaz IMovable.
/// </summary>
public class PlayerMovement : MonoBehaviour, IMovable
{
    [Tooltip("Velocidad de movimiento del jugador.")]
    [SerializeField] private float moveSpeed = 5f;
    [Tooltip("Referencia al componente Rigidbody2D del jugador.")]
    [SerializeField] private Rigidbody2D rb;
    private Vector2 moveInput; // Almacena el vector de entrada de movimiento normalizado.

    /// <summary>
    /// Se llama una vez cuando el script es cargado.
    /// Obtiene la referencia al Rigidbody2D.
    /// </summary>
    private void Awake()
    {
        // Obtiene el componente Rigidbody2D si no está asignado en el Inspector.
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rb == null) Debug.LogError("PlayerMovement: Rigidbody2D no encontrado en " + gameObject.name, this);
    }

    /// <summary>
    /// Procesa la entrada del teclado (Horizontal y Vertical) para determinar la dirección de movimiento.
    /// El vector resultante es normalizado para asegurar velocidad constante en diagonal.
    /// Este método debe ser llamado en Update desde el script Player principal.
    /// </summary>
    public void HandleInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal"); // Entrada cruda para movimiento inmediato.
        float vertical = Input.GetAxisRaw("Vertical");
        // Normaliza el vector para que la velocidad diagonal no sea mayor.
        moveInput = new Vector2(horizontal, vertical).normalized;
    }

    /// <summary>
    /// Aplica el movimiento al Rigidbody2D del jugador estableciendo su velocidad lineal.
    /// Este método debe ser llamado en Update o FixedUpdate desde el script Player principal.
    /// </summary>
    /// <param name="direction">El vector de dirección del movimiento (usualmente moveInput).</param>
    /// <param name="speed">La velocidad a la que se moverá el jugador.</param>
    public void Move(Vector2 direction, float speed)
    {
        if (rb == null) return; // No hacer nada si no hay Rigidbody.
        // Establece la velocidad lineal del Rigidbody para mover al jugador.
        rb.linearVelocity = direction * speed;
    }

    /// <summary>
    /// Devuelve el vector de entrada de movimiento actual (normalizado).
    /// </summary>
    /// <returns>El vector de movimiento normalizado.</returns>
    public Vector2 GetMoveInput()
    {
        return moveInput;
    }

    /// <summary>
    /// Devuelve la velocidad de movimiento configurada para el jugador.
    /// </summary>
    /// <returns>La velocidad de movimiento.</returns>
    public float GetMoveSpeed()
    {
        return moveSpeed;
    }

    /// <summary>
    /// Detiene inmediatamente el movimiento del jugador estableciendo la velocidad lineal del Rigidbody a cero.
    /// </summary>
    public void StopMoving()
    {
        if (rb == null) return;
        rb.linearVelocity = Vector2.zero;
        moveInput = Vector2.zero; // También resetea el input para evitar movimiento residual en el siguiente frame.
    }
}