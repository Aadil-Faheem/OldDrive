using UnityEngine;

public class HeadlightsController : MonoBehaviour
{
    [SerializeField] private Light[] headlights; // Array to hold headlight objects
    [SerializeField] private Light[] otherLights; // Array to hold other light objects (e.g., interior, auxiliary lights)
    [SerializeField] private Light[] dipperLights; // Array to hold independent dipper light objects
    [SerializeField] private float dipperDuration = 0.5f; // Duration for which the dipper lights stay on

    private bool areHeadlightsOn = true; // Headlights are always on by default
    private bool areOtherLightsOn = false; // Tracks whether the other lights are on or off
    private bool isDipperActive = false; // Tracks if the dipper lights are currently active

    void Start()
    {
        // Ensure headlights are always on at the start
        SetHeadlightsState(true);
        SetOtherLightsState(false);
        SetDipperLightsState(false);
    }

    void Update()
    {
        // Prevent toggling headlights when L is pressed

        // Toggle other lights when K is pressed
        if (Input.GetKeyDown(KeyCode.K))
        {
            if (!areOtherLightsOn) // If other lights are off
            {
                areOtherLightsOn = true;
                SetOtherLightsState(true);
            }
            else // If other lights are already on
            {
                areOtherLightsOn = false;
                SetOtherLightsState(false); // Only turn off other lights
            }
        }

        // Activate dipper lights when F is pressed
        if (Input.GetKeyDown(KeyCode.F) && !isDipperActive)
        {
            StartCoroutine(ActivateDipperLights());
        }
    }

    /// <summary>
    /// Sets the state of the headlights.
    /// </summary>
    /// <param name="state">True to turn on, False to turn off.</param>
    private void SetHeadlightsState(bool state)
    {
        foreach (var headlight in headlights)
        {
            if (headlight != null)
                headlight.enabled = state;
        }
    }

    /// <summary>
    /// Sets the state of the other lights.
    /// </summary>
    /// <param name="state">True to turn on, False to turn off.</param>
    private void SetOtherLightsState(bool state)
    {
        foreach (var light in otherLights)
        {
            if (light != null)
                light.enabled = state;
        }
    }

    /// <summary>
    /// Sets the state of the dipper lights.
    /// </summary>
    /// <param name="state">True to turn on, False to turn off.</param>
    private void SetDipperLightsState(bool state)
    {
        foreach (var dipperLight in dipperLights)
        {
            if (dipperLight != null)
                dipperLight.enabled = state;
        }
    }

    /// <summary>
    /// Activates the dipper lights for a brief duration.
    /// </summary>
    private System.Collections.IEnumerator ActivateDipperLights()
    {
        isDipperActive = true;

        // Temporarily turn on the dipper lights
        SetDipperLightsState(true);
        yield return new WaitForSeconds(dipperDuration);

        // Turn off the dipper lights
        SetDipperLightsState(false);
        isDipperActive = false;
    }
}
