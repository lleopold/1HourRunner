using MoreMountains.Feedbacks;
using UnityEngine;

/// <summary>
/// Automatically sets up the camera shake component on the main camera
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraShakeSetup : MonoBehaviour
{
    private void Awake()
    {
        // Check if MMCameraShaker already exists
        if (GetComponent<MMCameraShaker>() == null)
        {
            // Add the shaker component
            MMCameraShaker shaker = gameObject.AddComponent<MMCameraShaker>();

            // Configure the wiggle component
            MMWiggle wiggle = GetComponent<MMWiggle>();
            if (wiggle != null)
            {
                wiggle.PositionActive = true;
                wiggle.PositionWiggleProperties = new WiggleProperties
                {
                    WigglePermitted = false,
                    WiggleType = WiggleTypes.Noise
                };
            }

            Debug.Log("MMCameraShaker component added to camera");
        }
    }
}