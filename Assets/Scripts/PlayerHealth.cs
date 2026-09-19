using UnityEngine;
using System.Collections.Generic;

public class PlayerHealth : MonoBehaviour
{
    [Header("Hearts (GameObjects)")]
    public GameObject[] hearts;

    [Header("Health & Hit Settings")]
    [Tooltip("Number of hits required to lose one heart (default: 3, editable in Inspector)")]
    [Min(1)]
    public int hitsPerHeart = 3;

    [Header("Live Status (Debug View)")]
    [SerializeField] private int currentHearts;
    [SerializeField] private int currentHitsOnHeart = 0;

    private Dictionary<GameObject, float> originalHeartAlphas = new Dictionary<GameObject, float>();

    public int CurrentHearts => currentHearts;
    public int HitsPerHeart => hitsPerHeart;
    public int CurrentHitsOnHeart => currentHitsOnHeart;

    private void Start()
    {
        // Auto-detect hearts in the scene if not assigned in Inspector
        if (hearts == null || hearts.Length == 0)
        {
            AutoFindHearts();
        }

        currentHearts = CountActiveHearts();
        currentHitsOnHeart = 0;

        CacheHeartAlphas();
        ResetHeartsOpacity();
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

    private void CacheHeartAlphas()
    {
        originalHeartAlphas.Clear();
        if (hearts == null) return;

        foreach (var h in hearts)
        {
            if (h == null) continue;
            SpriteRenderer sr = h.GetComponentInChildren<SpriteRenderer>(true);
            if (sr != null)
            {
                originalHeartAlphas[h] = sr.color.a > 0 ? sr.color.a : 1f;
            }
            else
            {
                originalHeartAlphas[h] = 1f;
            }
        }
    }

    public void ResetHeartsOpacity()
    {
        if (hearts == null) return;
        foreach (var h in hearts)
        {
            if (h != null)
            {
                SetHeartOpacity(h, 1f);
            }
        }
    }

    private void SetHeartOpacity(GameObject heartObj, float normalizedFraction)
    {
        if (heartObj == null) return;

        float baseAlpha = 1f;
        if (originalHeartAlphas != null && originalHeartAlphas.TryGetValue(heartObj, out float cachedAlpha))
        {
            baseAlpha = cachedAlpha;
        }

        float targetAlpha = Mathf.Clamp01(normalizedFraction) * baseAlpha;

        // 1. SpriteRenderer(s)
        SpriteRenderer[] srs = heartObj.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in srs)
        {
            if (sr != null)
            {
                Color c = sr.color;
                c.a = targetAlpha;
                sr.color = c;
            }
        }

        // 2. UI Image(s)
        UnityEngine.UI.Image[] imgs = heartObj.GetComponentsInChildren<UnityEngine.UI.Image>(true);
        foreach (var img in imgs)
        {
            if (img != null)
            {
                Color c = img.color;
                c.a = targetAlpha;
                img.color = c;
            }
        }

        // 3. CanvasGroup
        CanvasGroup cg = heartObj.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = targetAlpha;
        }
    }

    public GameObject GetCurrentActiveHeart()
    {
        if (hearts == null) return null;
        for (int i = hearts.Length - 1; i >= 0; i--)
        {
            if (hearts[i] != null && hearts[i].activeInHierarchy)
            {
                return hearts[i];
            }
        }
        return null;
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

        int requiredHits = Mathf.Max(1, hitsPerHeart);
        currentHitsOnHeart++;

        GameObject activeHeart = GetCurrentActiveHeart();

        // Calculate remaining opacity fraction for this heart
        float remainingFraction = (float)(requiredHits - currentHitsOnHeart) / requiredHits;

        if (activeHeart != null)
        {
            SetHeartOpacity(activeHeart, remainingFraction);
        }

        Debug.Log(
            $"{gameObject.name} took hit {currentHitsOnHeart}/{requiredHits} on current heart. Heart Opacity: {remainingFraction:P0}"
        );

        // Only lose a heart after required number of hits
        if (currentHitsOnHeart >= requiredHits)
        {
            currentHitsOnHeart = 0;
            LoseHeart();
        }
    }

    public void LoseHeart()
    {
        if (currentHearts <= 0)
            return;

        currentHearts--;

        // Destroy / delete the last remaining heart GameObject in the array
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