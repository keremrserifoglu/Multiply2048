using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public sealed class FloatingTextPopup : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TMP_Text popupText;

    [Header("Animation")]
    [SerializeField, Min(0.05f)] private float lifetime = 0.85f;
    [SerializeField, Min(0f)] private float travelPixels = 54f;
    [SerializeField, Min(0f)] private float startScaleMultiplier = 1.10f;
    [SerializeField, Min(0f)] private float endScaleMultiplier = 0.92f;
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private bool faceCamera = true;

    private Camera targetCamera;
    private Coroutine playCo;
    private Quaternion cameraFacingRotationOffset = Quaternion.identity;
    private Vector3 moveDirectionWorld = Vector3.up;

    private void Awake()
    {
        if (!popupText)
            popupText = GetComponent<TMP_Text>();
    }

    public void Play(
        string text,
        Camera cameraForPixels,
        Color color,
        int sortingOrder,
        Quaternion rotationOffset,
        Vector3 moveDirection)
    {
        if (!popupText)
            popupText = GetComponent<TMP_Text>();

        targetCamera = cameraForPixels != null ? cameraForPixels : Camera.main;
        cameraFacingRotationOffset = rotationOffset;
        moveDirectionWorld = moveDirection.sqrMagnitude > 0.0001f
            ? moveDirection.normalized
            : Vector3.up;

        popupText.text = text;
        popupText.alignment = TextAlignmentOptions.Center;
        popupText.textWrappingMode = TextWrappingModes.NoWrap;
        popupText.overflowMode = TextOverflowModes.Overflow;

        color.a = 1f;
        popupText.color = color;

        Renderer textRenderer = popupText.GetComponent<Renderer>();
        if (textRenderer != null)
            textRenderer.sortingOrder = sortingOrder;

        ApplyRotation();

        if (playCo != null)
            StopCoroutine(playCo);

        playCo = StartCoroutine(CoPlay(color));
    }

    private IEnumerator CoPlay(Color baseColor)
    {
        Vector3 startPosition = transform.position;
        Vector3 endPosition = startPosition + moveDirectionWorld * PixelsToWorldDistance(travelPixels);
        Vector3 baseScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
        Vector3 startScale = baseScale * startScaleMultiplier;
        Vector3 endScale = baseScale * endScaleMultiplier;

        float elapsed = 0f;

        while (elapsed < lifetime)
        {
            float delta = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            elapsed += delta;

            float t = Mathf.Clamp01(elapsed / lifetime);
            float moveT = 1f - Mathf.Pow(1f - t, 3f);
            float alphaT = t * t;

            transform.position = Vector3.LerpUnclamped(startPosition, endPosition, moveT);
            transform.localScale = Vector3.LerpUnclamped(startScale, endScale, t);

            Color c = baseColor;
            c.a = Mathf.Lerp(1f, 0f, alphaT);
            popupText.color = c;

            ApplyRotation();
            yield return null;
        }

        Destroy(gameObject);
    }

    private float PixelsToWorldDistance(float pixels)
    {
        if (targetCamera == null)
            return pixels * 0.01f;

        if (targetCamera.orthographic)
        {
            float screenHeight = Mathf.Max(1f, Screen.height);
            return pixels * ((targetCamera.orthographicSize * 2f) / screenHeight);
        }

        Vector3 screenPosition = targetCamera.WorldToScreenPoint(transform.position);
        if (screenPosition.z <= 0f)
            return pixels * 0.01f;

        Vector3 worldA = targetCamera.ScreenToWorldPoint(screenPosition);
        Vector3 worldB = targetCamera.ScreenToWorldPoint(screenPosition + new Vector3(0f, pixels, 0f));
        return Vector3.Distance(worldA, worldB);
    }

    private void ApplyRotation()
    {
        if (faceCamera && targetCamera != null)
        {
            transform.rotation = targetCamera.transform.rotation * cameraFacingRotationOffset;
            return;
        }

        transform.rotation = cameraFacingRotationOffset;
    }
}