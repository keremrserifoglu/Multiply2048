using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    [SerializeField] private bool applyOnStart = true;
    [SerializeField] private bool applyOnScreenChange = true;

    [Header("Runtime Ad Insets (pixels)")]
    [SerializeField] private float extraBottomInsetPx = 0f;
    [SerializeField] private float extraTopInsetPx = 0f;
    [SerializeField] private float extraLeftInsetPx = 0f;
    [SerializeField] private float extraRightInsetPx = 0f;

    private RectTransform rectTransform;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;
    private ScreenOrientation lastOrientation;

    public float ExtraBottomInsetPx => extraBottomInsetPx;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void Start()
    {
        if (applyOnStart)
        {
            ApplySafeArea();
        }
    }

    private void Update()
    {
        if (!applyOnScreenChange)
        {
            return;
        }

        if (lastSafeArea != Screen.safeArea ||
            lastScreenSize.x != Screen.width ||
            lastScreenSize.y != Screen.height ||
            lastOrientation != Screen.orientation)
        {
            ApplySafeArea();
        }
    }

    public void SetExtraBottomInsetPx(float value)
    {
        extraBottomInsetPx = Mathf.Max(0f, value);
        ApplySafeArea();
    }

    public void SetExtraInsetsPx(float left, float right, float top, float bottom)
    {
        extraLeftInsetPx = Mathf.Max(0f, left);
        extraRightInsetPx = Mathf.Max(0f, right);
        extraTopInsetPx = Mathf.Max(0f, top);
        extraBottomInsetPx = Mathf.Max(0f, bottom);
        ApplySafeArea();
    }

    public void ClearExtraInsets()
    {
        extraLeftInsetPx = 0f;
        extraRightInsetPx = 0f;
        extraTopInsetPx = 0f;
        extraBottomInsetPx = 0f;
        ApplySafeArea();
    }

    public void ApplySafeArea()
    {
        Rect safe = Screen.safeArea;

        safe.xMin += extraLeftInsetPx;
        safe.xMax -= extraRightInsetPx;
        safe.yMin += extraBottomInsetPx;
        safe.yMax -= extraTopInsetPx;

        safe.xMin = Mathf.Clamp(safe.xMin, 0f, Screen.width);
        safe.xMax = Mathf.Clamp(safe.xMax, 0f, Screen.width);
        safe.yMin = Mathf.Clamp(safe.yMin, 0f, Screen.height);
        safe.yMax = Mathf.Clamp(safe.yMax, 0f, Screen.height);

        lastSafeArea = safe;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        lastOrientation = Screen.orientation;

        Vector2 anchorMin = safe.position;
        Vector2 anchorMax = safe.position + safe.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}