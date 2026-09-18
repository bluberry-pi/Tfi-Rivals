using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public Image[] hearts;

    private int currentHearts;

    private void Start()
    {
        currentHearts = hearts.Length;
        UpdateHearts();
    }

    public void TakeHit()
    {
        // 1. Play hit glow on the character who received the hit
        PlayerTransition transition = GetComponent<PlayerTransition>();
        if (transition != null)
        {
            transition.PlayHitGlow();
        }
        else
        {
            SpriteGlowFade glow = GetComponent<SpriteGlowFade>();
            if (glow != null)
                glow.Play();
        }

        // 2. Process damage
        if (currentHearts <= 0)
            return;

        currentHearts--;

        Debug.Log(
            gameObject.name +
            " lost a heart! Hearts remaining: " +
            currentHearts
        );

        UpdateHearts();
    }

    private void UpdateHearts()
    {
        if (hearts == null) return;

        for (int i = 0; i < hearts.Length; i++)
        {
            if (hearts[i] != null)
                hearts[i].enabled = i < currentHearts;
        }
    }
}