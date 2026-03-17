using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCameraFar : MonoBehaviour
{
    [SerializeField] public Transform targetCharacter; // Reference to the main character transform
    [SerializeField] private float cameraHeight = 5f; // Distance above the character
    [SerializeField] private float offsetX = 0f; // Horizontal offset from center
    [SerializeField] private float offsetZ = 0f; // Forward offset from center
    [SerializeField] private float currentZoom = 0f;
    [SerializeField] private float minZoom = 0.44f;
    [SerializeField] private float maxZoom = 2f; 
    [SerializeField] private float zoomSensitivity = 1f;
    public GameObject player;

    private void Start()
    {
        GameObject player = GameObject.Find("Player");
    }
    private void LateUpdate()
    {
        if (targetCharacter == null)
        {
            GameObject player = GameObject.Find("Player");
            targetCharacter = player.transform;
        }

        // Read scroll input and adjust zoom
        float scrollDelta = -Input.GetAxis("Mouse ScrollWheel");
        currentZoom = Mathf.Clamp(currentZoom + scrollDelta * zoomSensitivity, minZoom, maxZoom);

        // Update camera position with zoom
        transform.position = targetCharacter.position + Vector3.up * (cameraHeight * currentZoom) + Vector3.right * offsetX + Vector3.forward * offsetZ;
        // Look at the character from above
        transform.LookAt(targetCharacter, Vector3.up);

        // Clamp camera movement to prevent clipping through terrain (optional)
        // ...

    }
}
