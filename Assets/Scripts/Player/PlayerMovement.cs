using UnityEngine;

public class PlayerMovement : MonoBehaviour, IMovable
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Rigidbody2D rb;
    private Vector2 moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void HandleInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        moveInput = new Vector2(horizontal, vertical).normalized;
    }

    public void Move(Vector2 direction, float speed)
    {
        rb.linearVelocity = direction * speed;
    }

    public Vector2 GetMoveInput()
    {
        return moveInput;
    }

    public float GetMoveSpeed()
    {
        return moveSpeed;
    }

    public void StopMoving()
    {
        rb.linearVelocity = Vector2.zero;
    }
}