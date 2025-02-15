using UnityEngine;
using UnityEngine.SceneManagement; // Required for scene management

public class LevelResetter : MonoBehaviour
{
    void Update()
    {
        // Check if the player presses the P key
        if (Input.GetKeyDown(KeyCode.P))
        {
            ResetLevel();
        }
    }

    /// <summary>
    /// Resets the current level by reloading the active scene.
    /// </summary>
    void ResetLevel()
    {
        // Get the active scene and reload it
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }
}
