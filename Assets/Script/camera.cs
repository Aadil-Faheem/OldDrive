using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class camera : MonoBehaviour
{
    public float Sensitivity
    {
        get { return sensitivity; }
        set { sensitivity = value; }
    }

    [Range(0.1f, 9f)] [SerializeField] float sensitivity = 2f;
    [Tooltip("Limits vertical camera rotation. Prevents flipping when rotation goes above 90.")]
    [Range(0f, 90f)] [SerializeField] float yRotationLimit = 88f;

    [Tooltip("Limits horizontal camera rotation. Set both to 0 for no X limit.")]
    [SerializeField] float xRotationMin = -90f;
    [SerializeField] float xRotationMax = 90f;

    [Tooltip("Locks Z rotation. Set to 0 for no Z tilt.")]
    [SerializeField] float zRotation = 0f;

    [Header("Zoom Settings")]
    [SerializeField] float zoomedFOV = 40f; // Field of view when zoomed in
    [SerializeField] float normalFOV = 60f; // Normal field of view
    [SerializeField] float zoomSpeed = 10f; // Speed of zooming in and out

    private Vector2 rotation = Vector2.zero;
    private Quaternion initialRotation; // Store the initial rotation of the camera
    private bool isLocked = false; // Toggle to track whether the camera is locked
    private Camera cam; // Reference to the Camera component

    const string xAxis = "Mouse X";
    const string yAxis = "Mouse Y";

    void Start()
    {
        // Set the initial rotation to face forward
        initialRotation = Quaternion.Euler(0f, 0f, 0f);
        transform.localRotation = initialRotation;

        // Initialize rotation values to match the initial rotation
        rotation.x = 0f; // Start with no horizontal rotation
        rotation.y = 0f; // Start with no vertical rotation

        // Get the Camera component
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("Camera component is missing!");
        }
    }

    void Update()
    {
        // Toggle lock state when M is pressed
        if (Input.GetKeyDown(KeyCode.M))
        {
            isLocked = !isLocked;

            // If locking, reset the camera to its centered position
            if (isLocked)
            {
                LockToCenter();
            }
        }

        // Skip mouse input updates if the camera is locked
        if (isLocked)
            return;

        // Update X and Y rotation based on mouse input
        rotation.x += Input.GetAxis(xAxis) * sensitivity;
        rotation.y += Input.GetAxis(yAxis) * sensitivity;

        // Clamp the Y rotation to prevent flipping
        rotation.y = Mathf.Clamp(rotation.y, -yRotationLimit, yRotationLimit);

        // Clamp the X rotation to the specified range
        rotation.x = Mathf.Clamp(rotation.x, xRotationMin, xRotationMax);

        // Apply rotations with the clamped values
        var xQuat = Quaternion.AngleAxis(rotation.x, Vector3.up);
        var yQuat = Quaternion.AngleAxis(rotation.y, Vector3.left);
        var zQuat = Quaternion.AngleAxis(zRotation, Vector3.forward);

        // Combine rotations and apply to the camera
        transform.localRotation = xQuat * yQuat * zQuat;

        // Zoom in/out based on right mouse button input
        HandleZoom();
    }

    /// <summary>
    /// Handles zooming in and out when holding or releasing the right mouse button.
    /// </summary>
    void HandleZoom()
    {
        if (cam == null) return;

        // Target field of view
        float targetFOV = Input.GetMouseButton(1) ? zoomedFOV : normalFOV;

        // Smoothly interpolate between current FOV and target FOV
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * zoomSpeed);
    }

    /// <summary>
    /// Locks the camera to its initial (centered) rotation.
    /// </summary>
    void LockToCenter()
    {
        rotation = Vector2.zero; // Reset rotation values
        transform.localRotation = initialRotation; // Reset to stored initial rotation
    }
}
