using UnityEngine;
using System.Collections;

public class PlayerTransition : MonoBehaviour
{
    [Header("Character States")]
    public GameObject idle;
    public GameObject run;
    public GameObject punch;

    [Header("Transition Timing")]
    public float transitionTime = 0.2f;

    private PlayerControls playerMovement;

    // Glow components
    private SpriteGlowFade idleGlow;
    private SpriteGlowFade runGlow;
    private SpriteGlowFade punchGlow;

    private bool isRunning = false;
    private bool isTransitioning = false;
    private bool wasRunningBeforePunch = false;

    void Start()
    {
        playerMovement = GetComponent<PlayerControls>();

        idleGlow = idle.GetComponent<SpriteGlowFade>();
        runGlow = run.GetComponent<SpriteGlowFade>();
        punchGlow = punch.GetComponent<SpriteGlowFade>();

        // Start in Idle
        SetIdle();
    }

    void Update()
    {
        // -----------------------------------------
        // PUNCH
        // -----------------------------------------

        if (playerMovement.isPunchPressed && !isTransitioning)
        {
            StartCoroutine(TransitionToPunch());
            return;
        }

        // Don't change states while punching
        if (punch.activeSelf)
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
    // SET IDLE
    // --------------------------------------------------

    void SetIdle()
    {
        idle.SetActive(true);
        run.SetActive(false);
        punch.SetActive(false);

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

        isRunning = true;

        if (runGlow != null)
            runGlow.Play();
    }
}