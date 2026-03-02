using UnityEngine;
using UnityEngine.UI;

public class UIBackgroundController : MonoBehaviour
{
    public Image mainMenuBackground;
    public Image hudBackground;
    public Image gameOverBackground;

    private void Start()
    {
        MakePanelsTransparent();
    }

    private void MakePanelsTransparent()
    {
        SetTransparent(mainMenuBackground);
        SetTransparent(hudBackground);
        SetTransparent(gameOverBackground);
    }

    private void SetTransparent(Image img)
    {
        if (!img) return;

        Color c = img.color;
        c.a = 0f;   // FULLY TRANSPARENT
        img.color = c;
    }
}
