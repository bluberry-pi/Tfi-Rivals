using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

// Attach this to the SAME GameObject as the SpriteRenderer you want to
// "reveal" with a white glowing silhouette (e.g. your Idle and Run objects).
//
// Requires a Material using the "Custom/URP/SpriteGlowUnlit" shader
// (SpriteGlowUnlit.shader), assigned to the Glow Material field below.
// That shader is what actually makes the silhouette solid white/glowing -
// the default sprite shader can't do this (tinting with white is a no-op).
//
// Optionally, assign your scene's Global Volume to Bloom Volume below.
// Instead of Bloom sitting on permanently, this script will push its
// intensity up to Bloom Intensity right as the glow appears, then ease it
// back down to 0 over the same fade window - so bloom is only actually
// visible during the transition burst, not all the time.
[RequireComponent(typeof(SpriteRenderer))]
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
    public int outlineDirections = 8;

    [Header("Timing")]
    [Tooltip("How long the glow takes to fade away and reveal the real sprite.")]
    public float fadeDuration = 0.25f;

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
    private Bloom bloom; // null if bloomVolume isn't assigned or has no Bloom override

    // --------------------------------------------------
    void Awake()
    {
        sourceRenderer = GetComponent<SpriteRenderer>();
        propBlock = new MaterialPropertyBlock();

        if (glowMaterial == null)
        {
            Debug.LogWarning($"{name}: SpriteGlowFade has no Glow Material assigned. " +
                              "Create a Material using Custom/URP/SpriteGlowUnlit and assign it.");
        }

        SetupBloom();
        BuildGlowRenderers();
        ApplyGlowProperties();
        SetGlowGroupActive(false);
    }

    void SetupBloom()
    {
        if (bloomVolume == null) return;

        if (bloomVolume.profile.TryGet(out bloom))
        {
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 0f; // start with no bloom contribution
        }
        else
        {
            Debug.LogWarning($"{name}: bloomVolume is assigned but its profile has no Bloom override.");
            bloom = null;
        }
    }

    // --------------------------------------------------
    // BUILD GLOW RENDERERS
    // --------------------------------------------------

    void BuildGlowRenderers()
    {
        // Solid fill, directly on top of the real sprite.
        fillRenderer = CreateCopy("GlowFill", Vector3.zero, sourceRenderer.sortingOrder + outlineDirections + 1);

        // Outline copies, offset in a ring around the sprite.
        outlineRenderers = new SpriteRenderer[outlineDirections];
        for (int i = 0; i < outlineDirections; i++)
        {
            float angle = (360f / outlineDirections) * i * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * outlineOffset;
            outlineRenderers[i] = CreateCopy("GlowOutline_" + i, offset, sourceRenderer.sortingOrder + i + 1);
        }
    }

    SpriteRenderer CreateCopy(string goName, Vector3 localOffset, int sortingOrder)
    {
        GameObject go = new GameObject(goName);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localOffset;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sourceRenderer.sprite;
        sr.sharedMaterial = glowMaterial; // the shader ignores sprite's own colors
        sr.sortingLayerID = sourceRenderer.sortingLayerID;
        sr.sortingOrder = sortingOrder;
        sr.flipX = sourceRenderer.flipX;
        sr.flipY = sourceRenderer.flipY;

        return sr;
    }

    // --------------------------------------------------
    // GLOW COLOR / INTENSITY (shared across all copies)
    // --------------------------------------------------

    void ApplyGlowProperties()
    {
        propBlock.SetColor("_Color", glowColor);
        propBlock.SetFloat("_GlowIntensity", glowIntensity);

        fillRenderer.SetPropertyBlock(propBlock);
        foreach (var r in outlineRenderers)
            r.SetPropertyBlock(propBlock);
    }

    // --------------------------------------------------
    // DISABLE CLEANUP
    // --------------------------------------------------

    // If you toggle this component off (e.g. to test without the glow),
    // this makes sure any in-progress fade doesn't get left stuck visible.
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

    // Call this right after the real sprite's GameObject becomes active.
    public void Play()
    {
        RefreshSprite();

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeGlow());
    }

    void RefreshSprite()
    {
        fillRenderer.sprite = sourceRenderer.sprite;
        fillRenderer.flipX = sourceRenderer.flipX;
        fillRenderer.flipY = sourceRenderer.flipY;

        foreach (var r in outlineRenderers)
        {
            r.sprite = sourceRenderer.sprite;
            r.flipX = sourceRenderer.flipX;
            r.flipY = sourceRenderer.flipY;
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
        // Alpha rides on SpriteRenderer.color.a, which the shader reads
        // as vertex color alpha (IN.color.a) - separate from the glow
        // tint/intensity, which live in the shared material property block.
        SetAlpha(fillRenderer, alpha);
        foreach (var r in outlineRenderers)
            SetAlpha(r, alpha);
    }

    void SetAlpha(SpriteRenderer r, float alpha)
    {
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
        fillRenderer.gameObject.SetActive(active);
        foreach (var r in outlineRenderers)
            r.gameObject.SetActive(active);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Keep glow color/intensity live-editable in the Inspector while
        // in Play Mode, if the renderers already exist.
        if (propBlock == null || fillRenderer == null) return;
        ApplyGlowProperties();
    }
#endif
}