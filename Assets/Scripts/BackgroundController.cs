using UnityEngine;

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

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<SpriteRenderer>();

        cam = Camera.main;
    }

    private void Start()
    {
        ForceRefresh();
    }

    private void OnEnable()
    {
        ForceRefresh();
    }

    private void Update()
    {
        TilePaletteDatabase.ThemeFamily currentFamily = GetCurrentFamily();

        if (currentFamily != lastFamily)
        {
            ApplyTheme(currentFamily);
            FitToCamera();
            lastFamily = currentFamily;
        }

        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            FitToCamera();
        }
    }

    private TilePaletteDatabase.ThemeFamily GetCurrentFamily()
    {
        if (ThemeManager.I != null)
            return ThemeManager.I.GetCurrentPaletteFamily();

        return TilePaletteDatabase.ThemeFamily.Colorful;
    }

    private void ForceRefresh()
    {
        TilePaletteDatabase.ThemeFamily currentFamily = GetCurrentFamily();
        ApplyTheme(currentFamily);
        FitToCamera();
        lastFamily = currentFamily;
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

    private void FitToCamera()
    {
        if (targetRenderer == null || targetRenderer.sprite == null)
            return;

        if (cam == null)
            cam = Camera.main;

        if (cam == null || !cam.orthographic)
            return;

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

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