using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AchievementTracker : MonoBehaviour
{
    [SerializeField] private float nightSeconds = 180f;
    [SerializeField] private Sprite panelSprite;

    private readonly Queue<AchievementDef> pending = new Queue<AchievementDef>();
    private GameObject banner;
    private Text title;
    private Text detail;
    private float shownUntil;
    private float nightTimer;

    void OnEnable()
    {
        Achievements.OnUnlocked += Enqueue;
    }

    void OnDisable()
    {
        Achievements.OnUnlocked -= Enqueue;
    }

    void Awake()
    {
        var canvasObject = new GameObject("AchievementToast", typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 55;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        banner = new GameObject("Banner", typeof(RectTransform), typeof(Image));
        banner.transform.SetParent(canvasObject.transform, false);
        var rect = banner.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(760f, 130f);
        rect.anchoredPosition = new Vector2(0f, -24f);
        var background = banner.GetComponent<Image>();
        background.raycastTarget = false;
        if (panelSprite != null)
        {
            background.sprite = panelSprite;
            background.type = Image.Type.Sliced;
        }
        else
        {
            background.color = new Color(0.1f, 0.1f, 0.2f, 0.92f);
        }

        title = MakeText("Title", banner.transform, 44, new Vector2(0f, 0.5f), new Color(1f, 0.85f, 0.35f));
        detail = MakeText("Detail", banner.transform, 32, new Vector2(0f, 0f), Color.white);
        banner.SetActive(false);
    }

    private static Text MakeText(string name, Transform parent, int size, Vector2 offset, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        bool top = offset.y > 0f;
        rect.anchorMin = new Vector2(0f, top ? 0.5f : 0.08f);
        rect.anchorMax = new Vector2(1f, top ? 0.95f : 0.5f);
        rect.offsetMin = new Vector2(20f, 0f);
        rect.offsetMax = new Vector2(-20f, 0f);
        var text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.raycastTarget = false;
        text.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
        return text;
    }

    private void Enqueue(AchievementDef def)
    {
        pending.Enqueue(def);
    }

    void Update()
    {
        TrackNight();

        if (banner.activeSelf && Time.unscaledTime >= shownUntil) banner.SetActive(false);

        if (!banner.activeSelf && pending.Count > 0)
        {
            var def = pending.Dequeue();
            title.text = "Logro: " + def.name;
            detail.text = def.description;
            shownUntil = Time.unscaledTime + 3.5f;
            banner.SetActive(true);
        }
    }

    private void TrackNight()
    {
        if (Time.timeScale <= 0f || ARCameraToggle.Active || DataPersistence.Instance == null) return;

        if (!DayCycle.IsNight)
        {
            nightTimer = 0f;
            return;
        }

        nightTimer += Time.deltaTime;
        if (nightTimer < nightSeconds) return;

        nightTimer = 0f;
        DataPersistence.Instance.AddNightSurvived();
    }
}
