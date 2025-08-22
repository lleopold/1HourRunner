using UnityEngine;

public class AimingIndicator : MonoBehaviour
{
    public float length = 5f;           // Length of the V
    public float baseWidth = 0.2f;      // Base width of the V
    public float precisionMultiplier = 2f; // Adjust this multiplier for precision

    LineRenderer lineRenderer;
    Transform playerCamera; // Assuming the camera determines the aiming direction

    void Start() 
    {
        lineRenderer = GetComponent<LineRenderer>();
        playerCamera = Camera.main.transform; // Assuming the main camera is used
        UpdateAimingIndicator();
    }

    void Update()
    {
        // Update the aiming indicator only if the player is aiming or shooting
        if (IsAimingOrShooting())
        {
            UpdateAimingIndicator();
        }
        else
        {
            // Disable Line Renderer when not aiming or shooting
            lineRenderer.enabled = false;
        }
    }

    bool IsAimingOrShooting()
    {
        // Implement your logic to check if the player is aiming or shooting
        // For example, you can use Input.GetKey or another mechanism
        // Replace this with your actual implementation
        return Input.GetMouseButton(1); // Assuming right mouse button for aiming
    }

    void UpdateAimingIndicator()
    {
        // Enable Line Renderer when aiming or shooting
        lineRenderer.enabled = true;

        // Calculate the positions for the V shape
        Vector3 center = transform.position;
        Vector3 left = center - transform.right * (baseWidth * precisionMultiplier / 2f);
        Vector3 right = center + transform.right * (baseWidth * precisionMultiplier / 2f);

        // Calculate the endpoint based on the player's aiming direction
        Vector3 tip = center + playerCamera.forward * length;

        // Set positions for the Line Renderer
        lineRenderer.positionCount = 4;
        lineRenderer.SetPositions(new Vector3[] { left, center, right, tip });

        // You can further customize Line Renderer properties such as color, material, etc.
        lineRenderer.startWidth = baseWidth * precisionMultiplier;
        lineRenderer.endWidth = baseWidth * precisionMultiplier;
    }
}
