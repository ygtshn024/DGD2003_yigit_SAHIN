using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Start Butonu Ayari")]
    [Tooltip("Start'a basinca yuklenecek sahne adi. Bos birakirsan Build Settings'te bir sonraki sahne yuklenir.")]
    [SerializeField] private string startSceneName = "";

    public void StartGame()
    {
        if (!string.IsNullOrWhiteSpace(startSceneName))
        {
            SceneManager.LoadScene(startSceneName);
            return;
        }

        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = currentSceneIndex + 1;

        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            Debug.LogWarning("StartGame: Build Settings'te yüklenecek sonraki sahne yok.");
        }
    }

    public void QuitGame()
    {
        Debug.Log("QuitGame cagrildi.");
        Application.Quit();
    }
}
