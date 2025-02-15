using UnityEngine;

public class CarIgnitionSystem : MonoBehaviour
{
    public AudioSource ignitionSound;  // The sound that plays when the car starts
    public GameObject carObject;  // The GameObject that has the PrometeoCarController attached to it
    private PrometeoCarController carControllerScript;  // Reference to the PrometeoCarController script
    private bool isCarOn = false;  // Tracks whether the car is on or off
    private float ignitionHoldTime = 0f;  // Tracks how long the key has been held

    [Header("Ignition Settings")]
    [SerializeField]
    [Tooltip("Time in seconds to hold the I key to start the car")]
    private float ignitionRequiredTime = 2f;  // Time in seconds to hold the I key to start the car, adjustable in the Inspector

    // Light GameObjects (assign in the Inspector)
    public GameObject brakelightsObject;
    public GameObject carBlinkerObject;
    public GameObject headlightsObject;

    // Rigidbody for MyCar object to handle slowing down
    private Rigidbody carRigidbody;

    void Start()
    {
        // Get the PrometeoCarController script attached to the car object
        if (carObject != null)
        {
            carControllerScript = carObject.GetComponent<PrometeoCarController>();
        }

        // Get Rigidbody for slowing down MyCar
        carRigidbody = carObject.GetComponent<Rigidbody>();

        // Ensure the car starts off
        if (carControllerScript != null)
        {
            carControllerScript.enabled = false; // Car controller is off at the start
            StopCarSounds();  // Stop car sounds initially
        }

        // Ensure lights are off at the start
        TurnOffLights();
    }

    void Update()
    {
        HandleIgnition();
    }

    private void HandleIgnition()
    {
        // When the 'I' key is pressed, toggle the car's state
        if (Input.GetKey(KeyCode.I) && !isCarOn)
        {
            // Start playing ignition sound if not already playing
            if (!ignitionSound.isPlaying)
            {
                ignitionSound.Play();
            }

            ignitionHoldTime += Time.deltaTime;  // Track hold time for ignition

            if (ignitionHoldTime >= ignitionRequiredTime)
            {
                TurnCarOn();  // Turn the car on
                ignitionHoldTime = 0f;  // Reset hold time after car is started
            }
        }
        else
        {
            // Stop the ignition sound and reset hold time if the key is released
            if (ignitionSound.isPlaying && !isCarOn)
            {
                ignitionSound.Stop();
            }

            ignitionHoldTime = 0f;
        }

        // Turn off the car with a simple press of the 'I' key if it's already on
        if (Input.GetKeyDown(KeyCode.I) && isCarOn)
        {
            TurnCarOff();  // Turn the car off
        }
    }

    private void TurnCarOn()
    {
        if (carControllerScript != null)
        {
            carControllerScript.enabled = true;  // Enable the car controller script
            isCarOn = true;  // Mark the car as on
            ignitionSound.Stop();  // Stop the ignition sound once the car is on
            PlayCarSounds();  // Play car sounds (engine and tire screech)
            Debug.Log("Car is ON!");

            // Ensure lights are on when the car is on
            TurnOnLights();
        }
    }

    private void TurnCarOff()
    {
        if (carControllerScript != null)
        {
            carControllerScript.enabled = false;  // Disable the car controller script
            isCarOn = false;  // Mark the car as off
            StopCarSounds();  // Stop the car's sounds when it's off
            Debug.Log("Car is OFF!");

            // Turn off lights when the car is off
            TurnOffLights();

            // Slow down the car and stop it
            SlowDownCar();
        }
    }

    private void PlayCarSounds()
    {
        // Ensure that the car sounds (engine and tire screech) play when the car is turned on
        if (carControllerScript != null)
        {
            if (carControllerScript.carEngineSound != null && !carControllerScript.carEngineSound.isPlaying)
            {
                carControllerScript.carEngineSound.Play();
            }

            if (carControllerScript.tireScreechSound != null && !carControllerScript.tireScreechSound.isPlaying)
            {
                carControllerScript.tireScreechSound.Play();
            }

        }
    }

    private void StopCarSounds()
    {
        // Stop the car sounds (engine and tire screech) when the car is turned off
        if (carControllerScript != null)
        {
            if (carControllerScript.carEngineSound != null)
            {
                carControllerScript.carEngineSound.Stop();
            }

            if (carControllerScript.tireScreechSound != null)
            {
                carControllerScript.tireScreechSound.Stop();
            }
        }
    }

    private void TurnOnLights()
    {
        // Turn on the car's light GameObjects
        if (brakelightsObject != null) brakelightsObject.SetActive(true);
        if (carBlinkerObject != null) carBlinkerObject.SetActive(true);
        if (headlightsObject != null) headlightsObject.SetActive(true);
    }

    private void TurnOffLights()
    {
        // Turn off the car's light GameObjects
        if (brakelightsObject != null) brakelightsObject.SetActive(false);
        if (carBlinkerObject != null) carBlinkerObject.SetActive(false);
        if (headlightsObject != null) headlightsObject.SetActive(false);
    }

    private void SlowDownCar()
    {
        // Slowly decelerate the car and stop it when ignition is off
        if (carRigidbody != null)
        {
            carRigidbody.velocity = Vector3.Lerp(carRigidbody.velocity, Vector3.zero, Time.deltaTime * 2f);  // Adjust the deceleration speed as needed
        }
    }
}
