using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CollectionScreen : MonoBehaviour
{
    [SerializeField] private Sprite panelSprite;
    [SerializeField] private Sprite buttonSprite;

    private static readonly Color Gold = new Color(1f, 0.85f, 0.35f);
    private static readonly Color Bone = new Color(0.97f, 0.96f, 0.92f);

    private GameObject root;
    private RectTransform albumPage;
    private RectTransform achievementPage;
    private Text counter;
    private readonly List<Texture2D> photos = new List<Texture2D>();
    private float savedTimeScale = 1f;
    private bool open;

    public static bool IsOpen { get; private set; }

    void Awake()
    {
        var canvasObject = new GameObject("CollectionCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 70;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        root = canvasObject;

        var dim = Child("Dim", canvasObject.transform);
        Stretch(dim);
        dim.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);

        var panel = Child("Panel", canvasObject.transform);
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(1560f, 960f);
        var panelImage = panel.gameObject.AddComponent<Image>();
        if (panelSprite != null)
        {
            panelImage.sprite = panelSprite;
            panelImage.type = Image.Type.Sliced;
        }
        else
        {
            panelImage.color = new Color(0.1f, 0.1f, 0.2f, 0.97f);
        }

        var title = MakeText(panel, "Title", "Colección", 64, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(700f, 90f), Gold);
        counter = MakeText(panel, "Counter", "", 34, new Vector2(0.5f, 1f), new Vector2(0f, -135f), new Vector2(900f, 50f), Bone);

        var albumTab = MakeButton(panel, "Álbum", new Vector2(0.5f, 1f), new Vector2(-190f, -215f), new Vector2(340f, 74f));
        var achievementTab = MakeButton(panel, "Logros", new Vector2(0.5f, 1f), new Vector2(190f, -215f), new Vector2(340f, 74f));
        albumTab.onClick.AddListener(ShowAlbum);
        achievementTab.onClick.AddListener(ShowAchievements);

        albumPage = Child("AlbumPage", panel);
        albumPage.anchorMin = new Vector2(0f, 0f);
        albumPage.anchorMax = new Vector2(1f, 1f);
        albumPage.offsetMin = new Vector2(40f, 130f);
        albumPage.offsetMax = new Vector2(-40f, -290f);

        achievementPage = Child("AchievementPage", panel);
        achievementPage.anchorMin = new Vector2(0f, 0f);
        achievementPage.anchorMax = new Vector2(1f, 1f);
        achievementPage.offsetMin = new Vector2(60f, 130f);
        achievementPage.offsetMax = new Vector2(-60f, -300f);

        var close = MakeButton(panel, "Cerrar", new Vector2(0.5f, 0f), new Vector2(0f, 62f), new Vector2(380f, 80f));
        close.onClick.AddListener(Close);

        root.SetActive(false);
    }

    void OnDestroy()
    {
        if (open) Time.timeScale = savedTimeScale;
        IsOpen = false;
        ClearPhotos();
    }

    public void Open()
    {
        if (open) return;

        open = true;
        IsOpen = true;
        savedTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        root.SetActive(true);
        ShowAlbum();
    }

    public void OpenAchievements()
    {
        Open();
        ShowAchievements();
    }

    public void Close()
    {
        if (!open) return;

        open = false;
        IsOpen = false;
        Time.timeScale = savedTimeScale;
        root.SetActive(false);
        ClearPhotos();
    }

    public void ShowAlbum()
    {
        albumPage.gameObject.SetActive(true);
        achievementPage.gameObject.SetActive(false);
        BuildAlbum();
    }

    public void ShowAchievements()
    {
        albumPage.gameObject.SetActive(false);
        achievementPage.gameObject.SetActive(true);
        BuildAchievements();
    }

    private void ClearPhotos()
    {
        foreach (var photo in photos)
            if (photo != null) Destroy(photo);
        photos.Clear();
    }

    private static void Clear(RectTransform page)
    {
        for (int i = page.childCount - 1; i >= 0; i--)
            Destroy(page.GetChild(i).gameObject);
    }

    private void BuildAlbum()
    {
        Clear(albumPage);
        ClearPhotos();

        var persistence = DataPersistence.Instance;
        int total = TreasureCatalog.All.Length;
        int found = persistence != null ? persistence.Data.album.Count : 0;
        counter.text = "Objetos preciosos  " + found + "/" + total;

        const int columns = 4;
        const float cell = 330f;
        const float rowHeight = 250f;

        for (int i = 0; i < total; i++)
        {
            var kind = TreasureCatalog.All[i];
            var entry = persistence != null ? persistence.FindAlbum(kind.id) : null;

            int column = i % columns;
            int row = i / columns;
            var slot = Child("Slot" + i, albumPage);
            slot.anchorMin = slot.anchorMax = slot.pivot = new Vector2(0.5f, 1f);
            slot.sizeDelta = new Vector2(cell - 30f, rowHeight - 10f);
            slot.anchoredPosition = new Vector2((column - (columns - 1) * 0.5f) * cell, -row * rowHeight);

            var frame = Child("Frame", slot);
            frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(0.5f, 1f);
            frame.sizeDelta = new Vector2(190f, 190f);
            frame.anchoredPosition = Vector2.zero;
            var frameImage = frame.gameObject.AddComponent<Image>();
            frameImage.color = entry != null ? Bone : new Color(0.2f, 0.22f, 0.32f);

            var inner = Child("Photo", frame);
            Stretch(inner, 8f);

            if (entry != null)
            {
                var texture = persistence.LoadPhoto(kind.id);
                if (texture != null)
                {
                    photos.Add(texture);
                    var raw = inner.gameObject.AddComponent<RawImage>();
                    raw.texture = texture;
                }
            }
            else
            {
                inner.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.09f, 0.16f);
                MakeText(inner, "Unknown", "?", 90, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200f, 140f), new Color(0.45f, 0.48f, 0.62f));
            }

            string caption = entry != null ? entry.title : "Sin descubrir";
            var label = MakeText(slot, "Name", caption, 28, new Vector2(0.5f, 1f), new Vector2(0f, -212f), new Vector2(cell - 30f, 40f), entry != null ? Bone : new Color(0.55f, 0.58f, 0.7f));
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
        }
    }

    private void BuildAchievements()
    {
        Clear(achievementPage);

        int unlocked = 0;
        foreach (var def in Achievements.All)
            if (Achievements.IsUnlocked(def)) unlocked++;
        counter.text = "Logros  " + unlocked + "/" + Achievements.All.Length;

        const float rowHeight = 56f;
        for (int i = 0; i < Achievements.All.Length; i++)
        {
            var def = Achievements.All[i];
            bool done = Achievements.IsUnlocked(def);
            int value = Mathf.Min(Achievements.Value(def.metric), def.target);

            var row = Child("Row" + i, achievementPage);
            row.anchorMin = new Vector2(0f, 1f);
            row.anchorMax = new Vector2(1f, 1f);
            row.pivot = new Vector2(0.5f, 1f);
            row.sizeDelta = new Vector2(0f, rowHeight - 6f);
            row.anchoredPosition = new Vector2(0f, -i * rowHeight);
            row.gameObject.AddComponent<Image>().color = done ? new Color(0.35f, 0.28f, 0.08f, 0.85f) : new Color(0.12f, 0.13f, 0.22f, 0.85f);

            var mark = MakeText(row, "Mark", done ? "★" : "☆", 40, new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(70f, 50f), done ? Gold : new Color(0.5f, 0.52f, 0.65f));
            var name = MakeText(row, "Name", def.name, 32, new Vector2(0f, 0.5f), new Vector2(90f, 0f), new Vector2(380f, 50f), done ? Gold : Bone);
            name.alignment = TextAnchor.MiddleLeft;
            var description = MakeText(row, "Description", def.description, 28, new Vector2(0f, 0.5f), new Vector2(490f, 0f), new Vector2(760f, 50f), done ? Bone : new Color(0.7f, 0.72f, 0.82f));
            description.alignment = TextAnchor.MiddleLeft;
            var progress = MakeText(row, "Progress", value + "/" + def.target, 30, new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(160f, 50f), done ? Gold : Bone);
            progress.alignment = TextAnchor.MiddleRight;
        }
    }

    private static RectTransform Child(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static void Stretch(RectTransform rect, float inset = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    private static Text MakeText(Transform parent, string name, string content, int size, Vector2 anchor, Vector2 position, Vector2 box, Color color)
    {
        var rect = Child(name, parent);
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = box;
        var text = rect.gameObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.text = content;
        text.raycastTarget = false;
        rect.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.75f);
        return text;
    }

    private Button MakeButton(Transform parent, string label, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var rect = Child(label + "Button", parent);
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        var image = rect.gameObject.AddComponent<Image>();
        if (buttonSprite != null)
        {
            image.sprite = buttonSprite;
            image.type = Image.Type.Sliced;
        }

        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var text = MakeText(rect, "Label", label, 38, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Bone);
        Stretch(text.rectTransform);
        return button;
    }
}
