using System.Collections;
using UnityEngine;

public class CameraPaletteBackground : MonoBehaviour
{
    public float transitionDuration = 0.35f;

    private Camera cam;
    private Coroutine co;

    private void Awake()
    {
        cam = Camera.main;
    }

    private void OnEnable()
    {
        StartCoroutine(InitRoutine());
    }

    private IEnumerator InitRoutine()
    {
        while (ThemeManager.I == null)
            yield return null;

        ThemeManager.I.OnPaletteChanged += ApplySmooth;
        ApplyInstant();
    }

    private void OnDisable()
    {
        if (ThemeManager.I != null)
            ThemeManager.I.OnPaletteChanged -= ApplySmooth;
    }

    private void ApplyInstant()
    {
        if (!cam) return;

        Color c = ThemeManager.I.GetBackgroundColor();
        c.a = 1f;
        cam.backgroundColor = c;
    }

    private void ApplySmooth()
    {
        if (!cam) return;

        Color to = ThemeManager.I.GetBackgroundColor();
        to.a = 1f;

        if (co != null) StopCoroutine(co);
        co = StartCoroutine(Transition(to));
    }

    private IEnumerator Transition(Color to)
    {
        Color from = cam.backgroundColor;

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
