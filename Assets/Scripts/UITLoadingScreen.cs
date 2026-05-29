using UnityEngine;
using UnityEngine.UIElements;

public class UITLoadingScreen : MonoBehaviour
{
    // How fast the fake bar creeps forward while the scene is still loading (units/sec, 0–0.85 range)
    [SerializeField] private float fakeSpeed = 0.12f;
    // How fast the bar animates to 100% once the real load is done
    [SerializeField] private float fillSpeed = 1.8f;

    private VisualElement _barFill;
    private Label _percentLabel;
    private float _displayedProgress;

    private void Awake()
    {
        var doc = GetComponent<UIDocument>();
        if (doc == null) return;

        var root = doc.rootVisualElement;
        _barFill = root.Q<VisualElement>("bar-fill");
        _percentLabel = root.Q<Label>("percent-label");
        _displayedProgress = 0f;
    }

    private void Update()
    {
        float realProgress = Loader.GetLoaderProgress();
        bool loadDone = realProgress >= 1f;

        if (loadDone)
        {
            // Real load finished — animate quickly to 100%
            _displayedProgress = Mathf.MoveTowards(_displayedProgress, 1f, fillSpeed * Time.unscaledDeltaTime);
        }
        else
        {
            // While loading: creep forward at fakeSpeed but never overtake the real progress
            // Cap fake at 0.85 so there's always a visible "jump" when load completes
            float fakeTarget = Mathf.Min(realProgress + 0.05f, 0.85f);
            _displayedProgress = Mathf.MoveTowards(_displayedProgress, fakeTarget, fakeSpeed * Time.unscaledDeltaTime);
        }

        if (_barFill != null)
            _barFill.style.width = new StyleLength(new Length(_displayedProgress * 100f, LengthUnit.Percent));

        if (_percentLabel != null)
            _percentLabel.text = Mathf.RoundToInt(_displayedProgress * 100f) + "%";

        // Once the bar visually reaches 100%, let Loader switch the scene
        if (_displayedProgress >= 0.999f)
            Loader.AllowActivation = true;
    }
}
