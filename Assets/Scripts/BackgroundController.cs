using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(1000)]
[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private Sprite darkBackgroundSprite;
    [SerializeField] private Sprite colorfulBackgroundSprite;
    [SerializeField] private Sprite lightBackgroundSprite;
    [SerializeField] private float fitPadding = 1.08f;
    [SerializeField] private float targetZ = 1f;

    private Camera cam;
    private TilePaletteDatabase.ThemeFamily lastFamily;
    private int lastScreenWidth = -1;
    private int lastScreenHeight = -1;
    private float lastCameraSize = -1f;
    private float lastCameraAspect = -1f;
    private Vector3 lastCameraPosition = new Vector3(float.MinValue, float.MinValue, float.MinValue);

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<SpriteRenderer>();

        cam = Camera.main;
    }

    private void OnEnable()
    {
        if (ThemeManager.I != null)
            ThemeManager.I.OnPaletteChanged += HandlePaletteChanged;

        StartCoroutine(InitialRefreshRoutine());
    }

    private void OnDisable()
    {
        if (ThemeManager.I != null)
            ThemeManager.I.OnPaletteChanged -= HandlePaletteChanged;
    }

    private IEnumerator InitialRefreshRoutine()
    {
        ForceRefresh();
        yield return null;
        ForceRefresh();
        yield return null;
        ForceRefresh();
    }

    private void LateUpdate()
    {
        if (cam == null)
            cam = Camera.main;

        TilePaletteDatabase.ThemeFamily currentFamily = GetCurrentFamily();
        bool familyChanged = currentFamily != lastFamily;
        bool screenChanged = Screen.width != lastScreenWidth || Screen.height != lastScreenHeight;
        bool cameraChanged = HasCameraChanged();

        if (familyChanged)
        {
            ApplyTheme(currentFamily);
            lastFamily = currentFamily;
        }

        if (familyChanged || screenChanged || cameraChanged)
        {
            FitToCamera();
            CacheCameraState();
        }
    }

    private void HandlePaletteChanged()
    {
        ForceRefresh();
    }

    private TilePaletteDatabase.ThemeFamily GetCurrentFamily()
    {
        if (ThemeManager.I != null)
            return ThemeManager.I.GetCurrentPaletteFamily();

        return TilePaletteDatabase.ThemeFamily.Colorful;
    }

    private void ForceRefresh()
    {
        if (cam == null)
            cam = Camera.main;

        TilePaletteDatabase.ThemeFamily currentFamily = GetCurrentFamily();
        ApplyTheme(currentFamily);
        FitToCamera();
        lastFamily = currentFamily;
        CacheCameraState();
    }

    private void ApplyTheme(TilePaletteDatabase.ThemeFamily family)
    {
        if (targetRenderer == null)
            return;

        switch (family)
        {
            case TilePaletteDatabase.ThemeFamily.Dark:
                targetRenderer.sprite = darkBackgroundSprite != null ? darkBackgroundSprite : colorfulBackgroundSprite;
                break;

            case TilePaletteDatabase.ThemeFamily.Light:
                targetRenderer.sprite = lightBackgroundSprite != null ? lightBackgroundSprite : colorfulBackgroundSprite;
                break;

            default:
                targetRenderer.sprite = colorfulBackgroundSprite != null ? colorfulBackgroundSprite : darkBackgroundSprite;
                break;
        }

        targetRenderer.color = Color.white;
    }

    private bool HasCameraChanged()
    {
        if (cam == null || !cam.orthographic)
            return false;

        if (!Mathf.Approximately(cam.orthographicSize, lastCameraSize))
            return true;

        if (!Mathf.Approximately(cam.aspect, lastCameraAspect))
            return true;

        if ((cam.transform.position - lastCameraPosition).sqrMagnitude > 0.000001f)
            return true;

        return false;
    }

    private void CacheCameraState()
    {
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        if (cam == null)
            return;

        lastCameraSize = cam.orthographicSize;
        lastCameraAspect = cam.aspect;
        lastCameraPosition = cam.transform.position;
    }

    private void FitToCamera()
    {
        if (targetRenderer == null || targetRenderer.sprite == null)
            return;

        if (cam == null)
            cam = Camera.main;

        if (cam == null || !cam.orthographic)
            return;

        float worldHeight = cam.orthographicSize * 2f;
        float worldWidth = worldHeight * cam.aspect;

        Vector2 spriteSize = targetRenderer.sprite.bounds.size;
        if (spriteSize.x <= 0.0001f || spriteSize.y <= 0.0001f)
            return;

        float scale = Mathf.Max(worldWidth / spriteSize.x, worldHeight / spriteSize.y) * fitPadding;
        transform.localScale = new Vector3(scale, scale, 1f);

        Vector3 pos = transform.position;
        pos.x = cam.transform.position.x;
        pos.y = cam.transform.position.y;
        pos.z = targetZ;
        transform.position = pos;
    }
}