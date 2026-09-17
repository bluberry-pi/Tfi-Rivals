using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControls : MonoBehaviour
{
    public float speed = 5f;

    [Header("Jump")]
    public float jumpForce = 10f;
    public float jumpHoldForce = 10f;
    public float maxJumpTime = 0.1f;

    [Header("Jump Gravity")]
    public float jumpGravity = 4f;
    public float fallGravity = 5f;

    [Header("Running Tilt")]
    public bool enableRunningTilt = true;
    public bool smoothTilt = true;
    [Header("Punch")]
    public bool isPunchPressed = false;
    public float punchTime = 0.2f;

    private float punchTimer;

    public float runTiltAmount = 6f;
    public float runTiltSpeed = 7f;
    public float tiltSmoothSpeed = 12f;

    public float horizontal;

    private Rigidbody2D rb;

    bool jumpPressed;
    bool jumpHeld;
    float jumpTime;
    bool isGrounded;

    float tiltTimer;
    bool tiltRight;

    // START

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        transform.localScale = new Vector3(1, 1, 1);
        transform.localRotation = Quaternion.identity;
    }

    // UPDATE

    void Update()
    {
        RunningJuice();

        if (isPunchPressed)
        {
            Punch();
        }
    }

    // FIXED UPDATE

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

    // MOVEMENT

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

    public void Punch()
    {
        punchTimer -= Time.deltaTime;

        if (punchTimer <= 0f)
        {
            isPunchPressed = false;
        }
    }

    // RUNNING TILT

    void RunningJuice()
    {
        // MASTER TOGGLE
        if (!enableRunningTilt)
        {
            tiltTimer = 0f;
            tiltRight = false;

            // Make absolutely sure the character is upright
            transform.localRotation = Quaternion.identity;

            return;
        }

        // NOT MOVING

        if (Mathf.Abs(horizontal) < 0.01f)
        {
            tiltTimer = 0f;
            tiltRight = false;

            if (smoothTilt)
            {
                transform.localRotation = Quaternion.Lerp(
                    transform.localRotation,
                    Quaternion.identity,
                    tiltSmoothSpeed * Time.deltaTime
                );
            }
            else
            {
                transform.localRotation = Quaternion.identity;
            }

            return;
        }

        // RUNNING

        tiltTimer += Time.deltaTime;

        float switchTime = 1f / runTiltSpeed;

        if (tiltTimer >= switchTime)
        {
            tiltTimer = 0f;
            tiltRight = !tiltRight;
        }

        // Choose left or right tilt
        float tilt = tiltRight
            ? runTiltAmount
            : -runTiltAmount;

        // Reverse tilt when moving left
        tilt *= Mathf.Sign(horizontal);

        Quaternion targetRotation = Quaternion.Euler(
            0f,
            0f,
            tilt
        );

        // SMOOTH / INSTANT

        if (smoothTilt)
        {
            transform.localRotation = Quaternion.Lerp(
                transform.localRotation,
                targetRotation,
                tiltSmoothSpeed * Time.deltaTime
            );
        }
        else
        {
            // Instant snap
            transform.localRotation = targetRotation;
        }
    }

    // JUMP INPUT

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

    public void Punch(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            isPunchPressed = true;
            punchTimer = punchTime;
        }
    }

    // JUMP

    public void Jump()
    {
        rb.linearVelocity = new Vector2(
            horizontal * speed,
            jumpForce
        );

        isGrounded = false;
    }

    // VARIABLE JUMP

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

    // BETTER GRAVITY

    public void BetterGravity()
    {
        if (rb.linearVelocity.y > 0)
        {
            rb.gravityScale = jumpGravity;
        }
        else
        {
            rb.gravityScale = fallGravity;
        }
    }

    // GROUND CHECK

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