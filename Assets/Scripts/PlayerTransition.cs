using UnityEngine;
using System.Collections;

public class PlayerTransition : MonoBehaviour
{
    [Header("Character States")]
    public GameObject idle;
    public GameObject run;
    public GameObject punch;
    public GameObject kick;
    public GameObject block;

    [Header("Transition Timing")]
    public float transitionTime = 0.2f;

    [Header("Attack Recovery / Cooldown")]
    public float punchCooldown = 0.12f;
    public float kickCooldown = 0.12f;

    public bool autoRepeatOnHold = false;

    private PlayerControls playerMovement;

    [Header("Hit Glow Settings")]
    public Material glowMaterial;
    public Color hitGlowColor = Color.white;
    public float hitGlowIntensity = 2f;
    public float hitGlowDuration = 0.15f;

    private SpriteGlowFade hitGlow;

    private bool isRunning = false;
    private bool isTransitioning = false;
    private bool isAttacking = false;

    private bool punchBuffered = false;
    private bool kickBuffered = false;

    private bool punchHeld = false;
    private bool kickHeld = false;

    private bool blockHeld = false;

    private Coroutine currentAttackRoutine = null;
    private Coroutine currentMovementRoutine = null;

    public bool IsAttacking => isAttacking;
    public bool IsBlocking => blockHeld;

    void Awake()
    {
        playerMovement = GetComponent<PlayerControls>();
    }

    void Start()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerControls>();

        if (block == null)
        {
            Transform found = transform.Find("Sheild") ?? transform.Find("Shield") ?? transform.Find("Block");
            if (found != null)
                block = found.gameObject;
        }

        EnsureHitGlow();

        if (block != null)
            block.SetActive(false);

