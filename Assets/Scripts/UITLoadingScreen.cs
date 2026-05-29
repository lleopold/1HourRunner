using UnityEngine;
using UnityEngine.UIElements;

public class UITLoadingScreen : MonoBehaviour
{
    [SerializeField] private float spinDegreesPerSecond = 300f;

    private VisualElement _ring;
    private float _angle;

    private void Awake()
    {
        var doc = GetComponent<UIDocument>();
        if (doc == null) return;

        _ring = doc.rootVisualElement.Q<VisualElement>("spinner-ring");
        _angle = 0f;
    }

    private void Update()
    {
        // Spin the ring
        if (_ring != null)
        {
            _angle = (_angle + spinDegreesPerSecond * Time.unscaledDeltaTime) % 360f;
            _ring.transform.rotation = Quaternion.Euler(0f, 0f, -_angle);
        }

        // Allow scene activation as soon as Unity has finished loading
        if (Loader.GetLoaderProgress() >= 1f)
            Loader.AllowActivation = true;
    }
}
