using UnityEngine;

public class brakelights : MonoBehaviour
{
    [SerializeField] private GameObject leftBrakeLight;  // The left brake light GameObject
    [SerializeField] private GameObject rightBrakeLight; // The right brake light GameObject
    [SerializeField] private Color brakeLightColor = Color.red; // The color of the brake light
    [SerializeField] private Color offLightColor = Color.black; // The color of the brake light when off

    private Light leftLightComponent;  // Light component of the left brake light
    private Light rightLightComponent; // Light component of the right brake light

    void Start()
    {
        // Get the Light components from the brake light GameObjects
        if (leftBrakeLight != null)
            leftLightComponent = leftBrakeLight.GetComponent<Light>();
        
        if (rightBrakeLight != null)
            rightLightComponent = rightBrakeLight.GetComponent<Light>();

        // Initialize lights to be off
        SetBrakeLights(false);
    }

    void Update()
    {
        // Check if the S key is pressed or released
        if (Input.GetKey(KeyCode.S))
        {
            SetBrakeLights(true);
        }
        else
        {
            SetBrakeLights(false);
        }
    }

    /// <summary>
    /// Turns the brake lights on or off.
    /// </summary>
    /// <param name="isActive">True to activate the lights, false to deactivate.</param>
    private void SetBrakeLights(bool isActive)
    {
        if (leftLightComponent != null)
            leftLightComponent.color = isActive ? brakeLightColor : offLightColor;

        if (rightLightComponent != null)
            rightLightComponent.color = isActive ? brakeLightColor : offLightColor;
    }
}
