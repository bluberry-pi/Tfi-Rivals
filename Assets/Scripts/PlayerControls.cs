using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControls : MonoBehaviour
{
    public float speed = 5f;

    [Header("Facing Direction")]
    [Tooltip("Enable this if the source sprite naturally faces left (e.g. Pawan)")]
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

    private float punchTimer;

    [Header("Kick")]
    public bool isKickPressed = false;
    public float kickTime = 0.2f;

    private float kickTimer;

    public float horizontal;

    private Rigidbody2D rb;

    [Header("Debug Info")]
    [SerializeField] private bool isGrounded;
    [SerializeField] private bool jumpPressed;
    [SerializeField] private bool jumpHeld;
    private float jumpTime;

    float tiltTimer;
    bool tiltRight;

    // START

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

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

        if (isKickPressed)
        {
            Kick();
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
            float scaleX = defaultFacingLeft ? -Mathf.Abs(transform.localScale.x) : Mathf.Abs(transform.localScale.x);
            transform.localScale = new Vector3(scaleX, transform.localScale.y, transform.localScale.z);
        }
        else if (horizontal < 0)
        {
            // Face left
            float scaleX = defaultFacingLeft ? Mathf.Abs(transform.localScale.x) : -Mathf.Abs(transform.localScale.x);
            transform.localScale = new Vector3(scaleX, transform.localScale.y, transform.localScale.z);
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

    public void Kick()
    {
        kickTimer -= Time.deltaTime;

        if (kickTimer <= 0f)
        {
            isKickPressed = false;
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
        if (context.started)
        {
            if (isGrounded)
            {
                jumpPressed = true;
                jumpHeld = true;
                Debug.Log("<color=cyan>[Jump] Button pressed: CAN JUMP (Character is grounded: true)</color>");
            }
            else
            {
                Debug.LogWarning("<color=orange>[Jump] Button pressed: CANNOT JUMP (Character is in air / grounded: false)</color>");
            }
        }

        if (context.canceled)
        {
            jumpHeld = false;
            Debug.Log("[Jump] Button released (jumpHeld: false)");
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

    public void Kick(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            isKickPressed = true;
            kickTimer = kickTime;
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
        groundTriggerCount = 0;
        Debug.Log($"<color=cyan>[Jump] Executed Jump! Velocity: {rb.linearVelocity}, Force: {jumpForce}, isGrounded set to false (in air)</color>");
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

    // GROUND CHECK (TRIGGER)

    private int groundTriggerCount = 0;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Ground"))
        {
            groundTriggerCount++;
            isGrounded = true;
            rb.gravityScale = 1f;
            Debug.Log($"<color=green>[GroundCheck] Trigger entered '{other.name}' (Tag: Ground) -> GROUNDED: TRUE (Can jump)</color>");
        }
        else
        {
            Debug.Log($"[GroundCheck] Trigger touched '{other.name}' (Tag: '{other.tag}')");
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Ground") && rb.linearVelocity.y <= 0.1f)
        {
            isGrounded = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Ground"))
        {
            groundTriggerCount = Mathf.Max(0, groundTriggerCount - 1);
            if (groundTriggerCount == 0)
            {
                isGrounded = false;
                Debug.Log($"<color=yellow>[GroundCheck] Trigger left '{other.name}' (Tag: Ground) -> IN AIR / GROUNDED: FALSE (Cannot jump)</color>");
            }
            else
            {
                Debug.Log($"[GroundCheck] Trigger left one ground surface, but still overlapping another -> GROUNDED remains TRUE");
            }
        }
    }
}