using UnityEngine;

public class PlayerControlManager : MonoBehaviour
{
    [Header("First Person Setup")]
    public GameObject firstPersonCharacter; // First-person character GameObject
    public Camera firstPersonCamera; // First-person character's camera
    public MonoBehaviour firstPersonController; // First-person character's movement script

    [Header("Car Setup")]
    public GameObject car; // Car GameObject
    public Camera carCamera; // Car's main camera
    public MonoBehaviour carController; // Car controller script

    [Header("Settings")]
    public KeyCode switchControlKey = KeyCode.F; // Key to switch control between character and car

    private bool isControllingFirstPerson = true; // Tracks whether the player is controlling the character

    void Start()
    {
        // Ensure first-person character control is active at the start
        SetControl(firstPerson: true);
    }

    void Update()
    {
        // Check for control switch input
        if (Input.GetKeyDown(switchControlKey))
        {
            SetControl(firstPerson: !isControllingFirstPerson);
        }
    }

    private void SetControl(bool firstPerson)
    {
        isControllingFirstPerson = firstPerson;

        // First Person Control
        if (firstPersonCharacter != null)
        {
            firstPersonCharacter.SetActive(true); // Ensure the character is active
            if (firstPersonCamera != null) firstPersonCamera.enabled = firstPerson;
            if (firstPersonController != null) firstPersonController.enabled = firstPerson;
        }

        // Car Control
        if (car != null)
        {
            if (carCamera != null) carCamera.enabled = !firstPerson; // Toggle car camera
            if (carController != null) carController.enabled = !firstPerson; // Toggle car controller
        }

        Debug.Log($"Switched to {(firstPerson ? "First Person" : "Car")} control.");
    }
}
