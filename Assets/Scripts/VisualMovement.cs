using UnityEngine;

public class VisualMovement : MonoBehaviour
{
    [Tooltip("How fast the sprite moves up and down.")]
    [SerializeField]
    private float moveSpeed = 1.0f; // Adjust this in the Inspector for the speed of the animation

    [Tooltip("The maximum distance the sprite moves up or down from its starting position.")]
    [SerializeField]
    private float moveDistance = 5.0f; // Adjust this in the Inspector. 5.0 means it will go 5 units up and 5 units down from its center.

    private Vector3 initialPosition; // Stores the GameObject's starting position

    void Awake()
    {
        // Store the initial position of the GameObject when the script starts.
        // This ensures the movement is relative to where you place the sprite in the scene.
        initialPosition = transform.position;
    }

    void Update()
    {
        // Calculate the new Y position using a sine wave.
        // Mathf.Sin(Time.time * moveSpeed) creates a smooth oscillation between -1 and 1 over time.
        // Multiplying by moveDistance scales this oscillation to the desired up/down range.
        float newY = initialPosition.y + Mathf.Sin(Time.time * moveSpeed) * moveDistance;

        // Apply the new position to the GameObject, keeping its X and Z coordinates unchanged.
        transform.position = new Vector3(initialPosition.x, newY, initialPosition.z);
    }
}