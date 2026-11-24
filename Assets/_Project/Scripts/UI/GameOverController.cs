using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverController : MonoBehaviour
{
    [SerializeField] private Text reasonText;
    [SerializeField] private Text timeText;
    [SerializeField] private Text recordText;

    void Start()
    {
        Time.timeScale = 1f;

        if (reasonText != null) reasonText.text = string.IsNullOrEmpty(GameManager.LastDeathReason) ? "Fin del vuelo" : GameManager.LastDeathReason;
        if (timeText != null) timeText.text = "Tiempo " + UIManager.FormatTime(GameManager.LastSurvivalTime);

        if (recordText == null) return;

        if (GameManager.LastWasRecord)
        {
            recordText.text = "Nuevo récord";
        }
        else
        {
            float best = DataPersistence.Instance != null ? DataPersistence.Instance.Data.bestSurvivalTime : 0f;
            recordText.text = "Récord " + UIManager.FormatTime(best);
        }
    }

    public void Retry()
    {
        LoadingScreenController.LoadScene("GameScene");
    }

    public void MainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
