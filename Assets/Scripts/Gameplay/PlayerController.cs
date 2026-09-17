using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Настройки движения")]
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 movementInput;
    private Vector2 lastMovementDirection = Vector2.right;

    public float SpeedMultiplier { get; set; } = 1f;
    public bool IsDodging { get; set; } = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.linearDamping = 15f;
    }

    public void SetMovementInput(Vector2 input)
    {
        movementInput = input;
        if (movementInput.magnitude > 1f)
        {
            movementInput.Normalize();
        }
        
        if (movementInput != Vector2.zero)
        {
            lastMovementDirection = movementInput;
        }
    }

    void FixedUpdate()
    {
        if (IsDodging) return;
        rb.linearVelocity = movementInput * moveSpeed * SpeedMultiplier;
    }

    public Vector2 GetLastMovementDirection()
    {
        return lastMovementDirection;
    }
}