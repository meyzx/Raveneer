using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private Text recordText;

    void Start()
    {
        Time.timeScale = 1f;
        float best = DataPersistence.Instance != null ? DataPersistence.Instance.Data.bestSurvivalTime : 0f;
        if (recordText != null) recordText.text = "Récord " + UIManager.FormatTime(best);
    }

    public void Play()
    {
        LoadingScreenController.LoadScene("GameScene");
    }

    public void Quit()
    {
        Application.Quit();
    }
}
