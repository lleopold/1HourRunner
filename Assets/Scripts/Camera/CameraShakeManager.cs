using MoreMountains.Feedbacks;
using UnityEngine;

/// <summary>
/// Manages camera shake effects based on weapon recoil and player strength
/// </summary>
public class CameraShakeManager : MonoBehaviour
{
    [Header("Shake Settings")]
    [Tooltip("Base duration of the camera shake in seconds")]
    [SerializeField] private float baseShakeDuration = 0.1f;

    [Tooltip("Base amplitude multiplier for shake intensity")]
    [SerializeField] private float baseAmplitude = 0.3f;

    [Tooltip("Shake frequency (higher = faster shake)")]
    [SerializeField] private float shakeFrequency = 25f;

    [Header("Scaling Factors")]
    [Tooltip("How much weapon recoil affects shake intensity (0-1)")]
    [SerializeField] private float recoilInfluence = 0.5f;

    [Tooltip("How much player strength reduces shake intensity (0-1)")]
    [SerializeField] private float strengthDampening = 0.3f;

    [Tooltip("Maximum shake amplitude cap")]
    [SerializeField] private float maxAmplitude = 1.0f;

    private static CameraShakeManager _instance;
    public static CameraShakeManager Instance => _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    /// <summary>
    /// Triggers a camera shake based on weapon recoil and player strength
    /// </summary>
    /// <param name="weaponRecoil">Weapon's recoil value (from WeaponConfig)</param>
    /// <param name="playerStrength">Player's strength value (from PlayerConfig)</param>
    public void ShakeOnFire(float weaponRecoil, float playerStrength)
    {
        // Calculate shake intensity based on weapon recoil
        float recoilFactor = 1f + (weaponRecoil / 100f) * recoilInfluence;

        // Calculate strength dampening (higher strength = less shake)
        float strengthFactor = 1f - Mathf.Clamp01(playerStrength / 100f) * strengthDampening;

        // Combine factors to get final amplitude
        float finalAmplitude = baseAmplitude * recoilFactor * strengthFactor;
        finalAmplitude = Mathf.Clamp(finalAmplitude, 0.05f, maxAmplitude);

        // Trigger the shake event
        MMCameraShakeEvent.Trigger(
            baseShakeDuration,
            finalAmplitude,
            shakeFrequency,
            0f, // amplitudeX
            0f, // amplitudeY
            0f, // amplitudeZ
            false, // infinite
            null, // channelData
            false // useUnscaledTime
        );
    }

    public void ShakeCustom(float duration, float amplitude, float frequency = 25f)
    {
        MMCameraShakeEvent.Trigger(
            duration,
            amplitude,
            frequency,
            0f, // amplitudeX
            0f, // amplitudeY
            0f, // amplitudeZ
            false, // infinite
            null, // channelData
            false // useUnscaledTime
        );
    }
}