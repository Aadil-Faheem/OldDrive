using UnityEngine;
using UnityEngine.SceneManagement; // Required for loading scenes

public class SimpleMenu : MonoBehaviour
{
    public void StartGame()
    {
        // Load the main game scene (replace "GameScene" with your scene name)
        SceneManager.LoadScene("GameScene");
    }

    public void OpenSettings()
    {
        // Open the settings menu (optional implementation, e.g., enabling a settings panel)
        Debug.Log("Settings menu opened.");
    }

    public void QuitGame()
    {
        // Quit the application
        Debug.Log("Game is quitting.");
        Application.Quit();
    }
}
