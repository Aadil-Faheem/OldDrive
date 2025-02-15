using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseGame : MonoBehaviour
{
    private bool isPaused = false; // Tracks the pause state

    [SerializeField] private GameObject pauseMenuUI; // Reference to the Pause Menu UI GameObject
    [SerializeField] private AudioSource[] allAudioSources; // Array to hold all audio sources in the scene
    [SerializeField] private string mainMenuSceneName = "MainMenu"; // Name of the main menu scene

    void Update()
    {
        // Toggle pause state when the Escape key is pressed
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                Pause();
            }
        }
    }

    /// <summary>
    /// Pauses the game.
    /// </summary>
    public void Pause()
    {
        Time.timeScale = 0; // Freeze the game by setting time scale to 0
        isPaused = true;

        // Mute all audio sources
        foreach (var audioSource in allAudioSources)
        {
            if (audioSource != null)
            {
                audioSource.Pause();
            }
        }

        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(true); // Show the pause menu
        }
        Debug.Log("Game Paused");
    }

    /// <summary>
    /// Resumes the game.
    /// </summary>
    public void ResumeGame()
    {
        Time.timeScale = 1; // Resume the game by setting time scale to 1
        isPaused = false;

        // Unmute all audio sources
        foreach (var audioSource in allAudioSources)
        {
            if (audioSource != null)
            {
                audioSource.UnPause();
            }
        }

        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false); // Hide the pause menu
        }
        Debug.Log("Game Resumed");
    }

    /// <summary>
    /// Handles the Resume button click.
    /// </summary>
    public void OnResumeButtonClick()
    {
        ResumeGame();
    }

    /// <summary>
    /// Handles the Main Menu button click.
    /// </summary>
    public void OnMainMenuButtonClick()
    {
        Time.timeScale = 1; // Ensure the time scale is reset before switching scenes
        SceneManager.LoadScene(mainMenuSceneName);
    }

    /// <summary>
    /// Handles the Quit button click.
    /// </summary>
    public void OnQuitButtonClick()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
    }
}
