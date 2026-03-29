using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Oyun bittiginde ana menuye donus. Build Settings'te ana menu genelde ilk sirada (FirstScene).
/// </summary>
public static class GameFlow
{
    public const string MainMenuSceneName = "FirstScene";

    public static void LoadMainMenu()
    {
        SceneManager.LoadScene(MainMenuSceneName);
    }
}
