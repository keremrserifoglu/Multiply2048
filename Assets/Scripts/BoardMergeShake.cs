using System.Collections;
using UnityEngine;

public class BoardMergeShake : MonoBehaviour
{
    [Header("Shake Target")]
    [SerializeField] private Transform shakeTarget;

    [Header("Amplitude")]
    [SerializeField] private float minAmplitude = 0.05f;
    [SerializeField] private float maxAmplitude = 0.15f;
    [SerializeField] private int minValue = 8;
    [SerializeField] private int maxValue = 2048;

    [Header("Duration")]
    [SerializeField] private float minDuration = 0.1f;
    [SerializeField] private float maxDuration = 0.2f;

    [Header("Damping")]
    [SerializeField] private float frequency = 38f;
    [SerializeField] private float rotationalStrength = 0.9f;
    [SerializeField] private AnimationCurve falloff = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    private Coroutine shakeRoutine;
    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;

    private void Awake()
    {
        if (shakeTarget == null)
            shakeTarget = transform;

        baseLocalPosition = shakeTarget.localPosition;
        baseLocalRotation = shakeTarget.localRotation;
    }

    public void ShakeForValue(int mergedValue)
    {
        if (shakeTarget == null)
            return;

        float normalized = Mathf.InverseLerp(minValue, maxValue, Mathf.Max(minValue, mergedValue));
        normalized = Mathf.Pow(normalized, 0.85f);

        float amplitude = Mathf.Lerp(minAmplitude, maxAmplitude, normalized);
        float duration = Mathf.Lerp(minDuration, maxDuration, normalized);

        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        shakeRoutine = StartCoroutine(CoShake(amplitude, duration));
    }

    private IEnumerator CoShake(float amplitude, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float fade = falloff.Evaluate(t);

            float noiseX = (Mathf.PerlinNoise(13.1f, elapsed * frequency) - 0.5f) * 2f;
            float noiseY = (Mathf.PerlinNoise(29.7f, elapsed * frequency) - 0.5f) * 2f;
            float noiseR = (Mathf.PerlinNoise(47.3f, elapsed * frequency) - 0.5f) * 2f;

            Vector3 offset = new Vector3(noiseX, noiseY, 0f) * (amplitude * fade);
            float angle = noiseR * rotationalStrength * amplitude * 30f * fade;

            shakeTarget.localPosition = baseLocalPosition + offset;
            shakeTarget.localRotation = baseLocalRotation * Quaternion.Euler(0f, 0f, angle);

            yield return null;
        }

        shakeTarget.localPosition = baseLocalPosition;
        shakeTarget.localRotation = baseLocalRotation;
        shakeRoutine = null;
    }

    private void OnDisable()
    {
        if (shakeTarget == null)
            return;

        shakeTarget.localPosition = baseLocalPosition;
        shakeTarget.localRotation = baseLocalRotation;
        shakeRoutine = null;
    }
}
