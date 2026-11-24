using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingScreenController : MonoBehaviour
{
    private static string targetScene = "GameScene";

    [SerializeField] private Slider progressBar;
    [SerializeField] private float minShowTime = 1f;

    public static void LoadScene(string sceneName)
    {
        targetScene = sceneName;
        SceneManager.LoadScene("LoadingScene");
    }

    IEnumerator Start()
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(targetScene);
        operation.allowSceneActivation = false;

        float elapsed = 0f;
        while (operation.progress < 0.9f || elapsed < minShowTime)
        {
            elapsed += Time.unscaledDeltaTime;
            if (progressBar != null) progressBar.value = Mathf.Clamp01(operation.progress / 0.9f);
            yield return null;
        }

        if (progressBar != null) progressBar.value = 1f;
        operation.allowSceneActivation = true;
    }
}
