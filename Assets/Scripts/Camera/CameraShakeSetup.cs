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
            // Add the shaker component (MMWiggle is auto-added via RequireComponent)
            gameObject.AddComponent<MMCameraShaker>();

            // Ensure the wiggle component exists and is enabled for position shake
            MMWiggle wiggle = GetComponent<MMWiggle>();
            if (wiggle == null)
            {
                wiggle = gameObject.AddComponent<MMWiggle>();
            }

            wiggle.PositionActive = true;
            wiggle.PositionWiggleProperties = new WiggleProperties
            {
                // Idle until MMCameraShaker triggers a finite WigglePosition(duration) on fire.
                // Leaving this true makes the camera noise-wiggle permanently from game start.
                WigglePermitted = false,
                WiggleType = WiggleTypes.Noise
            };

            Debug.Log("MMCameraShaker component added to camera");
        }
    }
}