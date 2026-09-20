using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager instance;

    [Header("UI References")]
    public GameObject gameOverCanvas;
    public GameObject mbWinText;
    public GameObject pkWinText;
    public GameObject restartText;

    [Header("Player Health References")]
    public PlayerHealth mbHealth;
    public PlayerHealth pkHealth;
    [SerializeField] private AudioClip mbCLIP;
    [SerializeField] private AudioClip pkCLIP;


    private bool isGameOver = false;

    public bool IsGameOver => isGameOver;

    private void Awake()
    {
        instance = this;
        Time.timeScale = 1f;
    }

    private void Start()
    {
        Time.timeScale = 1f;

        if (mbHealth == null || pkHealth == null)
        {
            AutoFindPlayerHealth();
        }
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (isGameOver)
        {
            bool restartPressed = false;

            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                restartPressed = true;
            }

            #if ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                if (Input.GetKeyDown(KeyCode.R))
                {
                    restartPressed = true;
                }
            }
            catch {}
            #endif

            if (restartPressed)
            {
                RestartGame();
            }

            return;
        }

        // If MB loses all hearts -> PK wins
        if (mbHealth != null && mbHealth.CurrentHearts <= 0)
        {
            PKWins();
        }
        // If PK loses all hearts -> MB wins
        else if (pkHealth != null && pkHealth.CurrentHearts <= 0)
        {
            MBWins();
        }
    }

    public void MBWins()
    {
        if (isGameOver) return;
        if (mbCLIP != null && SoundFXManager.instance != null)
        {
            SoundFXManager.instance.PlaySoundFXClip(mbCLIP, transform, 1f);
        }
        TriggerGameOver(mbWinText);
    }

    public void PKWins()
    {
        if (isGameOver) return;
        if (pkCLIP != null && SoundFXManager.instance != null)
        {
            SoundFXManager.instance.PlaySoundFXClip(pkCLIP, transform, 0.25f);
        }
        TriggerGameOver(pkWinText);
    }

    private void TriggerGameOver(GameObject winTextToActivate)
    {
        if (isGameOver) return;
        isGameOver = true;

        if (gameOverCanvas != null)
        {
            gameOverCanvas.SetActive(true);
        }

        if (winTextToActivate != null)
        {
            winTextToActivate.SetActive(true);

            // If no dedicated restart text object is assigned, append a hint to the win text
            if (restartText == null)
            {
                TextMeshProUGUI tmp = winTextToActivate.GetComponent<TextMeshProUGUI>();
                if (tmp != null && !tmp.text.Contains("Restart"))
                {
                    tmp.text += "\n<size=35%><color=#FFE600>Press [R] to Restart</color></size>";
                }
            }
        }

        if (restartText != null)
        {
            restartText.SetActive(true);
        }

        // Freeze player movements and inputs
        DisablePlayerControls();

        // Pause the game
        Time.timeScale = 0f;
    }

    private void DisablePlayerControls()
    {
        // 1. Deactivate all PlayerInput components
        PlayerInput[] playerInputs = FindObjectsByType<PlayerInput>(FindObjectsSortMode.None);
        foreach (var pi in playerInputs)
        {
            if (pi != null)
            {
                pi.DeactivateInput();
            }
        }

        // 2. Disable all PlayerControls scripts
        PlayerControls[] playerControls = FindObjectsByType<PlayerControls>(FindObjectsSortMode.None);
        foreach (var pc in playerControls)
        {
            if (pc != null)
            {
                pc.enabled = false;
            }
        }

        // 3. Disable PlayerTransition scripts & stop ongoing coroutines
        PlayerTransition[] transitions = FindObjectsByType<PlayerTransition>(FindObjectsSortMode.None);
        foreach (var pt in transitions)
        {
            if (pt != null)
            {
                pt.StopAllCoroutines();
                pt.enabled = false;
            }
        }

        // 4. Halt physics velocities so players don't slide
        Rigidbody2D[] rbs = FindObjectsByType<Rigidbody2D>(FindObjectsSortMode.None);
        foreach (var rb in rbs)
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f; // Must unpause before loading scene
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.buildIndex);
    }

    // Called automatically right when a player loses their last heart
    public void OnPlayerDefeated(string playerName)
    {
        if (isGameOver) return;

        string lower = (playerName ?? "").ToLower();
        if (lower.Contains("mahesh") || lower.Contains("mb") || (mbHealth != null && playerName == mbHealth.gameObject.name))
        {
            PKWins();
        }
        else if (lower.Contains("pawan") || lower.Contains("pk") || (pkHealth != null && playerName == pkHealth.gameObject.name))
        {
            MBWins();
        }
    }

    private void AutoFindPlayerHealth()
    {
        PlayerHealth[] allHealths = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
        foreach (var ph in allHealths)
        {
            string n = ph.gameObject.name.ToLower();
            if (mbHealth == null && (n.Contains("mahesh") || n.Contains("mb")))
            {
                mbHealth = ph;
            }
            else if (pkHealth == null && (n.Contains("pawan") || n.Contains("pk")))
            {
                pkHealth = ph;
            }
        }
    }
}
