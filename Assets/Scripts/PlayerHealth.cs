using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Hearts")]
    public Image[] hearts;

    private int currentHearts;

    private void Start()
    {
        currentHearts = hearts.Length;

        UpdateHearts();
    }

    public void TakeHit()
    {
        if (currentHearts <= 0)
            return;

        currentHearts--;

        Debug.Log(
            gameObject.name +
            " lost a heart! Hearts remaining: " +
            currentHearts
        );

        UpdateHearts();

        if (currentHearts <= 0)
        {
            KO();
        }
    }

    private void UpdateHearts()
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            hearts[i].enabled = i < currentHearts;
        }
    }

    private void KO()
    {
        Debug.Log(gameObject.name + " has been KO'd!");
    }
}