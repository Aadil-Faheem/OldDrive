using UnityEngine;
using UnityEngine.UI;

public class LevelStartText : MonoBehaviour
{
    [SerializeField] private Text levelStartText; // Reference to the UI Text component
    [SerializeField] private float displayDuration = 3.0f; // Duration the text stays visible

    void Start()
    {
        if (levelStartText != null)
        {
            StartCoroutine(DisplayLevelStartText());
        }
    }

    private System.Collections.IEnumerator DisplayLevelStartText()
    {
        // Ensure the text is visible at the start
        levelStartText.gameObject.SetActive(true);

        // Wait for the specified duration
        yield return new WaitForSeconds(displayDuration);

        // Hide the text after the duration
        levelStartText.gameObject.SetActive(false);
    }
}
