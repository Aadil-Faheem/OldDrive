using UnityEngine;

public class CarBlinker : MonoBehaviour
{
    [SerializeField] private Transform blinkys; // Parent object containing Turn_Lights_l and Turn_Lights_r
    [SerializeField] private float blinkInterval = 0.5f; // Interval between blinks
    [SerializeField] private AudioSource blinkerSound; // Sound for blinkers and hazard lights

    private Light[] leftBlinkers;  // Array for left blinker lights
    private Light[] rightBlinkers; // Array for right blinker lights

    private bool isLeftBlinkerOn = false; // Tracks if left blinker is active
    private bool isRightBlinkerOn = false; // Tracks if right blinker is active
    private bool isHazardOn = false; // Tracks if hazard lights are active
    private float blinkTimer = 0f; // Timer for blinking

    void Start()
    {
        if (blinkys == null)
        {
            Debug.LogError("Blinkys object is not assigned!");
            return;
        }

        // Initialize and assign left and right blinker lights from the hierarchy
        Transform leftGroup = blinkys.Find("Turn_Lights_l");
        Transform rightGroup = blinkys.Find("Turn_Lights_r");

        if (leftGroup == null || rightGroup == null)
        {
            Debug.LogError("Blinkys must contain Turn_Lights_l and Turn_Lights_r as children!");
            return;
        }

        // Assign the lights to arrays
        leftBlinkers = new Light[]
        {
            leftGroup.Find("LFBlinky")?.GetComponent<Light>(),
            leftGroup.Find("LRBlinky")?.GetComponent<Light>()
        };

        rightBlinkers = new Light[]
        {
            rightGroup.Find("RFBlinky")?.GetComponent<Light>(),
            rightGroup.Find("RBlinky")?.GetComponent<Light>()
        };

        // Ensure all lights are initially off
        SetBlinkersState(leftBlinkers, false);
        SetBlinkersState(rightBlinkers, false);
    }

    void Update()
    {
        // Check for blinker inputs
        if (Input.GetKeyDown(KeyCode.Q))
        {
            isLeftBlinkerOn = !isLeftBlinkerOn; // Toggle left blinker
            isRightBlinkerOn = false; // Disable right blinker if active
            isHazardOn = false; // Disable hazard if active
            HandleBlinkerSound();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            isRightBlinkerOn = !isRightBlinkerOn; // Toggle right blinker
            isLeftBlinkerOn = false; // Disable left blinker if active
            isHazardOn = false; // Disable hazard if active
            HandleBlinkerSound();
        }

        // Check for hazard input
        if (Input.GetKeyDown(KeyCode.G))
        {
            isHazardOn = !isHazardOn; // Toggle hazard lights
            isLeftBlinkerOn = false; // Disable left blinker if active
            isRightBlinkerOn = false; // Disable right blinker if active
            HandleBlinkerSound();
        }

        // Handle blinking logic
        HandleBlinkers();
    }

    /// <summary>
    /// Handles the blinking logic for both sets of blinkers and hazards.
    /// </summary>
    private void HandleBlinkers()
    {
        blinkTimer += Time.deltaTime;

        // Toggle blinker state at the blink interval
        if (blinkTimer >= blinkInterval)
        {
            blinkTimer = 0f;

            if (isLeftBlinkerOn)
                ToggleBlinkers(leftBlinkers);

            if (isRightBlinkerOn)
                ToggleBlinkers(rightBlinkers);

            if (isHazardOn)
            {
                ToggleBlinkers(leftBlinkers);
                ToggleBlinkers(rightBlinkers);
            }
        }

        // Ensure the other blinkers stay off
        if (!isLeftBlinkerOn && !isHazardOn)
            SetBlinkersState(leftBlinkers, false);

        if (!isRightBlinkerOn && !isHazardOn)
            SetBlinkersState(rightBlinkers, false);
    }

    /// <summary>
    /// Toggles the state of a set of blinkers.
    /// </summary>
    /// <param name="blinkers">The array of blinkers to toggle.</param>
    private void ToggleBlinkers(Light[] blinkers)
    {
        foreach (var blinker in blinkers)
        {
            if (blinker != null)
                blinker.enabled = !blinker.enabled;
        }
    }

    /// <summary>
    /// Sets the state of a set of blinkers.
    /// </summary>
    /// <param name="blinkers">The array of blinkers to modify.</param>
    /// <param name="state">True to turn them on, false to turn them off.</param>
    private void SetBlinkersState(Light[] blinkers, bool state)
    {
        foreach (var blinker in blinkers)
        {
            if (blinker != null)
                blinker.enabled = state;
        }
    }

    /// <summary>
    /// Handles the sound for blinkers or hazard lights.
    /// </summary>
    private void HandleBlinkerSound()
    {
        if (blinkerSound != null)
        {
            if (isLeftBlinkerOn || isRightBlinkerOn || isHazardOn)
            {
                if (!blinkerSound.isPlaying)
                {
                    blinkerSound.Play();
                }
            }
            else
            {
                blinkerSound.Stop();
            }
        }
    }
}
