using UnityEngine;
using System.Collections;

public class PlayerTransition : MonoBehaviour
{
    [Header("Character States")]
    public GameObject idle;
    public GameObject run;
    public GameObject punch;
    public GameObject kick;

    [Header("Transition Timing")]
    public float transitionTime = 0.2f;

    [Header("Attack Recovery / Cooldown")]
    [Tooltip("How long the character visibly returns to Idle/Run before another punch can come out when spamming")]
    public float punchCooldown = 0.12f;

    [Tooltip("How long the character visibly returns to Idle/Run before another kick can come out when spamming")]
    public float kickCooldown = 0.12f;

    [Tooltip("If true, holding down punch/kick will automatically chain attacks with visible retraction")]
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

    private Coroutine currentAttackRoutine = null;
    private Coroutine currentMovementRoutine = null;

    public bool IsAttacking => isAttacking;

    void Awake()
    {
        playerMovement = GetComponent<PlayerControls>();
    }

    void Start()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerControls>();

        EnsureHitGlow();

        // Start in Idle
        SetIdle();
    }

    void OnDisable()
    {
        // Reset state on disable to prevent stuck attacks
        StopAllCoroutines();
        currentAttackRoutine = null;
        currentMovementRoutine = null;
        isAttacking = false;
        isTransitioning = false;
        punchBuffered = false;
        kickBuffered = false;
        punchHeld = false;
        kickHeld = false;
        if (playerMovement != null)
        {
            playerMovement.isPunchPressed = false;
            playerMovement.isKickPressed = false;
        }
    }

    void Update()
    {
        // If attacking, the attack coroutine manages attack and neutral states
        if (isAttacking)
            return;

        if (isTransitioning)
            return;

        bool shouldRun = playerMovement != null && Mathf.Abs(playerMovement.horizontal) > 0.01f;

        // Idle → Run
        if (shouldRun && !isRunning)
        {
            currentMovementRoutine = StartCoroutine(TransitionToRun());
        }
        // Run → Idle
        else if (!shouldRun && isRunning)
        {
            currentMovementRoutine = StartCoroutine(TransitionToIdle());
        }
    }

    // --------------------------------------------------
    // ATTACK TRIGGERS
    // --------------------------------------------------

    public void OnPunchPressed()
    {
        punchHeld = true;
        TriggerPunch();
    }

    public void OnPunchReleased()
    {
        punchHeld = false;
    }

    public void OnKickPressed()
    {
        kickHeld = true;
        TriggerKick();
    }

    public void OnKickReleased()
    {
        kickHeld = false;
    }

    public void TriggerPunch()
    {
        if (isAttacking)
        {
            // Already attacking: buffer at most 1 punch so it executes after current attack finishes & recovers
            punchBuffered = true;
            kickBuffered = false;
            return;
        }

        currentAttackRoutine = StartCoroutine(PunchRoutine());
    }

    public void TriggerKick()
    {
        if (isAttacking)
        {
            // Already attacking: buffer at most 1 kick so it executes after current attack finishes & recovers
            kickBuffered = true;
            punchBuffered = false;
            return;
        }

        currentAttackRoutine = StartCoroutine(KickRoutine());
    }

    // --------------------------------------------------
    // PUNCH ROUTINE
    // --------------------------------------------------

    IEnumerator PunchRoutine()
    {
        isAttacking = true;
        punchBuffered = false;

        // Stop any active movement transition
        if (currentMovementRoutine != null)
        {
            StopCoroutine(currentMovementRoutine);
            currentMovementRoutine = null;
            isTransitioning = false;
        }

        if (playerMovement != null)
        {
            playerMovement.isPunchPressed = true;
        }

        // 1. Optional transition delay (windup)
        if (transitionTime > 0f)
        {
            yield return new WaitForSeconds(transitionTime);
        }

        // 2. Turn ON Punch Game Object!
        if (idle != null) idle.SetActive(false);
        if (run != null) run.SetActive(false);
        if (kick != null) kick.SetActive(false);
        if (punch != null) punch.SetActive(true);


        // 3. Keep punch active for punchTime
        float activeDuration = (playerMovement != null && playerMovement.punchTime > 0f)
            ? playerMovement.punchTime
            : 0.25f;

        yield return new WaitForSeconds(activeDuration);

        // 4. Turn OFF Punch Game Object! GUARANTEED!
        if (punch != null) punch.SetActive(false);

        if (playerMovement != null)
        {
            playerMovement.isPunchPressed = false;
        }

        // 5. Visibly return to neutral pose (Idle or Run)
        UpdateNeutralPose();

        // 6. Recovery delay: character visibly stays in Idle/Run
        // so the player clearly sees the punch retract before the next punch
        float cooldown = (playerMovement != null && playerMovement.punchCooldown > 0f)
            ? playerMovement.punchCooldown
            : punchCooldown;

        if (cooldown > 0f)
        {
            yield return new WaitForSeconds(cooldown);
        }

        // 7. Attack cycle finished
        isAttacking = false;
        currentAttackRoutine = null;

        // 8. If spammed, execute the single buffered attack
        if (punchBuffered)
        {
            punchBuffered = false;
            currentAttackRoutine = StartCoroutine(PunchRoutine());
        }
        else if (kickBuffered)
        {
            kickBuffered = false;
            currentAttackRoutine = StartCoroutine(KickRoutine());
        }
        else if (autoRepeatOnHold && punchHeld)
        {
            currentAttackRoutine = StartCoroutine(PunchRoutine());
        }
        else if (autoRepeatOnHold && kickHeld)
        {
            currentAttackRoutine = StartCoroutine(KickRoutine());
        }
    }

    // --------------------------------------------------
    // KICK ROUTINE
    // --------------------------------------------------

    IEnumerator KickRoutine()
    {
        isAttacking = true;
        kickBuffered = false;

        // Stop any active movement transition
        if (currentMovementRoutine != null)
        {
            StopCoroutine(currentMovementRoutine);
            currentMovementRoutine = null;
            isTransitioning = false;
        }

        if (playerMovement != null)
        {
            playerMovement.isKickPressed = true;
        }

        // 1. Optional transition delay (windup)
        if (transitionTime > 0f)
        {
            yield return new WaitForSeconds(transitionTime);
        }

        // 2. Turn ON Kick Game Object!
        if (idle != null) idle.SetActive(false);
        if (run != null) run.SetActive(false);
        if (punch != null) punch.SetActive(false);
        if (kick != null) kick.SetActive(true);


        // 3. Keep kick active for kickTime
        float activeDuration = (playerMovement != null && playerMovement.kickTime > 0f)
            ? playerMovement.kickTime
            : 0.25f;

        yield return new WaitForSeconds(activeDuration);

        // 4. Turn OFF Kick Game Object! GUARANTEED!
        if (kick != null) kick.SetActive(false);

        if (playerMovement != null)
        {
            playerMovement.isKickPressed = false;
        }

        // 5. Visibly return to neutral pose (Idle or Run)
        UpdateNeutralPose();

        // 6. Recovery delay: character visibly stays in Idle/Run
        // so the player clearly sees the kick retract before the next kick
        float cooldown = (playerMovement != null && playerMovement.kickCooldown > 0f)
            ? playerMovement.kickCooldown
            : kickCooldown;

        if (cooldown > 0f)
        {
            yield return new WaitForSeconds(cooldown);
        }

        // 7. Attack cycle finished
        isAttacking = false;
        currentAttackRoutine = null;

        // 8. If spammed, execute the single buffered attack
        if (kickBuffered)
        {
            kickBuffered = false;
            currentAttackRoutine = StartCoroutine(KickRoutine());
        }
        else if (punchBuffered)
        {
            punchBuffered = false;
            currentAttackRoutine = StartCoroutine(PunchRoutine());
        }
        else if (autoRepeatOnHold && kickHeld)
        {
            currentAttackRoutine = StartCoroutine(KickRoutine());
        }
        else if (autoRepeatOnHold && punchHeld)
        {
            currentAttackRoutine = StartCoroutine(PunchRoutine());
        }
    }

    // --------------------------------------------------
    // UPDATE NEUTRAL POSE
    // --------------------------------------------------

    private void UpdateNeutralPose()
    {
        bool shouldRun = playerMovement != null && Mathf.Abs(playerMovement.horizontal) > 0.01f;
        if (shouldRun)
        {
            SetRun();
        }
        else
        {
            SetIdle();
        }
    }

    // --------------------------------------------------
    // IDLE → RUN
    // --------------------------------------------------

    IEnumerator TransitionToRun()
    {
        isTransitioning = true;

        if (transitionTime > 0f)
            yield return new WaitForSeconds(transitionTime);

        SetRun();

        isTransitioning = false;
        currentMovementRoutine = null;
    }

    // --------------------------------------------------
    // RUN → IDLE
    // --------------------------------------------------

    IEnumerator TransitionToIdle()
    {
        isTransitioning = true;

        if (transitionTime > 0f)
            yield return new WaitForSeconds(transitionTime);

        SetIdle();

        isTransitioning = false;
        currentMovementRoutine = null;
    }

    // --------------------------------------------------
    // SET IDLE
    // --------------------------------------------------

    void SetIdle()
    {
        if (idle != null) idle.SetActive(true);
        if (run != null) run.SetActive(false);
        if (punch != null) punch.SetActive(false);
        if (kick != null) kick.SetActive(false);

        isRunning = false;

    }

    // --------------------------------------------------
    // SET RUN
    // --------------------------------------------------

    void SetRun()
    {
        if (idle != null) idle.SetActive(false);
        if (run != null) run.SetActive(true);
        if (punch != null) punch.SetActive(false);
        if (kick != null) kick.SetActive(false);

        isRunning = true;

    }

    // --------------------------------------------------
    // HIT GLOW
    // --------------------------------------------------

    public void PlayHitGlow()
    {
        if (hitGlow == null)
            EnsureHitGlow();

        if (hitGlow != null)
        {
            hitGlow.Play();
            return;
        }

        // Fallback to active child state's SpriteGlowFade if present
        if (idle != null && idle.activeSelf)
        {
            var g = idle.GetComponent<SpriteGlowFade>();
            if (g != null) g.Play();
        }
        else if (run != null && run.activeSelf)
        {
            var g = run.GetComponent<SpriteGlowFade>();
            if (g != null) g.Play();
        }
        else if (punch != null && punch.activeSelf)
        {
            var g = punch.GetComponent<SpriteGlowFade>();
            if (g != null) g.Play();
        }
        else if (kick != null && kick.activeSelf)
        {
            var g = kick.GetComponent<SpriteGlowFade>();
            if (g != null) g.Play();
        }
    }

    private void EnsureHitGlow()
    {
        hitGlow = GetComponent<SpriteGlowFade>();
        if (hitGlow == null)
        {
            hitGlow = gameObject.AddComponent<SpriteGlowFade>();
        }

        if (glowMaterial != null && hitGlow.glowMaterial == null)
        {
            hitGlow.glowMaterial = glowMaterial;
        }

        hitGlow.glowColor = hitGlowColor;
        hitGlow.glowIntensity = hitGlowIntensity;
        hitGlow.fadeDuration = hitGlowDuration;
        hitGlow.enabled = true;
    }
}