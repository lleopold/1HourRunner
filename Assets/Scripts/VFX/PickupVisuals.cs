using UnityEngine;

public class PickupVisuals : MonoBehaviour
{
    [Header("Glow Settings")]
    [SerializeField] private MeshRenderer _mainRenderer;
    [SerializeField] private Light _pointLight;
    [SerializeField] private float _pulseSpeed = 2f;
    [SerializeField] private float _minEmissionMultiplier = 0.5f;
    [SerializeField] private float _maxEmissionMultiplier = 2.0f;
    [SerializeField] private float _minLightIntensity = 0.5f;
    [SerializeField] private float _maxLightIntensity = 1.5f;

    [Header("Animation Settings")]
    [SerializeField] private float _bobSpeed = 1.5f;
    [SerializeField] private float _bobAmount = 0.1f;

    private Material _mainMaterial;
    private Color _baseEmissionColor;
    private Vector3 _startLocalPosition;
    private float _timeOffset;

    void Awake()
    {
        if (_mainRenderer != null)
        {
            _mainMaterial = _mainRenderer.material;
            _baseEmissionColor = _mainMaterial.GetColor("_EmissionColor");
        }
        _startLocalPosition = transform.localPosition;
        _timeOffset = Random.Range(0f, 10f);
    }

    void Update()
    {
        float sine = Mathf.Sin((Time.time + _timeOffset) * _pulseSpeed);
        float normalizedSine = (sine + 1f) / 2f; // 0 to 1
        float multiplier = Mathf.Lerp(_minEmissionMultiplier, _maxEmissionMultiplier, normalizedSine);

        // Pulse Emission
        if (_mainMaterial != null)
        {
            _mainMaterial.SetColor("_EmissionColor", _baseEmissionColor * multiplier);
        }

        // Pulse Light
        if (_pointLight != null)
        {
            _pointLight.intensity = Mathf.Lerp(_minLightIntensity, _maxLightIntensity, normalizedSine);
        }

        // Bobbing effect - Removed because it overrides global movement in Coin.cs
        // transform.localPosition = _startLocalPosition + Vector3.up * Mathf.Sin((Time.time + _timeOffset) * _bobSpeed) * _bobAmount;
    }

    private void OnDestroy()
    {
        if (_mainMaterial != null)
        {
            Destroy(_mainMaterial);
        }
    }
}
