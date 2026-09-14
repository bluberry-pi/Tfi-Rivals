using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    public float jumpForce = 5f;
    public float jumpHoldForce = 10f;
    public float maxJumpTime = 0.2f;
    float horizontal;
    private Rigidbody2D rb;
    bool jumpPressed;
    bool jumpHeld;
    float jumpTime;
    bool isGrounded;
    //-------------------------------------------------------------------------------------------------
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    //-------------------------------------------------------------------------------------------------
    void FixedUpdate()
    {
        Movement();

        if (jumpPressed)
        {
            Jump();
            jumpPressed = false;
            jumpTime = 0;
        }
        variableJump();
    }
    //-------------------------------------------------------------------------------------------------
    public void Move(InputAction.CallbackContext context)
    {
        horizontal = context.ReadValue<Vector2>().x;
    }

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

    public void Movement()
    {
        rb.linearVelocity = new Vector2(horizontal * speed, rb.linearVelocity.y);
    }

    public void Jump()
    {
        rb.linearVelocity = new Vector2(horizontal * speed, jumpForce);
        isGrounded = false;
    }

    public void variableJump()
    {
        if (jumpHeld && rb.linearVelocity.y > 0 && jumpTime < maxJumpTime)
        {
            rb.AddForce(Vector2.up * jumpHoldForce);
            jumpTime = jumpTime + Time.fixedDeltaTime;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
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
