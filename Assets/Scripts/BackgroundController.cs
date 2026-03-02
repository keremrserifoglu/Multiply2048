using System.Collections;
using UnityEngine;

public class BackgroundController : MonoBehaviour
{
    [Header("Transition")]
    [Tooltip("Seconds to blend between palette background colors.")]
    public float transitionDuration = 0.35f;

    [Tooltip("Set true if another script is overriding the camera background color.")]
    public bool enforceEveryFrame = false;

    private Camera cam;
    private Coroutine transitionCo;
    private Color targetColor;

    private void Awake()
    {
        cam = Camera.main;
        ApplyInstant(); // apply immediately on load
    }

    private void OnEnable()
    {
        if (ThemeManager.I != null)
            ThemeManager.I.OnPaletteChanged += OnPaletteChanged;

        // Apply once when enabled
        ApplyInstant();
    }

    private void OnDisable()
    {
        if (ThemeManager.I != null)
            ThemeManager.I.OnPaletteChanged -= OnPaletteChanged;
    }

    private void LateUpdate()
    {
        // Optional: If another script keeps overriding, force target at end of frame
        if (!enforceEveryFrame) return;
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        cam.backgroundColor = targetColor;
        Debug.Log("BG applied -> " + targetColor + " | cam=" + cam.name);

    }

    private void OnPaletteChanged()
    {
        if (ThemeManager.I == null) return;

        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Color next = ThemeManager.I.GetBackgroundColor();
        next.a = 1f;

        // Save target for enforce mode
        targetColor = next;

        // Smooth transition
        if (transitionCo != null) StopCoroutine(transitionCo);
        transitionCo = StartCoroutine(TransitionRoutine(next));
    }

    private void ApplyInstant()
    {
        if (ThemeManager.I == null) return;

        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Color c = ThemeManager.I.GetBackgroundColor();
        c.a = 1f;

        targetColor = c;
        cam.backgroundColor = c;
    }

    private IEnumerator TransitionRoutine(Color to)
    {
        Color from = cam.backgroundColor;
        from.a = 1f;
        to.a = 1f;

        // If duration is zero, snap
        if (transitionDuration <= 0.001f)
        {
            cam.backgroundColor = to;
            yield break;
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / transitionDuration;
            cam.backgroundColor = Color.Lerp(from, to, Mathf.Clamp01(t));
            yield return null;
        }

        cam.backgroundColor = to;
    }
}
