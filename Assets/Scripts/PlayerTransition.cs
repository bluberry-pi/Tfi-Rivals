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

    private PlayerControls playerMovement;

    // Glow components
    private SpriteGlowFade idleGlow;
    private SpriteGlowFade runGlow;
    private SpriteGlowFade punchGlow;
    private SpriteGlowFade kickGlow;

    private bool isRunning = false;
    private bool isTransitioning = false;
    private bool wasRunningBeforePunch = false;
    private bool wasRunningBeforeKick = false;

    void Start()
    {
        playerMovement = GetComponent<PlayerControls>();

        idleGlow = idle != null ? idle.GetComponent<SpriteGlowFade>() : null;
        runGlow = run != null ? run.GetComponent<SpriteGlowFade>() : null;
        punchGlow = punch != null ? punch.GetComponent<SpriteGlowFade>() : null;
        kickGlow = kick != null ? kick.GetComponent<SpriteGlowFade>() : null;

        // Start in Idle
        SetIdle();
    }

    void Update()
    {
        // -----------------------------------------
        // PUNCH & KICK
        // -----------------------------------------

        if (playerMovement.isPunchPressed && !isTransitioning)
        {
            StartCoroutine(TransitionToPunch());
            return;
        }

        if (playerMovement.isKickPressed && !isTransitioning)
        {
            StartCoroutine(TransitionToKick());
            return;
        }

        // Don't change states while punching or kicking
        if ((punch != null && punch.activeSelf) || (kick != null && kick.activeSelf))
        {
            return;
        }

        if (isTransitioning)
            return;

        bool shouldRun = Mathf.Abs(playerMovement.horizontal) > 0.01f;

        // Idle → Run
        if (shouldRun && !isRunning)
        {
            StartCoroutine(TransitionToRun());
        }

        // Run → Idle
        else if (!shouldRun && isRunning)
        {
            StartCoroutine(TransitionToIdle());
        }
    }

    // --------------------------------------------------
    // IDLE → RUN
    // --------------------------------------------------

    IEnumerator TransitionToRun()
    {
        isTransitioning = true;

        yield return new WaitForSeconds(transitionTime);

        SetRun();

        isTransitioning = false;
    }

    // --------------------------------------------------
    // RUN → IDLE
    // --------------------------------------------------

    IEnumerator TransitionToIdle()
    {
        isTransitioning = true;

        yield return new WaitForSeconds(transitionTime);

        SetIdle();

        isTransitioning = false;
    }

    // --------------------------------------------------
    // ANY STATE → PUNCH
    // --------------------------------------------------

    IEnumerator TransitionToPunch()
    {
        isTransitioning = true;

        // Remember what state we were in BEFORE punching
        wasRunningBeforePunch = isRunning;

        // Show punch glow first
        if (punchGlow != null)
            punchGlow.Play();

        yield return new WaitForSeconds(transitionTime);

        idle.SetActive(false);
        run.SetActive(false);
        punch.SetActive(true);
        if (kick != null) kick.SetActive(false);

        isTransitioning = false;

        // Wait until PlayerControls says the punch is over
        while (playerMovement.isPunchPressed)
        {
            yield return null;
        }

        // Hide punch
        punch.SetActive(false);

        // Check CURRENT movement after punch finishes
        bool shouldRun = Mathf.Abs(playerMovement.horizontal) > 0.01f;

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
    // ANY STATE → KICK
    // --------------------------------------------------

    IEnumerator TransitionToKick()
    {
        isTransitioning = true;

        // Remember what state we were in BEFORE kicking
        wasRunningBeforeKick = isRunning;

        // Show kick glow first
        if (kickGlow != null)
            kickGlow.Play();

        yield return new WaitForSeconds(transitionTime);

        idle.SetActive(false);
        run.SetActive(false);
        punch.SetActive(false);
        if (kick != null) kick.SetActive(true);

        isTransitioning = false;

        // Wait until PlayerControls says the kick is over
        while (playerMovement.isKickPressed)
        {
            yield return null;
        }

        // Hide kick
        if (kick != null) kick.SetActive(false);

        // Check CURRENT movement after kick finishes
        bool shouldRun = Mathf.Abs(playerMovement.horizontal) > 0.01f;

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
    // SET IDLE
    // --------------------------------------------------

    void SetIdle()
    {
        idle.SetActive(true);
        run.SetActive(false);
        punch.SetActive(false);
        if (kick != null) kick.SetActive(false);

        isRunning = false;

        if (idleGlow != null)
            idleGlow.Play();
    }

    // --------------------------------------------------
    // SET RUN
    // --------------------------------------------------

    void SetRun()
    {
        idle.SetActive(false);
        run.SetActive(true);
        punch.SetActive(false);
        if (kick != null) kick.SetActive(false);

        isRunning = true;

        if (runGlow != null)
            runGlow.Play();
    }
}