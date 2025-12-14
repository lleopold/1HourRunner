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
        // Ensure MMCameraShaker exists on THIS camera
        var shaker = GetComponent<MMCameraShaker>();
        if (shaker == null)
        {
            shaker = gameObject.AddComponent<MMCameraShaker>();
        }

        // Ensure MMWiggle exists on THIS camera
        var wiggle = GetComponent<MMWiggle>();
        if (wiggle == null)
        {
            wiggle = gameObject.AddComponent<MMWiggle>();
        }

        // Configure position wiggle for camera shaking, but DON'T permanently disable it
        wiggle.PositionActive = true;
        if (wiggle.PositionWiggleProperties == null)
        {
            wiggle.PositionWiggleProperties = new WiggleProperties();
        }

        // Reasonable defaults – MMShaker will override amplitude/frequency on each shake
        wiggle.PositionWiggleProperties.WigglePermitted = true;
        wiggle.PositionWiggleProperties.WiggleType = WiggleTypes.Noise;
        wiggle.PositionWiggleProperties.RelativeAmplitude = true;

        Debug.Log("Camera shake setup complete: MMCameraShaker + MMWiggle attached to camera.");
    }
}