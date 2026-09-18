using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

// Can be attached directly to a SpriteRenderer GameObject, or to a character's
// root GameObject where it dynamically targets the currently active sprite
// (e.g. Idle, Run, Punch, Kick) when Play() is called on hit.
public class SpriteGlowFade : MonoBehaviour
{
    [Header("Glow Material")]
    [Tooltip("A Material using the Custom/URP/SpriteGlowUnlit shader.")]
    public Material glowMaterial;

    [Header("Glow Look")]
    public Color glowColor = Color.white;

    [Tooltip("HDR boost. Values above 1 push the color into glow/bloom range.")]
    [Range(1f, 10f)]
    public float glowIntensity = 2f;

    [Tooltip("How far outward the outline copies sit, in world units (not pixels).")]
    public float outlineOffset = 0.05f;

    [Tooltip("How many outline copies ring the sprite. 8 = smooth, 4 = cheaper/chunkier.")]
    [Range(4, 16)]
    public int outlineDirections = 4;

    [Header("Timing")]
    [Tooltip("How long the glow takes to fade away and reveal the real sprite.")]
    public float fadeDuration = 0.15f;

    [Header("Bloom (optional)")]
    [Tooltip("Assign your scene's Global Volume here if you want Bloom to pulse in sync with this glow. Leave empty to skip bloom entirely.")]
    public Volume bloomVolume;

    [Tooltip("Bloom intensity at the peak of the glow (t = 0). Fades to 0 alongside the glow.")]
    public float bloomIntensity = 3f;

    private SpriteRenderer sourceRenderer;
    private SpriteRenderer fillRenderer;
    private SpriteRenderer[] outlineRenderers;
    private Coroutine fadeRoutine;
    private MaterialPropertyBlock propBlock;
    private Bloom bloom;

    // --------------------------------------------------
    void Awake()
    {
        sourceRenderer = GetComponent<SpriteRenderer>();
        propBlock = new MaterialPropertyBlock();

        EnsureMaterial();
        SetupBloom();
        BuildGlowRenderers();
        ApplyGlowProperties();
        SetGlowGroupActive(false);
    }

    void EnsureMaterial()
    {
        if (glowMaterial == null)
        {
            Shader glowShader = Shader.Find("Custom/URP/SpriteGlowUnlit");
            if (glowShader != null)
            {
                glowMaterial = new Material(glowShader);
                glowMaterial.name = "SpriteGlow_Runtime";
            }
            else
            {
                Debug.LogWarning($"{name}: SpriteGlowFade has no Glow Material assigned and Custom/URP/SpriteGlowUnlit shader not found.");
            }
        }
    }

    void SetupBloom()
    {
        if (bloomVolume == null)
        {
            bloomVolume = FindFirstObjectByType<Volume>();
        }

        if (bloomVolume == null) return;

        if (bloomVolume.profile != null && bloomVolume.profile.TryGet(out bloom))
        {
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 0f;
        }
        else
        {
            bloom = null;
        }
    }

    // --------------------------------------------------
    // BUILD GLOW RENDERERS
    // --------------------------------------------------

    void BuildGlowRenderers()
    {
        if (fillRenderer != null) return;

        EnsureMaterial();

        int baseOrder = sourceRenderer != null ? sourceRenderer.sortingOrder : 100;

        // Solid fill directly on top of the real sprite
        fillRenderer = CreateCopy("GlowFill", Vector3.zero, baseOrder + outlineDirections + 1);

        // Outline copies offset in a ring around the sprite
        outlineRenderers = new SpriteRenderer[outlineDirections];
        for (int i = 0; i < outlineDirections; i++)
        {
            float angle = (360f / outlineDirections) * i * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * outlineOffset;
            outlineRenderers[i] = CreateCopy("GlowOutline_" + i, offset, baseOrder + i + 1);
        }
    }

    SpriteRenderer CreateCopy(string goName, Vector3 localOffset, int sortingOrder)
    {
        GameObject go = new GameObject(goName);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localOffset;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        if (sourceRenderer != null)
        {
            sr.sprite = sourceRenderer.sprite;
            sr.sortingLayerID = sourceRenderer.sortingLayerID;
            sr.sortingOrder = sortingOrder;
            sr.flipX = sourceRenderer.flipX;
            sr.flipY = sourceRenderer.flipY;
        }
        else
        {
            sr.sortingOrder = sortingOrder;
        }

        if (glowMaterial != null)
        {
            sr.sharedMaterial = glowMaterial;
        }

        return sr;
    }

    bool IsOurGlowRenderer(SpriteRenderer r)
    {
        if (r == null) return false;
        if (r == fillRenderer) return true;
        if (outlineRenderers != null)
        {
            for (int i = 0; i < outlineRenderers.Length; i++)
            {
                if (outlineRenderers[i] == r) return true;
            }
        }
        return false;
    }

    public SpriteRenderer GetActiveSourceRenderer()
    {
        if (sourceRenderer != null)
            return sourceRenderer;

        // Search active children with a SpriteRenderer (excluding our own glow renderers)
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(false);
        foreach (var r in renderers)
        {
            if (!IsOurGlowRenderer(r))
                return r;
        }

        // Fallback: search inactive children if none currently active
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var r in renderers)
        {
            if (!IsOurGlowRenderer(r))
                return r;
        }

