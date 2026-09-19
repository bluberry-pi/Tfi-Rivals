using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Hearts (GameObjects)")]
    public GameObject[] hearts;

    [SerializeField] private int currentHearts;

    public int CurrentHearts => currentHearts;

    private void Start()
    {
        // Auto-detect hearts in the scene if not assigned in Inspector
        if (hearts == null || hearts.Length == 0)
        {
            AutoFindHearts();
        }

        currentHearts = CountActiveHearts();
    }

    private void AutoFindHearts()
    {
        string cleanName = gameObject.name.Replace("(Clone)", "").Trim();

        // 1. Try finding container GameObject matching "[Name]Hearts" or "[Name]Health"
        GameObject container = GameObject.Find(cleanName + "Hearts");
        if (container == null)
            container = GameObject.Find(cleanName + "Health");

        // 2. Fallback: search scene objects if named differently
        if (container == null)
        {
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (GameObject go in allObjects)
            {
                if (go.name.IndexOf(cleanName, System.StringComparison.OrdinalIgnoreCase) >= 0 &&
                    (go.name.IndexOf("Heart", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                     go.name.IndexOf("Health", System.StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    container = go;
                    break;
                }
            }
        }

        if (container != null)
        {
            int childCount = container.transform.childCount;
            hearts = new GameObject[childCount];
            for (int i = 0; i < childCount; i++)
            {
                hearts[i] = container.transform.GetChild(i).gameObject;
            }

            // Sort numbered children (e.g., "1", "2", "3", "4", "5") in ascending order
            System.Array.Sort(hearts, (a, b) =>
            {
                if (int.TryParse(a.name, out int numA) && int.TryParse(b.name, out int numB))
                    return numA.CompareTo(numB);
                return string.Compare(a.name, b.name, System.StringComparison.Ordinal);
            });
        }
    }

    private int CountActiveHearts()
    {
        if (hearts == null) return 0;
        int count = 0;
        for (int i = 0; i < hearts.Length; i++)
        {
            if (hearts[i] != null) count++;
        }
        return count;
    }

    public void TakeHit()
    {
        // If blocking, this GameObject takes no damage
        PlayerTransition transition = GetComponent<PlayerTransition>();
        if (transition != null && transition.IsBlocking)
        {
            Debug.Log(gameObject.name + " blocked the hit!");
            return;
        }

        PlayerControls controls = GetComponent<PlayerControls>();
        if (controls != null && controls.isBlockPressed)
        {
            Debug.Log(gameObject.name + " blocked the hit!");
            return;
        }

        // 1. Play hit glow on the character who received the hit
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

        // 3. Destroy / delete the last remaining heart GameObject in the array
        if (hearts != null)
        {
            for (int i = hearts.Length - 1; i >= 0; i--)
            {
                if (hearts[i] != null)
                {
                    GameObject heartToDestroy = hearts[i];
                    hearts[i] = null; // Clear reference
                    heartToDestroy.SetActive(false); // Immediately hide
                    Destroy(heartToDestroy); // Delete GameObject from scene
                    break;
                }
            }
        }

        Debug.Log(
            gameObject.name +
            " lost a heart! Hearts remaining: " +
            currentHearts
        );

        if (currentHearts <= 0 && GameOverManager.instance != null)
        {
            GameOverManager.instance.OnPlayerDefeated(gameObject.name);
        }
    }
}