using UnityEngine;

public class ControlsHint : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private float visibleSeconds = 12f;

    private float elapsed;

    public bool Visible => panel != null && panel.activeSelf;

    void Update()
    {
        if (panel == null || !panel.activeSelf) return;

        elapsed += Time.unscaledDeltaTime;
        if (elapsed >= visibleSeconds) Hide();
    }

    public void Show()
    {
        elapsed = 0f;
        if (panel != null) panel.SetActive(true);
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }
}