        return null;
    }

    // --------------------------------------------------
    // GLOW COLOR / INTENSITY
    // --------------------------------------------------

    public void ApplyGlowProperties()
    {
        if (propBlock == null)
            propBlock = new MaterialPropertyBlock();

        propBlock.SetColor("_Color", glowColor);
        propBlock.SetFloat("_GlowIntensity", glowIntensity);

        if (fillRenderer != null)
        {
            if (glowMaterial != null && fillRenderer.sharedMaterial != glowMaterial)
                fillRenderer.sharedMaterial = glowMaterial;
            fillRenderer.SetPropertyBlock(propBlock);
        }

        if (outlineRenderers != null)
        {
            foreach (var r in outlineRenderers)
            {
                if (r != null)
                {
                    if (glowMaterial != null && r.sharedMaterial != glowMaterial)
                        r.sharedMaterial = glowMaterial;
                    r.SetPropertyBlock(propBlock);
                }
            }
        }
    }

    // --------------------------------------------------
    // DISABLE CLEANUP
    // --------------------------------------------------

    void OnDisable()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        if (fillRenderer != null)
        {
            SetGlowGroupActive(false);
            SetBloom(0f);
        }
    }

    // --------------------------------------------------
    // PLAY
    // --------------------------------------------------

    // Call this when the character is hit to play the hit glow effect.
    public void Play()
    {
        if (!enabled)
            enabled = true;

        SpriteRenderer src = GetActiveSourceRenderer();
        if (src == null) return;

        ApplyGlowProperties();
        RefreshSprite(src);

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeGlow());
    }

    void RefreshSprite(SpriteRenderer src)
    {
        if (src == null) return;

        if (fillRenderer == null)
            BuildGlowRenderers();

        if (fillRenderer == null) return;

        bool isSelf = (src.gameObject == gameObject);

        fillRenderer.transform.localPosition = isSelf ? Vector3.zero : src.transform.localPosition;
        fillRenderer.transform.localRotation = isSelf ? Quaternion.identity : src.transform.localRotation;
        fillRenderer.transform.localScale = isSelf ? Vector3.one : src.transform.localScale;
        fillRenderer.sprite = src.sprite;
        fillRenderer.flipX = src.flipX;
        fillRenderer.flipY = src.flipY;
        fillRenderer.sortingLayerID = src.sortingLayerID;
        fillRenderer.sortingOrder = src.sortingOrder + outlineDirections + 1;

        if (outlineRenderers != null)
        {
            for (int i = 0; i < outlineRenderers.Length; i++)
            {
                var r = outlineRenderers[i];
                if (r == null) continue;

                float angle = (360f / outlineDirections) * i * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * outlineOffset;

                r.transform.localPosition = isSelf ? offset : (src.transform.localPosition + offset);
                r.transform.localRotation = isSelf ? Quaternion.identity : src.transform.localRotation;
                r.transform.localScale = isSelf ? Vector3.one : src.transform.localScale;
                r.sprite = src.sprite;
                r.flipX = src.flipX;
                r.flipY = src.flipY;
                r.sortingLayerID = src.sortingLayerID;
                r.sortingOrder = src.sortingOrder + i + 1;
            }
        }
    }

    IEnumerator FadeGlow()
    {
        SetGlowGroupActive(true);
        SetGlowAlpha(1f);
        SetBloom(1f);

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = fadeDuration <= 0f ? 0f : 1f - (t / fadeDuration);
            SetGlowAlpha(alpha);
            SetBloom(alpha);

            SpriteRenderer current = GetActiveSourceRenderer();
            if (current != null)
            {
                RefreshSprite(current);
            }

            yield return null;
        }

        SetGlowGroupActive(false);
        SetBloom(0f);
        fadeRoutine = null;
    }

    // --------------------------------------------------
    // HELPERS
    // --------------------------------------------------

    void SetGlowAlpha(float alpha)
    {
        SetAlpha(fillRenderer, alpha);
        if (outlineRenderers != null)
        {
            foreach (var r in outlineRenderers)
            {
                if (r != null)
                    SetAlpha(r, alpha);
            }
        }
    }

    void SetAlpha(SpriteRenderer r, float alpha)
    {
        if (r == null) return;
        Color c = r.color;
        c.a = alpha;
        r.color = c;
    }

    void SetBloom(float normalizedAlpha)
    {
        if (bloom == null) return;
        bloom.intensity.value = bloomIntensity * normalizedAlpha;
    }

    void SetGlowGroupActive(bool active)
    {
        if (fillRenderer != null)
            fillRenderer.gameObject.SetActive(active);

        if (outlineRenderers != null)
        {
            foreach (var r in outlineRenderers)
            {
                if (r != null)
                    r.gameObject.SetActive(active);
            }
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (propBlock == null || fillRenderer == null) return;
        ApplyGlowProperties();
    }
#endif
}