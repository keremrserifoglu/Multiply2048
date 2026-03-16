using System.Collections;
using UnityEngine;

public class BackgroundController : MonoBehaviour
{
    [Header("Transition")]
    [Tooltip("Seconds to blend between palette background colors.")]
    [SerializeField] private float transitionDuration = 0.35f;

    private Camera cam;
    private Coroutine transitionCo;

    private void Awake()
    {
        cam = Camera.main;
        ApplyInstant();
    }

    private void OnEnable()
    {
        if (ThemeManager.I != null)
            ThemeManager.I.OnPaletteChanged += OnPaletteChanged;

        ApplyInstant();
    }

    private void OnDisable()
    {
        if (ThemeManager.I != null)
            ThemeManager.I.OnPaletteChanged -= OnPaletteChanged;
    }

    private void OnPaletteChanged()
    {
        if (ThemeManager.I == null)
            return;

        if (cam == null)
            cam = Camera.main;
        if (cam == null)
            return;

        if (transitionCo != null)
            StopCoroutine(transitionCo);

        transitionCo = StartCoroutine(TransitionRoutine(GetOpaqueBackgroundColor()));
    }

    private void ApplyInstant()
    {
        if (ThemeManager.I == null)
            return;

        if (cam == null)
            cam = Camera.main;
        if (cam == null)
            return;

        cam.backgroundColor = GetOpaqueBackgroundColor();
    }

    private Color GetOpaqueBackgroundColor()
    {
        Color color = ThemeManager.I.GetBackgroundColor();
        color.a = 1f;
        return color;
    }

    private IEnumerator TransitionRoutine(Color to)
    {
        Color from = cam.backgroundColor;
        from.a = 1f;
        to.a = 1f;

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
