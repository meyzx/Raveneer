using UnityEngine;
using UnityEngine.UI;

public class DangerAlert : MonoBehaviour
{
    private static float bannerUntil;
    private static float musicUntil;
    private static string message = "";

    public static bool MusicActive => Time.unscaledTime < musicUntil;

    public static void Raise(string enemyName, float warningSeconds, float musicExtraSeconds = 4f)
    {
        message = "¡PELIGRO!  " + enemyName + " se acerca";
        bannerUntil = Time.unscaledTime + warningSeconds;
        musicUntil = bannerUntil + musicExtraSeconds;
    }

    public static void Clear()
    {
        bannerUntil = 0f;
        musicUntil = 0f;
    }

    private GameObject banner;
    private Image background;
    private Text label;

    void Awake()
    {
        bannerUntil = 0f;
        musicUntil = 0f;

        var canvasObject = new GameObject("DangerAlert", typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        banner = new GameObject("Banner", typeof(RectTransform), typeof(Image));
        banner.transform.SetParent(canvasObject.transform, false);
        var rect = banner.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, 110f);
        rect.anchoredPosition = new Vector2(0f, -140f);
        background = banner.GetComponent<Image>();
        background.raycastTarget = false;

        var textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(banner.transform, false);
        var textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        label = textObject.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 56;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.raycastTarget = false;

        banner.SetActive(false);
    }

    void Update()
    {
        bool visible = Time.unscaledTime < bannerUntil;
        if (banner.activeSelf != visible) banner.SetActive(visible);
        if (!visible) return;

        float pulse = 0.65f + 0.3f * Mathf.Sin(Time.unscaledTime * 10f);
        background.color = new Color(0.75f, 0.08f, 0.1f, pulse);
        label.text = message;
    }
}
