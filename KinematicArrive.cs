using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class KinematicArrive : MonoBehaviour
{
    // Nested class to store the steering information
    public class Steering
    {
        public Vector3 velocity;       // Movement velocity vector
        public Quaternion rotation;    // Desired rotation of the player

        // Constructor to initialize the velocity and rotation
        public Steering() {
            velocity = Vector3.zero;   // Default velocity is zero
            rotation = Quaternion.identity; // Default rotation is identity (no rotation)
        }
    }

    // Serialized field for setting a target in the scene
    [SerializeField]
    private Vector3 target = new Vector3(0, .5f, 0); // Initial target for movement

    private Rigidbody rb;  // Rigidbody component for physics interaction

    // Offsets for positioning the target slightly above the ground (optional)
    public Vector3 targetOffset = new Vector3(0, .5f, 0);

    // Parameters for controlling movement and rotation
    public float timeToTarget = 1f;       // Time to reach the target
    public float turnSpeed = .1f;         // Speed at which the player rotates
    public float maxSpeed = 10f;          // Maximum movement speed
    public float satisfactionRadius = .1f; // Radius within which the player considers they have arrived

    // Flags for controlling movement behavior
    public bool speedLimit = true;        // Limit speed to maxSpeed
    public bool gravity = true;           // Allow gravity to affect vertical movement

    // For debugging, stores the last ray that was cast when clicking
    public Ray lastRay = new Ray(); 

    // Method called when the script is first initialized
    void Start() {
        // Get the Rigidbody component attached to the player
        rb = GetComponent<Rigidbody>();

        // Freeze rotation on the X and Z axes to prevent the player from tipping over
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ; 
    }

    // Method to calculate the movement (steering) towards the target
    Steering getSteering() {
        Steering steering = new Steering(); // Create a new steering object

        // Calculate the direction towards the target
        steering.velocity = target - transform.position;

        // Ensure movement only happens on the XZ plane (ignore Y axis)
        steering.velocity.y = 0;

        // Check if the player is within the satisfaction radius (close enough to stop)
        if (steering.velocity.magnitude < satisfactionRadius) {
            steering.velocity = Vector3.zero; // Stop movement when close enough
            steering.rotation = transform.rotation; // Maintain current rotation
            return steering;
        }

        // Calculate the desired speed to reach the target in the specified time
        steering.velocity /= timeToTarget;

        // If speed limiting is enabled, ensure the player doesn't exceed maxSpeed
        if (steering.velocity.magnitude > maxSpeed && speedLimit) {
            steering.velocity.Normalize(); // Normalize to retain direction
            steering.velocity *= maxSpeed; // Scale velocity to maxSpeed
        }

        // Set the rotation to face the movement direction
        steering.rotation = Quaternion.LookRotation(steering.velocity);

        return steering; // Return the steering information (velocity and rotation)
    }

    // Method to apply the calculated steering to the player's movement and rotation
    void setOrientation(Steering steering) {
        // Apply the velocity (movement) to the player's Rigidbody
        rb.velocity = steering.velocity;

        // Only rotate the player if they are moving (velocity is not zero)
        if (steering.velocity.sqrMagnitude > 0.01f) {
            Quaternion targetRotation = steering.rotation; // Target rotation (direction of movement)

            // Smoothly rotate the player to face the target direction using Slerp (Spherical Linear Interpolation)
            // turnSpeed is multiplied by Time.deltaTime to control the smoothness without being too slow
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime * 60f);
        }
    }

    // Method to handle input and update the target position
    void getTarget() {
        // If the player clicks the left mouse button
        if (Input.GetMouseButton(0)) {
            // Cast a ray from the camera through the mouse click position
            Ray dir = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Check if the ray hits any object in the scene (like the ground)
            if (Physics.Raycast(dir, out hit)) {
                // If the clicked position is far enough from the player, update the target
                if (Vector3.Distance(transform.position, hit.point) > satisfactionRadius) {
                    target = hit.point + targetOffset; // Set the new target with offset
                }
            }

            lastRay = dir; // Store the last ray for debugging purposes
        }
    }

    // Update is called once per frame
    void Update() {
        // Update the target position based on mouse input
        getTarget();

        // Update the player's movement and rotation based on steering
        setOrientation(getSteering());
    }
}
