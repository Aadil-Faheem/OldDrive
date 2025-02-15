using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbitCamera : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private Transform target; // The target to orbit around (e.g., the car)
    [SerializeField] private float sensitivity = 2f; // Mouse sensitivity
    [SerializeField] private float distance = 5f; // Distance from the target (car)
    [SerializeField] private float minDistance = 2f; // Minimum distance for zooming
    [SerializeField] private float maxDistance = 10f; // Maximum distance for zooming

    [Header("Rotation Limits")]
    [SerializeField] private float xRotationMin = -60f;
    [SerializeField] private float xRotationMax = 60f;
    [SerializeField] private float yRotationMin = -180f;
    [SerializeField] private float yRotationMax = 180f;

    [Header("Collision Settings")]
    [SerializeField] private LayerMask collisionLayerMask; // Layers to check for collisions (e.g., ground, obstacles)

    private Vector2 rotation = Vector2.zero; // Store rotation values
    private bool isLocked = false; // Lock status
    private Vector3 offset; // Offset from the target (car)
    private Vector3 lockedPosition; // Position to lock the camera to
    private Quaternion lockedRotation; // Rotation to lock the camera to

    const string xAxis = "Mouse X";
    const string yAxis = "Mouse Y";

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("Target not set! Please assign the target (e.g., the car) to the Orbit Camera script.");
            return;
        }

        // Initialize camera position relative to the target
        offset = new Vector3(0f, 2f, -distance); // Default offset for a third-person view
    }

    void Update()
    {
        // Toggle lock state when pressing M
        if (Input.GetKeyDown(KeyCode.M))
        {
            isLocked = !isLocked;

            // If locking, store the current position and rotation to lock to
            if (isLocked)
            {
                LockCameraPosition();
            }
        }

        // If camera is locked, do not process mouse input
        if (isLocked)
            return;

        // Update rotation values based on mouse input (x = horizontal, y = vertical)
        rotation.x += Input.GetAxis(xAxis) * sensitivity;
        rotation.y -= Input.GetAxis(yAxis) * sensitivity;

        // Clamp the vertical rotation to prevent flipping
        rotation.y = Mathf.Clamp(rotation.y, xRotationMin, xRotationMax);

        // Clamp the horizontal rotation (optional, can allow full 360 rotation)
        rotation.x = Mathf.Clamp(rotation.x, yRotationMin, yRotationMax);

        // Calculate the rotation based on mouse movement
        Quaternion rotationQuat = Quaternion.Euler(rotation.y, rotation.x, 0);

        // Apply the calculated rotation and adjust the camera position (distance from the car)
        Vector3 desiredPosition = target.position + rotationQuat * offset;

        // Check for collisions between the camera and the environment
        RaycastHit hit;
        if (Physics.Raycast(target.position, desiredPosition - target.position, out hit, distance, collisionLayerMask))
        {
            // If the ray hits an object, adjust the camera position to avoid clipping
            transform.position = hit.point;
        }
        else
        {
            // If no collision, move the camera to the desired position
            transform.position = desiredPosition;
        }

        // Make the camera look at the target
        transform.LookAt(target);
    }

    /// <summary>
    /// Locks the camera to its current position and rotation.
    /// </summary>
    void LockCameraPosition()
    {
        // Store the current position and rotation of the camera when locking
        lockedPosition = transform.position;
        lockedRotation = transform.rotation;
    }

    /// <summary>
    /// Manually resets the camera to its locked position if needed.
    /// </summary>
    void LockToPosition()
    {
        // Lock camera to the stored position and rotation
        transform.position = lockedPosition;
        transform.rotation = lockedRotation;
    }
}
