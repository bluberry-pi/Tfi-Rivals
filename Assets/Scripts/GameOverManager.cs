using UnityEngine;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager instance;

    public GameObject gameOverCanvas;
    public GameObject mbWinText;
    public GameObject pkWinText;

    public PlayerHealth mbHealth;
    public PlayerHealth pkHealth;

    private void Awake()
    {
        instance = this;
    }

    private void Update()
    {
        // If MB loses all hearts -> PK wins
        if (mbHealth != null && mbHealth.CurrentHearts <= 0)
        {
            PKWins();
        }

        // If PK loses all hearts -> MB wins
        if (pkHealth != null && pkHealth.CurrentHearts <= 0)
        {
            MBWins();
        }
    }

    public void MBWins()
    {
        if (gameOverCanvas != null)
        {
            gameOverCanvas.SetActive(true);
        }

        if (mbWinText != null)
        {
            mbWinText.SetActive(true);
        }
    }

    public void PKWins()
    {
        if (gameOverCanvas != null)
        {
            gameOverCanvas.SetActive(true);
        }

        if (pkWinText != null)
        {
            pkWinText.SetActive(true);
        }
    }

    // Called automatically right when a player loses their last heart
    public void OnPlayerDefeated(string playerName)
    {
        if (playerName.ToLower().Contains("mahesh") || playerName.ToLower().Contains("mb"))
        {
            PKWins();
        }
        else if (playerName.ToLower().Contains("pawan") || playerName.ToLower().Contains("pk"))
        {
            MBWins();
        }
    }
}
