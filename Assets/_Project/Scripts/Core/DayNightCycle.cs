using System;
using UnityEngine;
using UnityEngine.UI;

public static class DayCycle
{
    public static float ForcedHour = -1f;

    public static float Hour
    {
        get
        {
            if (ForcedHour >= 0f) return ForcedHour;
            DateTime now = DateTime.Now;
            return now.Hour + now.Minute / 60f + now.Second / 3600f;
        }
    }

    public static bool IsNight
    {
        get
        {
            float hour = Hour;
            return hour >= 19f || hour < 6f;
        }
    }

    public static float Darkness
    {
        get
        {
            float hour = Hour;
            if (hour >= 12f) return Mathf.InverseLerp(18f, 20f, hour);
            return 1f - Mathf.InverseLerp(5f, 7f, hour);
        }
    }
}

public class DayNightCycle : MonoBehaviour
{
    [SerializeField] private float forcedHour = -1f;
    [SerializeField] private float maxDarkness = 0.55f;
    [SerializeField] private Color nightTint = new Color(0.04f, 0.07f, 0.24f);

    private Image overlay;
    private float shown;

    void Awake()
    {
        DayCycle.ForcedHour = forcedHour;

        var canvasObject = new GameObject("NightOverlay", typeof(Canvas));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -5;

        var imageObject = new GameObject("Tint", typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(canvasObject.transform, false);
        var rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        overlay = imageObject.GetComponent<Image>();
        overlay.raycastTarget = false;
        overlay.color = new Color(nightTint.r, nightTint.g, nightTint.b, 0f);
        shown = DayCycle.Darkness;
    }

    void Update()
    {
        DayCycle.ForcedHour = forcedHour;
        shown = Mathf.MoveTowards(shown, DayCycle.Darkness, Time.unscaledDeltaTime * 0.5f);
        float alpha = ARCameraToggle.Active ? 0f : shown * maxDarkness;
        overlay.color = new Color(nightTint.r, nightTint.g, nightTint.b, alpha);
    }
}
