using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarUpright : MonoBehaviour
{
    [Header("Upright Settings")]
    [SerializeField] private float uprightSpeed = 5f;  // Speed at which the car gets upright
    [SerializeField] private Vector3 uprightRotation = new Vector3(0f, 0f, 0f);  // The target rotation (upright position)

    private Quaternion targetRotation;

    void Start()
    {
        // Set the target rotation to the upright rotation at the start
        targetRotation = Quaternion.Euler(uprightRotation);
    }

    void Update()
    {
        // Check if the "R" key is pressed
        if (Input.GetKeyDown(KeyCode.R))
        {
            // Start the process to get the car upright
            StartCoroutine(RightCar());
        }
    }

    // Coroutine to smoothly rotate the car to the upright position
    private IEnumerator RightCar()
    {
        Quaternion initialRotation = transform.rotation;
        float timeElapsed = 0f;

        // While the car is not upright, smooth the rotation
        while (timeElapsed < 1f)
        {
            timeElapsed += Time.deltaTime * uprightSpeed;
            transform.rotation = Quaternion.Slerp(initialRotation, targetRotation, timeElapsed);
            yield return null;
        }

        // Make sure it's exactly upright when done
        transform.rotation = targetRotation;
    }
}
