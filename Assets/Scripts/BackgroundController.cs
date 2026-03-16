using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private Sprite darkBackgroundSprite;
    [SerializeField] private Sprite colorfulBackgroundSprite;
    [SerializeField] private Sprite lightBackgroundSprite;
    [SerializeField] private float fitPadding = 1.08f;
    [SerializeField] private float targetZ = 10f;

    private Camera cam;
    private int lastScreenW = -1;
    private int lastScreenH = -1;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<SpriteRenderer>();

        cam = Camera.main;
        ApplyTheme();
        FitToCamera();
    }

    private void OnEnable()
    {
        if (ThemeManager.I != null)
            ThemeManager.I.OnPaletteChanged += ApplyTheme;

        ApplyTheme();
        FitToCamera();
    }

    private void OnDisable()
    {
        if (ThemeManager.I != null)
            ThemeManager.I.OnPaletteChanged -= ApplyTheme;
    }

    private void LateUpdate()
    {
        if (Screen.width != lastScreenW || Screen.height != lastScreenH)
            FitToCamera();
    }

    private void ApplyTheme()
    {
        if (targetRenderer == null)
            return;

        TilePaletteDatabase.ThemeFamily family = ThemeManager.I != null
            ? ThemeManager.I.GetCurrentPaletteFamily()
            : TilePaletteDatabase.ThemeFamily.Colorful;

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

        FitToCamera();
    }

    private void FitToCamera()
    {
        if (targetRenderer == null || targetRenderer.sprite == null)
            return;

        if (cam == null)
            cam = Camera.main;

        if (cam == null || !cam.orthographic)
            return;

        lastScreenW = Screen.width;
        lastScreenH = Screen.height;

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