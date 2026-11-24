using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ARCameraToggle : MonoBehaviour
{
    private class Marker
    {
        public RectTransform rect;
        public float yaw;
        public float pitch;
        public float phase;
        public int kind;
        public GameObject badge;
    }

    private const int PhotoSize = 256;
    private static readonly Color Outline = new Color(0.97f, 0.96f, 0.92f);

    [SerializeField] private PlayerController player;
    [SerializeField] private FollowCamera followCamera;
    [SerializeField] private GameObject[] hideDuringAR;
    [SerializeField] private NamePrompt namePrompt;
    [SerializeField] private Sprite circleSprite;
    [SerializeField] private Sprite squareSprite;
    [SerializeField] private Sprite triangleSprite;
    [SerializeField] private Behaviour[] pauseDuringAR;
    [SerializeField] private ControlsHint arHint;
    [SerializeField] private ControlsHint worldHint;
    [SerializeField] private Text controlsBar;
    [SerializeField] private string worldBarText = "";
    [SerializeField] private string arBarText = "";
    [SerializeField] private float fieldOfView = 62f;
    [SerializeField] private float lookSensitivity = 0.2f;
    [SerializeField] private float markerSize = 170f;
    [SerializeField] private float captureRadius = 1.1f;

    private readonly List<Marker> markers = new List<Marker>();
    private AudioSource shutter;
    private AudioClip shutterClip;
    private Image flash;
    private float flashUntil;
    private bool capturing;
    private ScreenOrientation savedOrientation;
    private WebCamTexture webcam;
    private GameObject backdrop;
    private RawImage feed;
    private GameObject markerLayer;
    private RectTransform markerRoot;
    private float swipeYaw;
    private float swipePitch;
    private float sensorYaw;
    private float sensorPitch;
    private bool useAttitude;

    public static bool Active { get; private set; }

    public bool IsARActive { get; private set; }

    public bool CanSwipeLook => !useAttitude;

    public void ShowHelp()
    {
        if (IsARActive)
        {
            if (arHint != null) arHint.Show();
        }
        else if (worldHint != null) worldHint.Show();
    }

    public void Toggle()
    {
        ToggleARMode(!IsARActive);
    }

    public void ToggleARMode(bool activate)
    {
        if (activate == IsARActive) return;

        IsARActive = activate;
        Active = activate;

        foreach (var target in hideDuringAR)
            if (target != null) target.SetActive(!activate);

        foreach (var behaviour in pauseDuringAR)
            if (behaviour != null) behaviour.enabled = !activate;

        if (followCamera != null) followCamera.enabled = !activate;
        if (player != null) player.FlightEnabled = !activate;

        DangerAlert.Clear();
        if (controlsBar != null && !string.IsNullOrEmpty(arBarText)) controlsBar.text = activate ? arBarText : worldBarText;
        if (worldHint != null) worldHint.Hide();
        if (arHint != null)
        {
            if (activate) arHint.Show();
            else arHint.Hide();
        }

        if (activate)
        {
            BuildLayers();
            backdrop.SetActive(true);
            markerLayer.SetActive(true);
            SpawnMarkers();
            swipeYaw = 0f;
            swipePitch = 0f;
            StartCoroutine(StartPassthrough());
        }
        else
        {
            StopPassthrough();
            if (backdrop != null) backdrop.SetActive(false);
            if (markerLayer != null) markerLayer.SetActive(false);
        }
    }

    public void SimulateLook(Vector2 delta)
    {
        if (!IsARActive) return;

        swipeYaw -= delta.x * lookSensitivity;
        swipePitch = Mathf.Clamp(swipePitch - delta.y * lookSensitivity, -80f, 80f);
    }

    public bool TryCapture(Vector2 screenPosition, PlayerController target)
    {
        if (!IsARActive || capturing || NamePrompt.IsOpen) return false;
        if (arHint != null && arHint.Visible) return false;

        foreach (var marker in markers)
        {
            if (!Hit(marker, screenPosition)) continue;

            StartCoroutine(TakePhoto(marker));
            return true;
        }

        return false;
    }

    private bool Hit(Marker marker, Vector2 screenPosition)
    {
        if (!marker.rect.gameObject.activeSelf) return false;
        float reach = markerSize * captureRadius * marker.rect.localScale.x;
        return Vector2.Distance(screenPosition, marker.rect.position) <= reach;
    }

    private IEnumerator TakePhoto(Marker marker)
    {
        capturing = true;
        var kind = TreasureCatalog.All[marker.kind];
        Vector2 center = marker.rect.position;

        yield return new WaitForEndOfFrame();

        var shot = ScreenCapture.CaptureScreenshotAsTexture();
        int side = Mathf.RoundToInt(Mathf.Min(Mathf.Min(shot.width, shot.height), markerSize * 3.4f));
        int x = Mathf.Clamp(Mathf.RoundToInt(center.x - side * 0.5f), 0, shot.width - side);
        int y = Mathf.Clamp(Mathf.RoundToInt(center.y - side * 0.5f), 0, shot.height - side);

        var crop = new Texture2D(side, side, TextureFormat.RGB24, false);
        crop.SetPixels(shot.GetPixels(x, y, side, side));
        crop.Apply();
        Destroy(shot);

        var target = RenderTexture.GetTemporary(PhotoSize, PhotoSize, 0);
        Graphics.Blit(crop, target);
        var previous = RenderTexture.active;
        RenderTexture.active = target;
        var photo = new Texture2D(PhotoSize, PhotoSize, TextureFormat.RGB24, false);
        photo.ReadPixels(new Rect(0f, 0f, PhotoSize, PhotoSize), 0, 0);
        photo.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(target);
        Destroy(crop);

        PlayShutter();
        flashUntil = Time.unscaledTime + 0.25f;

        var persistence = DataPersistence.Instance;
        var existing = persistence != null ? persistence.FindAlbum(kind.id) : null;
        string suggestion = existing != null ? existing.title : kind.name;

        if (namePrompt == null)
        {
            SavePhoto(marker, kind, suggestion, photo);
            Destroy(photo);
            capturing = false;
            yield break;
        }

        yield return new WaitForSecondsRealtime(0.3f);

        namePrompt.Open(photo, suggestion, kind.name, title =>
        {
            if (title != null) SavePhoto(marker, kind, string.IsNullOrEmpty(title) ? kind.name : title, photo);
            Destroy(photo);
            capturing = false;
        });
    }

    private void SavePhoto(Marker marker, TreasureKind kind, string title, Texture2D photo)
    {
        if (DataPersistence.Instance == null) return;

        DataPersistence.Instance.SaveAlbumEntry(kind.id, title, photo.EncodeToJPG(88));
        if (marker.badge != null) marker.badge.SetActive(true);
    }

    private void PlayShutter()
    {
        if (shutter == null)
        {
            shutter = gameObject.AddComponent<AudioSource>();
            shutter.playOnAwake = false;
            shutter.spatialBlend = 0f;
            int samples = 5000;
            var data = new float[samples];
            var random = new System.Random(3);
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)samples;
                float noise = (float)(random.NextDouble() * 2.0 - 1.0);
                float click = i < 300 ? 1f : Mathf.Exp(-t * 16f) * 0.5f;
                data[i] = noise * click * 0.6f;
            }

            shutterClip = AudioClip.Create("Obturador", samples, 1, 22050, false);
            shutterClip.SetData(data, 0);
        }

        shutter.PlayOneShot(shutterClip, PauseMenu.SfxVolume);
    }

    void LateUpdate()
    {
        if (!IsARActive) return;

        UpdateAttitude();
        UpdateBackdrop();
        UpdateMarkers();
        UpdateFlash();
    }

    private void UpdateFlash()
    {
        if (flash == null) return;

        float remaining = flashUntil - Time.unscaledTime;
        flash.color = new Color(1f, 1f, 1f, remaining > 0f ? Mathf.Clamp01(remaining / 0.25f) * 0.85f : 0f);
    }

    private void BuildLayers()
    {
        if (backdrop != null) return;

        backdrop = new GameObject("ARBackdrop", typeof(Canvas));
        backdrop.transform.SetParent(transform, false);
        var backdropCanvas = backdrop.GetComponent<Canvas>();
        backdropCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        backdropCanvas.sortingOrder = -20;

        var dark = new GameObject("Dark", typeof(RectTransform), typeof(Image));
        dark.transform.SetParent(backdrop.transform, false);
        var darkRect = dark.GetComponent<RectTransform>();
        darkRect.anchorMin = Vector2.zero;
        darkRect.anchorMax = Vector2.one;
        darkRect.offsetMin = Vector2.zero;
        darkRect.offsetMax = Vector2.zero;
        var darkImage = dark.GetComponent<Image>();
        darkImage.color = new Color(0.05f, 0.06f, 0.09f);
        darkImage.raycastTarget = false;

        var feedObject = new GameObject("Feed", typeof(RectTransform), typeof(RawImage));
        feedObject.transform.SetParent(backdrop.transform, false);
        feed = feedObject.GetComponent<RawImage>();
        feed.raycastTarget = false;
        feed.enabled = false;

        markerLayer = new GameObject("ARMarkers", typeof(Canvas));
        markerLayer.transform.SetParent(transform, false);
        var markerCanvas = markerLayer.GetComponent<Canvas>();
        markerCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        markerCanvas.sortingOrder = -10;

        var rootObject = new GameObject("Root", typeof(RectTransform));
        rootObject.transform.SetParent(markerLayer.transform, false);
        markerRoot = rootObject.GetComponent<RectTransform>();
        markerRoot.anchorMin = markerRoot.anchorMax = markerRoot.pivot = new Vector2(0.5f, 0.5f);
        markerRoot.anchoredPosition = Vector2.zero;

        var flashCanvasObject = new GameObject("ARFlash", typeof(Canvas));
        flashCanvasObject.transform.SetParent(markerLayer.transform, false);
        var flashCanvas = flashCanvasObject.GetComponent<Canvas>();
        flashCanvas.overrideSorting = true;
        flashCanvas.sortingOrder = 60;
        var flashObject = new GameObject("Flash", typeof(RectTransform), typeof(Image));
        flashObject.transform.SetParent(flashCanvasObject.transform, false);
        var flashRect = flashObject.GetComponent<RectTransform>();
        flashRect.anchorMin = Vector2.zero;
        flashRect.anchorMax = Vector2.one;
        flashRect.offsetMin = Vector2.zero;
        flashRect.offsetMax = Vector2.zero;
        flash = flashObject.GetComponent<Image>();
        flash.color = new Color(1f, 1f, 1f, 0f);
        flash.raycastTarget = false;
    }

    private void SpawnMarkers()
    {
        foreach (var marker in markers)
            if (marker.rect != null) Destroy(marker.rect.gameObject);
        markers.Clear();

        int count = TreasureCatalog.All.Length;
        var order = new List<int>();
        for (int i = 0; i < count; i++) order.Add(i);
        for (int i = count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int swap = order[i];
            order[i] = order[j];
            order[j] = swap;
        }

        for (int i = 0; i < count; i++)
        {
            int kindIndex = order[i];
            var kind = TreasureCatalog.All[kindIndex];

            var root = new GameObject(kind.name, typeof(RectTransform));
            root.transform.SetParent(markerRoot, false);
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = Vector2.one * markerSize;

            var back = new GameObject("Outline", typeof(RectTransform)).GetComponent<RectTransform>();
            back.SetParent(root.transform, false);
            var front = new GameObject("Fill", typeof(RectTransform)).GetComponent<RectTransform>();
            front.SetParent(root.transform, false);
            BuildTreasure(back, front, kind);

            var badge = new GameObject("Fotografiado", typeof(RectTransform));
            badge.transform.SetParent(root.transform, false);
            var badgeBack = new GameObject("Outline", typeof(RectTransform)).GetComponent<RectTransform>();
            badgeBack.SetParent(badge.transform, false);
            var badgeFront = new GameObject("Fill", typeof(RectTransform)).GetComponent<RectTransform>();
            badgeFront.SetParent(badge.transform, false);
            Part(badgeBack, badgeFront, circleSprite, new Vector2(0.38f, 0.38f), Vector2.one * 0.3f, new Color(0.2f, 0.75f, 0.4f), 0f);
            Part(badgeBack, badgeFront, circleSprite, new Vector2(0.38f, 0.38f), Vector2.one * 0.11f, Color.white, 0f, false);
            bool done = DataPersistence.Instance != null && DataPersistence.Instance.FindAlbum(kind.id) != null;
            badge.SetActive(done);

            markers.Add(new Marker
            {
                rect = rect,
                kind = kindIndex,
                badge = badge,
                yaw = i * (360f / count) + Random.Range(-14f, 14f),
                pitch = Random.Range(-14f, 22f),
                phase = Random.value * Mathf.PI * 2f
            });
        }
    }

    private void BuildTreasure(RectTransform back, RectTransform front, TreasureKind kind)
    {
        Color m = kind.main;
        Color a = kind.accent;

        switch (kind.shape)
        {
            case TreasureShape.Gem:
                Part(back, front, squareSprite, Vector2.zero, Vector2.one * 0.62f, m, 45f);
                Part(back, front, squareSprite, new Vector2(-0.08f, 0.1f), Vector2.one * 0.22f, a, 45f, false);
                break;
            case TreasureShape.Coin:
                Part(back, front, circleSprite, Vector2.zero, Vector2.one * 0.8f, m, 0f);
                Part(back, front, circleSprite, Vector2.zero, Vector2.one * 0.56f, a, 0f, false);
                Part(back, front, circleSprite, Vector2.zero, Vector2.one * 0.34f, m, 0f, false);
                break;
            case TreasureShape.Pearl:
                Part(back, front, circleSprite, Vector2.zero, Vector2.one * 0.72f, m, 0f);
                Part(back, front, circleSprite, new Vector2(-0.14f, 0.14f), Vector2.one * 0.2f, a, 0f, false);
                break;
            case TreasureShape.Ruby:
                Part(back, front, triangleSprite, new Vector2(0f, 0.02f), Vector2.one * 0.78f, m, 0f);
                Part(back, front, triangleSprite, new Vector2(-0.05f, -0.02f), Vector2.one * 0.3f, a, 0f, false);
                break;
            case TreasureShape.Emerald:
                Part(back, front, squareSprite, Vector2.zero, new Vector2(0.58f, 0.74f), m, 0f);
                Part(back, front, squareSprite, Vector2.zero, new Vector2(0.34f, 0.5f), a, 0f, false);
                break;
            case TreasureShape.Key:
                Part(back, front, squareSprite, new Vector2(0.1f, -0.1f), new Vector2(0.62f, 0.12f), m, 45f);
                Part(back, front, squareSprite, new Vector2(0.3f, -0.3f), new Vector2(0.1f, 0.2f), m, 45f);
                Part(back, front, circleSprite, new Vector2(-0.2f, 0.2f), Vector2.one * 0.42f, m, 0f);
                Part(back, front, circleSprite, new Vector2(-0.2f, 0.2f), Vector2.one * 0.18f, a, 0f, false);
                break;
            case TreasureShape.Feather:
                Part(back, front, circleSprite, new Vector2(0.02f, 0.06f), new Vector2(0.36f, 0.8f), m, 25f);
                Part(back, front, squareSprite, new Vector2(-0.04f, -0.02f), new Vector2(0.05f, 0.9f), a, 25f, false);
                break;
            default:
                Part(back, front, triangleSprite, new Vector2(0f, 0.1f), Vector2.one * 0.66f, m, 0f);
                Part(back, front, triangleSprite, new Vector2(0f, -0.14f), new Vector2(0.5f, 0.34f), a, 180f);
                break;
        }
    }

    private void Part(RectTransform back, RectTransform front, Sprite sprite, Vector2 position, Vector2 size, Color color, float rotation, bool outline = true)
    {
        if (outline) AddImage(back, sprite, position, size + Vector2.one * 0.09f, Outline, rotation);
        AddImage(front, sprite, position, size, color, rotation);
    }

    private void AddImage(RectTransform parent, Sprite sprite, Vector2 position, Vector2 size, Color color, float rotation)
    {
        var go = new GameObject("Part", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = position * markerSize;
        rect.sizeDelta = size * markerSize;
        rect.localEulerAngles = new Vector3(0f, 0f, rotation);
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
    }

    private IEnumerator StartPassthrough()
    {
        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);

        if (!IsARActive) yield break;
        if (!Application.HasUserAuthorization(UserAuthorization.WebCam)) yield break;

        string deviceName = null;
        foreach (var device in WebCamTexture.devices)
        {
            if (device.isFrontFacing) continue;
            deviceName = device.name;
            break;
        }

        if (deviceName == null && WebCamTexture.devices.Length > 0)
            deviceName = WebCamTexture.devices[0].name;

        EnableAttitude();

        if (deviceName == null) yield break;

        savedOrientation = Screen.orientation;
        Screen.orientation = ScreenOrientation.Portrait;

        webcam = new WebCamTexture(deviceName, 1280, 720, 30);
        webcam.Play();
        feed.texture = webcam;
        feed.enabled = true;
    }

    private void StopPassthrough()
    {
        StopAllCoroutines();
        useAttitude = false;

        if (webcam != null)
        {
            webcam.Stop();
            Destroy(webcam);
            webcam = null;
            Screen.orientation = ScreenOrientation.AutoRotation;
        }

        if (feed != null) feed.enabled = false;
    }

    private void UpdateBackdrop()
    {
        if (webcam == null || feed == null || webcam.width < 100) return;

        int angle = webcam.videoRotationAngle;
        bool swap = angle == 90 || angle == 270;
        float width = swap ? webcam.height : webcam.width;
        float height = swap ? webcam.width : webcam.height;
        float scale = Mathf.Max(Screen.width / width, Screen.height / height);

        var rect = feed.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(webcam.width * scale, webcam.height * scale);
        rect.localEulerAngles = new Vector3(0f, 0f, -angle);
        feed.uvRect = webcam.videoVerticallyMirrored ? new Rect(0f, 1f, 1f, -1f) : new Rect(0f, 0f, 1f, 1f);
    }

    private void EnableAttitude()
    {
#if ENABLE_INPUT_SYSTEM
        var sensor = AttitudeSensor.current;
        if (sensor == null) return;

        InputSystem.EnableDevice(sensor);
        useAttitude = true;
#endif
    }

    private void UpdateAttitude()
    {
#if ENABLE_INPUT_SYSTEM
        if (!useAttitude) return;

        var sensor = AttitudeSensor.current;
        if (sensor == null) return;

        Quaternion q = sensor.attitude.ReadValue();
        Quaternion device = Quaternion.Euler(90f, 0f, 0f) * new Quaternion(-q.x, -q.y, q.z, q.w);
        Vector3 forward = device * Vector3.forward;
        sensorYaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        sensorPitch = Mathf.Asin(Mathf.Clamp(forward.y, -1f, 1f)) * Mathf.Rad2Deg;
#endif
    }

    private void UpdateMarkers()
    {
        float yaw = (useAttitude ? sensorYaw : 0f) + swipeYaw;
        float pitch = Mathf.Clamp((useAttitude ? sensorPitch : 0f) + swipePitch, -85f, 85f);
        float pixelsPerDegree = Screen.height / fieldOfView;
        float limitX = Screen.width * 0.5f + markerSize;
        float limitY = Screen.height * 0.5f + markerSize;

        foreach (var marker in markers)
        {
            float x = Mathf.DeltaAngle(yaw, marker.yaw) * pixelsPerDegree;
            float y = (marker.pitch - pitch) * pixelsPerDegree + Mathf.Sin(Time.time * 1.6f + marker.phase) * markerSize * 0.08f;

            marker.rect.gameObject.SetActive(Mathf.Abs(x) < limitX && Mathf.Abs(y) < limitY);
            marker.rect.anchoredPosition = new Vector2(x, y);
        }
    }

    void OnDisable()
    {
        Active = false;
        if (webcam != null) webcam.Stop();
    }
}
