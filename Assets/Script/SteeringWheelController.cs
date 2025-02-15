using UnityEngine;

public class SteeringWheelController : MonoBehaviour
{
    public float rotationSpeed = 100f; // Speed of rotation while steering
    public float maxRotationAngle = 45f; // Maximum rotation angle for the steering wheel
    public float returnSpeed = 50f; // Speed at which the wheel returns to center
    public float overshootFactor = 10f; // How far the wheel can overshoot the center during return

    private float currentRotation = 0f;
    private Quaternion initialRotation; // To store the initial orientation of the steering wheel
    private bool isReturning = false; // Flag to track if the wheel is returning to center

    void Start()
    {
        // Store the initial rotation of the steering wheel
        initialRotation = transform.localRotation; // Using local rotation to maintain local axis
    }

    void Update()
    {
        // Get input for left (A) and right (D)
        float input = Input.GetAxis("Horizontal"); // A = -1, D = 1

        if (input != 0f)
        {
            // Calculate desired rotation based on input
            float desiredRotation = currentRotation + input * rotationSpeed * Time.deltaTime;

            // Clamp the rotation to the max limits
            currentRotation = Mathf.Clamp(desiredRotation, -maxRotationAngle, maxRotationAngle);

            // Reset the returning flag since we are steering
            isReturning = false;
        }
        else
        {
            // Gradually return the steering wheel to the center when no input is detected
            if (!isReturning && Mathf.Abs(currentRotation) < overshootFactor)
            {
                // Allow a slight overshoot to mimic real-world behavior
                currentRotation += Mathf.Sign(currentRotation) * overshootFactor;
                isReturning = true;
            }

            currentRotation = Mathf.MoveTowards(currentRotation, 0f, returnSpeed * Time.deltaTime);
        }

        // Apply the rotation in local space around the Z-axis for proper tilting (left/right tilt)
        transform.localRotation = initialRotation * Quaternion.Euler(0f, 0f, -currentRotation); // Inverted Z-axis rotation
    }
}
