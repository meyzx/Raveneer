using System;
using UnityEngine;
using UnityEngine.UI;

public class NamePrompt : MonoBehaviour
{
    [SerializeField] private Sprite panelSprite;
    [SerializeField] private Sprite buttonSprite;

    private static readonly Color Gold = new Color(1f, 0.85f, 0.35f);
    private static readonly Color Bone = new Color(0.97f, 0.96f, 0.92f);

    private GameObject root;
    private RawImage preview;
    private InputField input;
    private Text caption;
    private Action<string> callback;

    public static bool IsOpen { get; private set; }

    void Awake()
    {
        var canvasObject = new GameObject("NameCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 65;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        root = canvasObject;

        var dim = Child("Dim", canvasObject.transform);
        dim.anchorMin = Vector2.zero;
        dim.anchorMax = Vector2.one;
        dim.offsetMin = dim.offsetMax = Vector2.zero;
        dim.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);

        var panel = Child("Panel", canvasObject.transform);
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.62f);
        panel.sizeDelta = new Vector2(1300f, 620f);
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

        MakeText(panel, "Title", "¡Foto tomada!", 60, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(900f, 80f), Gold, TextAnchor.MiddleCenter);

        var frame = Child("Frame", panel);
        frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(0f, 0.5f);
        frame.sizeDelta = new Vector2(330f, 330f);
        frame.anchoredPosition = new Vector2(70f, -10f);
        frame.gameObject.AddComponent<Image>().color = Bone;
        var photo = Child("Photo", frame);
        photo.anchorMin = Vector2.zero;
        photo.anchorMax = Vector2.one;
        photo.offsetMin = new Vector2(8f, 8f);
        photo.offsetMax = new Vector2(-8f, -8f);
        preview = photo.gameObject.AddComponent<RawImage>();

        caption = MakeText(panel, "Caption", "Ponle nombre a tu tesoro", 36, new Vector2(1f, 0.5f), new Vector2(-70f, 90f), new Vector2(760f, 60f), Bone, TextAnchor.MiddleLeft);
        caption.rectTransform.pivot = new Vector2(1f, 0.5f);

        var field = Child("Field", panel);
        field.anchorMin = field.anchorMax = new Vector2(1f, 0.5f);
        field.pivot = new Vector2(1f, 0.5f);
        field.sizeDelta = new Vector2(760f, 84f);
        field.anchoredPosition = new Vector2(-70f, -2f);
        var fieldImage = field.gameObject.AddComponent<Image>();
        fieldImage.color = new Color(0.04f, 0.05f, 0.12f, 0.95f);
        var inputText = MakeText(field, "Text", "", 40, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Bone, TextAnchor.MiddleLeft);
        Inset(inputText.rectTransform, 20f);
        var placeholder = MakeText(field, "Placeholder", "Escribe un nombre", 40, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.5f, 0.52f, 0.65f), TextAnchor.MiddleLeft);
        Inset(placeholder.rectTransform, 20f);
        input = field.gameObject.AddComponent<InputField>();
        input.targetGraphic = fieldImage;
        input.textComponent = inputText;
        input.placeholder = placeholder;
        input.characterLimit = 24;
        input.lineType = InputField.LineType.SingleLine;

        var save = MakeButton(panel, "Guardar", new Vector2(1f, 0f), new Vector2(-70f, 70f), new Vector2(360f, 84f));
        save.onClick.AddListener(() => Finish(true));
        var discard = MakeButton(panel, "Descartar", new Vector2(1f, 0f), new Vector2(-450f, 70f), new Vector2(300f, 84f));
        discard.onClick.AddListener(() => Finish(false));

        root.SetActive(false);
    }

    public void Open(Texture2D photo, string suggestedName, string kindName, Action<string> onDone)
    {
        callback = onDone;
        preview.texture = photo;
        caption.text = "Ponle nombre a tu " + kindName.ToLowerInvariant();
        input.text = suggestedName;
        root.SetActive(true);
        IsOpen = true;
        StartCoroutine(SelectName());
    }

    private System.Collections.IEnumerator SelectName()
    {
        yield return null;
        input.ActivateInputField();
        input.selectionAnchorPosition = 0;
        input.selectionFocusPosition = input.text.Length;
    }

    private void Finish(bool save)
    {
        string text = input.text.Trim();
        root.SetActive(false);
        IsOpen = false;

        var action = callback;
        callback = null;
        if (action != null) action(save ? text : null);
    }

    void OnDisable()
    {
        IsOpen = false;
    }

    private static RectTransform Child(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static void Inset(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, 0f);
        rect.offsetMax = new Vector2(-inset, 0f);
    }

    private static Text MakeText(Transform parent, string name, string content, int size, Vector2 anchor, Vector2 position, Vector2 box, Color color, TextAnchor align)
    {
        var rect = Child(name, parent);
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = box;
        var text = rect.gameObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.fontStyle = FontStyle.Bold;
        text.alignment = align;
        text.color = color;
        text.text = content;
        text.raycastTarget = false;
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
        var text = MakeText(rect, "Label", label, 38, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Bone, TextAnchor.MiddleCenter);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
        return button;
    }
}
