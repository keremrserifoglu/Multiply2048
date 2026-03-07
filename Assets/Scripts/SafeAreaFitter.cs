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
    private Rect lastScreenSafeArea;
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

        if (lastScreenSafeArea != Screen.safeArea ||
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
        Rect rawSafe = Screen.safeArea;
        Rect appliedSafe = rawSafe;

        appliedSafe.xMin += extraLeftInsetPx;
        appliedSafe.xMax -= extraRightInsetPx;
        appliedSafe.yMin += extraBottomInsetPx;
        appliedSafe.yMax -= extraTopInsetPx;

        appliedSafe.xMin = Mathf.Clamp(appliedSafe.xMin, 0f, Screen.width);
        appliedSafe.xMax = Mathf.Clamp(appliedSafe.xMax, 0f, Screen.width);
        appliedSafe.yMin = Mathf.Clamp(appliedSafe.yMin, 0f, Screen.height);
        appliedSafe.yMax = Mathf.Clamp(appliedSafe.yMax, 0f, Screen.height);

        lastScreenSafeArea = rawSafe;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        lastOrientation = Screen.orientation;

        Vector2 anchorMin = appliedSafe.position;
        Vector2 anchorMax = appliedSafe.position + appliedSafe.size;

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