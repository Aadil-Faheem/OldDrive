using UnityEngine;
using UnityEngine.UI; // Required for UI components

public class DisplayTextOnHold : MonoBehaviour
{
    public GameObject canvasText; // Reference to the Canvas Text GameObject

    void Start()
    {
        if (canvasText != null)
        {
            canvasText.SetActive(false); // Ensure the text is hidden initially
        }
    }

    void Update()
    {
        if (canvasText != null)
        {
            if (Input.GetKey(KeyCode.Tab))
            {
                canvasText.SetActive(true); // Show the text when Tab is held
            }
            else
            {
                canvasText.SetActive(false); // Hide the text when Tab is released
            }
        }
    }
}
