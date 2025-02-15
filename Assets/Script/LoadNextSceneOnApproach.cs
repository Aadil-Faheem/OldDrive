using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadNextSceneOnApproach : MonoBehaviour
{
    [Tooltip("Tag of the player object.")]
    public string playerTag = "Player";

    [Tooltip("Distance at which the next scene will load.")]
    public float activationDistance = 5f;

    [Tooltip("Name of the next scene to load.")]
    public string nextSceneName;

    [Tooltip("UI Text element to display loading message.")]
    public Text loadingText;

    private Transform playerTransform;
    private bool isLoading = false;

    private void Start()
    {
        // Find the player object by tag
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);

        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogError("Player object not found. Make sure the player has the correct tag.");
        }

        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogError("Next scene name is not set. Please set the scene name in the inspector.");
        }

        if (loadingText != null)
        {
            loadingText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (playerTransform == null || isLoading) return;

        // Calculate the distance between the player and this object
        float distance = Vector3.Distance(playerTransform.position, transform.position);

        if (distance <= activationDistance)
        {
            LoadNextScene();
        }
    }

    private void LoadNextScene()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            isLoading = true;

            if (loadingText != null)
            {
                loadingText.gameObject.SetActive(true);
                loadingText.text = "Loading...";
            }

            StartCoroutine(LoadSceneAsync());
        }
    }

    private System.Collections.IEnumerator LoadSceneAsync()
    {
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(nextSceneName);
        while (!asyncOperation.isDone)
        {
            yield return null;
        }
    }
}