        SetIdle();
    }

    void OnDisable()
    {
        StopAllCoroutines();

        currentAttackRoutine = null;
        currentMovementRoutine = null;

        isAttacking = false;
        isTransitioning = false;

        punchBuffered = false;
        kickBuffered = false;

        punchHeld = false;
        kickHeld = false;
        blockHeld = false;

        if (playerMovement != null)
        {
            playerMovement.isPunchPressed = false;
            playerMovement.isKickPressed = false;
            playerMovement.isBlockPressed = false;
        }

        if (block != null)
            block.SetActive(false);
    }

    // =========================================================
    // UPDATE
    // =========================================================

    void Update()
    {
        // While blocking, do absolutely nothing with
        // the Idle/Run transition system.
        if (blockHeld)
            return;

        // Attacks control their own states.
        if (isAttacking)
            return;

        if (isTransitioning)
            return;

        bool shouldRun =
            playerMovement != null &&
            Mathf.Abs(playerMovement.horizontal) > 0.01f;

        // Idle → Run
        if (shouldRun && !isRunning)
        {
            currentMovementRoutine =
                StartCoroutine(TransitionToRun());
        }

        // Run → Idle
        else if (!shouldRun && isRunning)
        {
            currentMovementRoutine =
                StartCoroutine(TransitionToIdle());
        }
    }

    // =========================================================
    // BLOCK
    // =========================================================

    public void OnBlockPressed()
    {
        // Cannot block during attacks
        if (isAttacking)
            return;

        // Stop any active movement transition
        if (currentMovementRoutine != null)
        {
            StopCoroutine(currentMovementRoutine);
            currentMovementRoutine = null;
            isTransitioning = false;
        }

        blockHeld = true;
        isRunning = false;

        // Make sure movement/attack states are OFF
        if (run != null)
            run.SetActive(false);

        if (punch != null)
            punch.SetActive(false);

        if (kick != null)
            kick.SetActive(false);

        // Keep IDLE ON
        if (idle != null)
            idle.SetActive(true);

        // Turn BLOCK ON
        if (block != null)
            block.SetActive(true);
    }

    public void OnBlockReleased()
    {
        // If block wasn't successfully activated, do nothing
        if (!blockHeld)
            return;

        blockHeld = false;

        // Turn BLOCK OFF
        if (block != null)
            block.SetActive(false);

        // Return to neutral pose (Idle or Run depending on movement input)
        UpdateNeutralPose();
    }

    // =========================================================
    // PUNCH
    // =========================================================

    public void OnPunchPressed()
    {
        // Can't punch while blocking
        if (blockHeld)
            return;

        punchHeld = true;

        TriggerPunch();
    }

    public void OnPunchReleased()
    {
        punchHeld = false;
    }

    public void TriggerPunch()
    {
        // Can't attack while blocking
        if (blockHeld)
            return;

        if (isAttacking)
        {
            punchBuffered = true;
            kickBuffered = false;
            return;
        }

        currentAttackRoutine =
            StartCoroutine(PunchRoutine());
    }

    // =========================================================
    // PUNCH ROUTINE
    // =========================================================

    IEnumerator PunchRoutine()
    {
        if (blockHeld)
            yield break;

        isAttacking = true;
        punchBuffered = false;

        if (currentMovementRoutine != null)
        {
            StopCoroutine(currentMovementRoutine);
            currentMovementRoutine = null;

            isTransitioning = false;
        }

        if (playerMovement != null)
            playerMovement.isPunchPressed = true;

        if (transitionTime > 0f)
            yield return new WaitForSeconds(transitionTime);

        // Block cannot interrupt punch
        if (blockHeld)
        {
            if (playerMovement != null)
                playerMovement.isPunchPressed = false;

            isAttacking = false;
            currentAttackRoutine = null;

            yield break;
        }

        if (idle != null)
            idle.SetActive(false);

        if (run != null)
            run.SetActive(false);

        if (kick != null)
            kick.SetActive(false);

        if (block != null)
            block.SetActive(false);

        if (punch != null)
            punch.SetActive(true);

        float activeDuration =
            playerMovement != null &&
            playerMovement.punchTime > 0f
            ? playerMovement.punchTime
            : 0.25f;

        yield return new WaitForSeconds(activeDuration);

        if (punch != null)
            punch.SetActive(false);

        if (playerMovement != null)
            playerMovement.isPunchPressed = false;

        UpdateNeutralPose();

        float cooldown =
            playerMovement != null &&
            playerMovement.punchCooldown > 0f
            ? playerMovement.punchCooldown
            : punchCooldown;

        if (cooldown > 0f)
            yield return new WaitForSeconds(cooldown);

        isAttacking = false;
        currentAttackRoutine = null;

        if (punchBuffered)
        {
            punchBuffered = false;
            currentAttackRoutine =
                StartCoroutine(PunchRoutine());
        }
        else if (kickBuffered)
        {
            kickBuffered = false;
            currentAttackRoutine =
                StartCoroutine(KickRoutine());
        }
        else if (autoRepeatOnHold && punchHeld)
        {
            currentAttackRoutine =
                StartCoroutine(PunchRoutine());
        }
        else if (autoRepeatOnHold && kickHeld)
        {
            currentAttackRoutine =
                StartCoroutine(KickRoutine());
        }
    }

    // =========================================================
    // KICK
    // =========================================================

    public void OnKickPressed()
    {
        // Can't kick while blocking
        if (blockHeld)
            return;

        kickHeld = true;

        TriggerKick();
    }

    public void OnKickReleased()
    {
        kickHeld = false;
    }

    public void TriggerKick()
    {
        // Can't attack while blocking
        if (blockHeld)
            return;

        if (isAttacking)
        {
            kickBuffered = true;
            punchBuffered = false;
            return;
        }

        currentAttackRoutine =
            StartCoroutine(KickRoutine());
    }

    // =========================================================
    // KICK ROUTINE
    // =========================================================

    IEnumerator KickRoutine()
    {
        if (blockHeld)
            yield break;

        isAttacking = true;
        kickBuffered = false;

        if (currentMovementRoutine != null)
        {
            StopCoroutine(currentMovementRoutine);
            currentMovementRoutine = null;

            isTransitioning = false;
        }

        if (playerMovement != null)
            playerMovement.isKickPressed = true;

        if (transitionTime > 0f)
            yield return new WaitForSeconds(transitionTime);

        if (blockHeld)
        {
            if (playerMovement != null)
                playerMovement.isKickPressed = false;

            isAttacking = false;
            currentAttackRoutine = null;

            yield break;
        }

        if (idle != null)
            idle.SetActive(false);

        if (run != null)
            run.SetActive(false);

        if (punch != null)
            punch.SetActive(false);

        if (block != null)
            block.SetActive(false);

        if (kick != null)
            kick.SetActive(true);

        float activeDuration =
            playerMovement != null &&
            playerMovement.kickTime > 0f
            ? playerMovement.kickTime
            : 0.25f;

        yield return new WaitForSeconds(activeDuration);

        if (kick != null)
            kick.SetActive(false);

        if (playerMovement != null)
            playerMovement.isKickPressed = false;

        UpdateNeutralPose();

        float cooldown =
            playerMovement != null &&
            playerMovement.kickCooldown > 0f
            ? playerMovement.kickCooldown
            : kickCooldown;

        if (cooldown > 0f)
            yield return new WaitForSeconds(cooldown);

        isAttacking = false;
        currentAttackRoutine = null;

        if (kickBuffered)
        {
            kickBuffered = false;
            currentAttackRoutine =
                StartCoroutine(KickRoutine());
        }
        else if (punchBuffered)
        {
            punchBuffered = false;
            currentAttackRoutine =
                StartCoroutine(PunchRoutine());
        }
        else if (autoRepeatOnHold && kickHeld)
        {
            currentAttackRoutine =
                StartCoroutine(KickRoutine());
        }
        else if (autoRepeatOnHold && punchHeld)
        {
            currentAttackRoutine =
                StartCoroutine(PunchRoutine());
        }
    }

    // =========================================================
    // NEUTRAL POSE
    // =========================================================

    private void UpdateNeutralPose()
    {
        // If blocking, stay blocking in Idle pose
        if (blockHeld)
        {
            if (run != null)
                run.SetActive(false);

            if (punch != null)
                punch.SetActive(false);

            if (kick != null)
                kick.SetActive(false);

            if (idle != null)
                idle.SetActive(true);

            if (block != null)
                block.SetActive(true);

            return;
        }

        bool shouldRun =
            playerMovement != null &&
            Mathf.Abs(playerMovement.horizontal) > 0.01f;

        if (shouldRun)
            SetRun();
        else
            SetIdle();
    }

    // =========================================================
    // IDLE → RUN
    // =========================================================

    IEnumerator TransitionToRun()
    {
        if (blockHeld)
            yield break;

        isTransitioning = true;

        if (transitionTime > 0f)
            yield return new WaitForSeconds(transitionTime);

        if (blockHeld)
        {
            isTransitioning = false;
            currentMovementRoutine = null;
            yield break;
        }

        SetRun();

        isTransitioning = false;
        currentMovementRoutine = null;
    }

    // =========================================================
    // RUN → IDLE
    // =========================================================

    IEnumerator TransitionToIdle()
    {
        if (blockHeld)
            yield break;

        isTransitioning = true;

        if (transitionTime > 0f)
            yield return new WaitForSeconds(transitionTime);

        if (blockHeld)
        {
            isTransitioning = false;
            currentMovementRoutine = null;
            yield break;
        }

        SetIdle();

        isTransitioning = false;
        currentMovementRoutine = null;
    }

    // =========================================================
    // SET IDLE
    // =========================================================

    void SetIdle()
    {
        if (blockHeld)
            return;

        if (idle != null)
            idle.SetActive(true);

        if (run != null)
            run.SetActive(false);

        if (punch != null)
            punch.SetActive(false);

        if (kick != null)
            kick.SetActive(false);

        if (block != null)
            block.SetActive(false);

        isRunning = false;
    }

    // =========================================================
    // SET RUN
    // =========================================================

    void SetRun()
    {
        if (blockHeld)
            return;

        if (idle != null)
            idle.SetActive(false);

        if (run != null)
            run.SetActive(true);

        if (punch != null)
            punch.SetActive(false);

        if (kick != null)
            kick.SetActive(false);

        if (block != null)
            block.SetActive(false);

        isRunning = true;
    }

    // =========================================================
    // HIT GLOW
    // =========================================================

    public void PlayHitGlow()
    {
        if (hitGlow == null)
            EnsureHitGlow();

        if (hitGlow != null)
        {
            hitGlow.Play();
            return;
        }

        if (idle != null && idle.activeSelf)
        {
            var g = idle.GetComponent<SpriteGlowFade>();

            if (g != null)
                g.Play();
        }
        else if (run != null && run.activeSelf)
        {
            var g = run.GetComponent<SpriteGlowFade>();

            if (g != null)
                g.Play();
        }
        else if (punch != null && punch.activeSelf)
        {
            var g = punch.GetComponent<SpriteGlowFade>();

            if (g != null)
                g.Play();
        }
        else if (kick != null && kick.activeSelf)
        {
            var g = kick.GetComponent<SpriteGlowFade>();

            if (g != null)
                g.Play();
        }
        else if (block != null && block.activeSelf)
        {
            var g = block.GetComponent<SpriteGlowFade>();

            if (g != null)
                g.Play();
        }
    }

    // =========================================================
    // ENSURE HIT GLOW
    // =========================================================

    private void EnsureHitGlow()
    {
        hitGlow = GetComponent<SpriteGlowFade>();

        if (hitGlow == null)
            hitGlow = gameObject.AddComponent<SpriteGlowFade>();

        if (glowMaterial != null &&
            hitGlow.glowMaterial == null)
        {
            hitGlow.glowMaterial = glowMaterial;
        }

        hitGlow.glowColor = hitGlowColor;
        hitGlow.glowIntensity = hitGlowIntensity;
        hitGlow.fadeDuration = hitGlowDuration;
        hitGlow.enabled = true;
    }
}