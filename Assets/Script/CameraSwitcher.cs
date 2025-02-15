using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    private Camera[] cameras; // Array to hold all cameras
    private int currentCameraIndex = 2; // Start with Camera2 as the active camera

    void Start()
    {
        // Manually initialize the cameras array with Camera, Camera1, and Camera2
        cameras = new Camera[3]; // Increase the size to accommodate the new camera
        cameras[0] = GameObject.Find("Camera").GetComponent<Camera>();  // Assign the first camera
        cameras[1] = GameObject.Find("Camera1").GetComponent<Camera>(); // Assign the new Camera1
        cameras[2] = GameObject.Find("Camera2").GetComponent<Camera>(); // Assign the second camera

        // Ensure only the currentCameraIndex camera (Camera2) is active at the start
        for (int i = 0; i < cameras.Length; i++)
        {
            bool isActive = i == currentCameraIndex;
            cameras[i].gameObject.SetActive(isActive);
            UpdateAudioListener(cameras[i], isActive);
        }
    }

    void Update()
    {
        // Check if the "C" key is pressed
        if (Input.GetKeyDown(KeyCode.C))
        {
            // Disable the current camera and its audio listener
            cameras[currentCameraIndex].gameObject.SetActive(false);
            UpdateAudioListener(cameras[currentCameraIndex], false);

            // Switch to the next camera
            currentCameraIndex = (currentCameraIndex + 1) % cameras.Length;

            // Enable the new active camera and its audio listener
            cameras[currentCameraIndex].gameObject.SetActive(true);
            UpdateAudioListener(cameras[currentCameraIndex], true);
        }
    }

    private void UpdateAudioListener(Camera camera, bool isActive)
    {
        // Ensure the camera has an AudioListener component
        AudioListener audioListener = camera.GetComponent<AudioListener>();
        if (audioListener != null)
        {
            audioListener.enabled = isActive;
        }
    }
}
