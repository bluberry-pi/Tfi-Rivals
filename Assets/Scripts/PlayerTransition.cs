using UnityEngine;
using System.Collections;

public class PlayerTransition : MonoBehaviour
{
    [Header("Character States")]
    public GameObject idle;
    public GameObject run;

    [Header("Transition Timing")]
    public float transitionTime = 0.2f;

    private PlayerMovement playerMovement;

    private bool isRunning = false;
    private bool isTransitioning = false;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();

        // Start in Idle
        SetIdle();
    }

    void Update()
    {
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
    // SET IDLE
    // --------------------------------------------------

    void SetIdle()
    {
        idle.SetActive(true);
        run.SetActive(false);

        isRunning = false;
    }

    // --------------------------------------------------
    // SET RUN
    // --------------------------------------------------

    void SetRun()
    {
        idle.SetActive(false);
        run.SetActive(true);

        isRunning = true;
    }
}