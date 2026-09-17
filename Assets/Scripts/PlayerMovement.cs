using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;

    [Header("Jump")]
    public float jumpForce = 10f;
    public float jumpHoldForce = 10f;
    public float maxJumpTime = 0.1f;

    [Header("Jump Gravity")]
    public float jumpGravity = 4f;
    public float fallGravity = 5f;

    float horizontal;

    private Rigidbody2D rb;

    bool jumpPressed;
    bool jumpHeld;
    float jumpTime;
    bool isGrounded;

    // --------------------------------------------------
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        transform.localScale = new Vector3(1, 1, 1);
    }

    // --------------------------------------------------
    void FixedUpdate()
    {
        Movement();

        if (jumpPressed)
        {
            Jump();
            jumpPressed = false;
            jumpTime = 0;
        }

        VariableJump();
        BetterGravity();
    }

    // --------------------------------------------------
    // MOVEMENT
    // --------------------------------------------------

    public void Move(InputAction.CallbackContext context)
    {
        horizontal = context.ReadValue<Vector2>().x;

        if (horizontal > 0)
        {
            // Face right
            transform.localScale = new Vector3(1, 1, 1);
        }
        else if (horizontal < 0)
        {
            // Face left
            transform.localScale = new Vector3(-1, 1, 1);
        }
    }

    public void Movement()
    {
        rb.linearVelocity = new Vector2(
            horizontal * speed,
            rb.linearVelocity.y
        );
    }

    // --------------------------------------------------
    // JUMP INPUT
    // --------------------------------------------------

    public void Jump(InputAction.CallbackContext context)
    {
        if (context.started && isGrounded)
        {
            jumpPressed = true;
            jumpHeld = true;
        }

        if (context.canceled)
        {
            jumpHeld = false;
        }
    }

    // --------------------------------------------------
    // JUMP
    // --------------------------------------------------

    public void Jump()
    {
        rb.linearVelocity = new Vector2(
            horizontal * speed,
            jumpForce
        );

        isGrounded = false;
    }

    // --------------------------------------------------
    // VARIABLE JUMP
    // --------------------------------------------------

    public void VariableJump()
    {
        if (jumpHeld &&
            rb.linearVelocity.y > 0 &&
            jumpTime < maxJumpTime)
        {
            rb.AddForce(Vector2.up * jumpHoldForce);
            jumpTime += Time.fixedDeltaTime;
        }
    }

    // --------------------------------------------------
    // BETTER GRAVITY
    // --------------------------------------------------

    public void BetterGravity()
    {
        // Going UP
        if (rb.linearVelocity.y > 0)
        {
            rb.gravityScale = jumpGravity;
        }
        // Going DOWN
        else
        {
            rb.gravityScale = fallGravity;
        }
    }

    // --------------------------------------------------
    // GROUND CHECK
    // --------------------------------------------------

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
            rb.gravityScale = 1f;
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }
}