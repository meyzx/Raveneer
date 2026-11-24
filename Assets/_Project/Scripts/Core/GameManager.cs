using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenuCanvas;
    [SerializeField] private PlayerController player;

    private bool isPaused;
    private bool isGameOver;

    public static float LastSurvivalTime { get; private set; }
    public static string LastDeathReason { get; private set; }
    public static bool LastWasRecord { get; private set; }

    void OnEnable()
    {
        PlayerController.OnPlayerDied += HandlePlayerDied;
    }

    void OnDisable()
    {
        PlayerController.OnPlayerDied -= HandlePlayerDied;
    }

    void Start()
    {
        Time.timeScale = 1f;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    private void OnApplicationFocus(bool focus)
    {
        if (!focus) AutoPauseGame();
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause) AutoPauseGame();
    }

    private void AutoPauseGame()
    {
        if (!isPaused && !isGameOver) TogglePause();
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;

        if (pauseMenuCanvas != null)
            pauseMenuCanvas.SetActive(isPaused);
    }

    private void HandlePlayerDied(string reason)
    {
        if (isGameOver) return;

        isGameOver = true;
        LastDeathReason = reason;
        LastSurvivalTime = player != null ? player.SurvivalTime : 0f;
        LastWasRecord = DataPersistence.Instance != null && DataPersistence.Instance.TrySetRecord(LastSurvivalTime);
        Time.timeScale = 1f;
        SceneManager.LoadScene("GameOverScene");
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        LoadingScreenController.LoadScene("GameScene");
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
