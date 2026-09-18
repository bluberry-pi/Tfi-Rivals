using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControls : MonoBehaviour
{
    public float speed = 5f;

    [Header("Facing Direction")]
    public bool defaultFacingLeft = false;

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
    public float runTiltAmount = 6f;
    public float runTiltSpeed = 7f;
    public float tiltSmoothSpeed = 12f;

    [Header("Punch")]
    public bool isPunchPressed = false;
    public float punchTime = 0.2f;
    public float punchCooldown = 0.12f;

    [Header("Kick")]
    public bool isKickPressed = false;
    public float kickTime = 0.2f;
    public float kickCooldown = 0.12f;

    [Header("Block")]
    public bool isBlockPressed = false;

    private PlayerTransition playerTransition;
    public float horizontal;

    private Rigidbody2D rb;

    [Header("Debug Info")]
    [SerializeField] private bool isGrounded;
    [SerializeField] private bool jumpPressed;
    [SerializeField] private bool jumpHeld;

    private float jumpTime;

    float tiltTimer;
    bool tiltRight;

    void Awake()
    {
        playerTransition = GetComponent<PlayerTransition>();
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (playerTransition == null)
            playerTransition = GetComponent<PlayerTransition>();

        transform.localRotation = Quaternion.identity;
    }

    void Update()
    {
        RunningJuice();
    }

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

    // =========================================================
    // MOVEMENT INPUT
    // =========================================================

    public void Move(InputAction.CallbackContext context)
    {
        horizontal = context.ReadValue<Vector2>().x;

        if (horizontal > 0)
        {
            float scaleX = defaultFacingLeft
                ? -Mathf.Abs(transform.localScale.x)
                : Mathf.Abs(transform.localScale.x);

            transform.localScale = new Vector3(
                scaleX,
                transform.localScale.y,
                transform.localScale.z
            );
        }
        else if (horizontal < 0)
        {
            float scaleX = defaultFacingLeft
                ? Mathf.Abs(transform.localScale.x)
                : -Mathf.Abs(transform.localScale.x);

            transform.localScale = new Vector3(
                scaleX,
                transform.localScale.y,
                transform.localScale.z
            );
        }
    }

    public void Movement()
    {
        // When block is on, the player cannot move
        if (isBlockPressed || (playerTransition != null && playerTransition.IsBlocking))
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        rb.linearVelocity = new Vector2(
            horizontal * speed,
            rb.linearVelocity.y
        );
    }

    // =========================================================
    // PUNCH
    // =========================================================

    public void Punch()
    {
        if (isBlockPressed || (playerTransition != null && playerTransition.IsBlocking))
            return;

        if (playerTransition != null)
        {
            playerTransition.TriggerPunch();
        }
    }

    public void Punch(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (isBlockPressed || (playerTransition != null && playerTransition.IsBlocking))
                return;

            if (playerTransition != null)
            {
                playerTransition.OnPunchPressed();
            }
            else
            {
                isPunchPressed = true;
            }
        }
        else if (context.canceled)
        {
            if (playerTransition != null)
            {
                playerTransition.OnPunchReleased();
            }
            else
            {
                isPunchPressed = false;
            }
        }
    }

    // =========================================================
    // KICK
    // =========================================================

    public void Kick()
    {
        if (isBlockPressed || (playerTransition != null && playerTransition.IsBlocking))
            return;

        if (playerTransition != null)
        {
            playerTransition.TriggerKick();
        }
    }

    public void Kick(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (isBlockPressed || (playerTransition != null && playerTransition.IsBlocking))
                return;

            if (playerTransition != null)
            {
                playerTransition.OnKickPressed();
            }
            else
            {
                isKickPressed = true;
            }
        }
        else if (context.canceled)
        {
            if (playerTransition != null)
            {
                playerTransition.OnKickReleased();
            }
            else
            {
                isKickPressed = false;
            }
        }
    }

    // =========================================================
    // BLOCK
    // =========================================================

    public void Block(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            isBlockPressed = true;
            if (playerTransition != null)
            {
                playerTransition.OnBlockPressed();
            }
        }
        else if (context.canceled)
        {
            isBlockPressed = false;
            if (playerTransition != null)
            {
                playerTransition.OnBlockReleased();
            }
        }
    }

    // =========================================================
    // RUNNING TILT
    // =========================================================

    void RunningJuice()
    {
        if (isBlockPressed || (playerTransition != null && playerTransition.IsBlocking))
        {
            tiltTimer = 0f;
            tiltRight = false;
            transform.localRotation = Quaternion.identity;
            return;
        }

        if (!enableRunningTilt)
        {
            tiltTimer = 0f;
            tiltRight = false;

            transform.localRotation = Quaternion.identity;
            return;
        }

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

        tiltTimer += Time.deltaTime;

        float switchTime = 1f / runTiltSpeed;

        if (tiltTimer >= switchTime)
        {
            tiltTimer = 0f;
            tiltRight = !tiltRight;
        }

        float tilt = tiltRight
            ? runTiltAmount
            : -runTiltAmount;

        tilt *= Mathf.Sign(horizontal);

        Quaternion targetRotation = Quaternion.Euler(
            0f,
            0f,
            tilt
        );

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
            transform.localRotation = targetRotation;
        }
    }

    // =========================================================
    // JUMP INPUT
    // =========================================================

    public void Jump(InputAction.CallbackContext context)
    {
        if (isBlockPressed || (playerTransition != null && playerTransition.IsBlocking))
            return;

        if (context.started)
        {
            if (isGrounded)
            {
                jumpPressed = true;
                jumpHeld = true;
            }
        }

        if (context.canceled)
        {
            jumpHeld = false;
        }
    }

    // =========================================================
    // JUMP
    // =========================================================

    public void Jump()
    {
        rb.linearVelocity = new Vector2(
            horizontal * speed,
            jumpForce
        );

        isGrounded = false;
        groundTriggerCount = 0;
    }

    // =========================================================
    // VARIABLE JUMP
    // =========================================================

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

    // =========================================================
    // BETTER GRAVITY
    // =========================================================

    public void BetterGravity()
    {
        if (isGrounded)
        {
            rb.gravityScale = 1f;
        }
        else if (rb.linearVelocity.y > 0)
        {
            rb.gravityScale = jumpGravity;
        }
        else
        {
            rb.gravityScale = fallGravity;
        }
    }

    // =========================================================
    // GROUND CHECK
    // =========================================================

    private int groundTriggerCount = 0;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Ground"))
        {
            groundTriggerCount++;

            isGrounded = true;
            rb.gravityScale = 1f;
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Ground") &&
            rb.linearVelocity.y <= 0.1f)
        {
            isGrounded = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Ground"))
        {
            groundTriggerCount = Mathf.Max(
                0,
                groundTriggerCount - 1
            );

            if (groundTriggerCount == 0)
            {
                isGrounded = false;
            }
        }
    }
}