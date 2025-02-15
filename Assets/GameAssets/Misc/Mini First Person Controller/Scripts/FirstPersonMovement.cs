using System.Collections.Generic;
using UnityEngine;

public class FirstPersonMovement : MonoBehaviour
{
    public float speed = 5;

    [Header("Running")]
    public bool canRun = true;
    public bool IsRunning { get; private set; }
    public float runSpeed = 9;
    public KeyCode runningKey = KeyCode.LeftShift;

    [Header("Zoom Settings")]
    public bool canZoom = true;
    public float zoomFOV = 30f; // Field of view when zoomed in
    public float normalFOV = 60f; // Normal field of view
    public float zoomSpeed = 10f; // Speed of FOV transition
    private Camera playerCamera;

    [Header("Interaction Settings")]
    public float interactionDistance = 3f; // How far the player can interact
    public LayerMask interactableLayer; // Layer for interactable objects

    Rigidbody rigidbody;

    /// <summary> Functions to override movement speed. Will use the last added override. </summary>
    public List<System.Func<float>> speedOverrides = new List<System.Func<float>>();

    void Awake()
    {
        // Get the rigidbody on this.
        rigidbody = GetComponent<Rigidbody>();

        // Get the Camera component for zoom functionality.
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
        {
            Debug.LogError("No camera found on the player. Please ensure the player has a Camera component.");
        }
    }

    void FixedUpdate()
    {
        // Update IsRunning from input.
        IsRunning = canRun && Input.GetKey(runningKey);

        // Get targetMovingSpeed.
        float targetMovingSpeed = IsRunning ? runSpeed : speed;
        if (speedOverrides.Count > 0)
        {
            targetMovingSpeed = speedOverrides[speedOverrides.Count - 1]();
        }

        // Get targetVelocity from input.
        Vector2 targetVelocity = new Vector2(Input.GetAxis("Horizontal") * targetMovingSpeed, Input.GetAxis("Vertical") * targetMovingSpeed);

        // Apply movement.
        rigidbody.velocity = transform.rotation * new Vector3(targetVelocity.x, rigidbody.velocity.y, targetVelocity.y);
    }

    void Update()
    {
        HandleZoom();
        HandleInteraction();
    }

    /// <summary>
    /// Handles zoom functionality.
    /// </summary>
    private void HandleZoom()
    {
        if (canZoom && playerCamera != null)
        {
            float targetFOV = Input.GetMouseButton(1) ? zoomFOV : normalFOV; // Right mouse button toggles zoom
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * zoomSpeed);
        }
    }

    /// <summary>
    /// Handles interaction with objects.
    /// </summary>
    private void HandleInteraction()
    {
        if (Input.GetMouseButtonDown(0)) // Left mouse button
        {
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactableLayer))
            {
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                if (interactable != null)
                {
                    interactable.Interact();
                }
                else
                {
                    Debug.Log($"Interacted with {hit.collider.name}, but no IInteractable script found.");
                }
            }
        }
    }
}

/// <summary>
/// Interface for interactable objects.
/// </summary>
public interface IInteractable
{
    void Interact();
}
