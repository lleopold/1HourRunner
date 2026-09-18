using UnityEngine;
using UnityEngine.UIElements;

// Counts down the run's survival timer and ends the run as a win when it reaches zero.
// survivalSeconds is per-level: overridden on the LevelSystems prefab instance in each level scene.
public class LevelTimer : MonoBehaviour
{
    [SerializeField] private float survivalSeconds = 180f;
    [SerializeField] private UIDocument _uiDocument;

    private Label _timerLabel;
    private UIT_GameScreen _gameScreen;
    private float _remaining;
    private bool _finished;

    private void OnEnable()
    {
        _remaining = survivalSeconds;
        _finished = false;

        if (_uiDocument == null)
        {
            Debug.LogError("[LevelTimer] No UIDocument assigned — countdown will not display.");
        }
        else
        {
            _timerLabel = _uiDocument.rootVisualElement.Q<Label>("lbl_extraction");
        }

        _gameScreen = FindObjectOfType<UIT_GameScreen>();
        UpdateLabel();
    }

    private void Update()
    {
        if (_finished) return;

        // Scaled time on purpose: Time.timeScale = 0f pause must freeze the countdown.
        _remaining -= Time.deltaTime;

        if (_remaining <= 0f)
        {
            _remaining = 0f;
            _finished = true;
            UpdateLabel();
            _gameScreen?.ShowEndGame("Extracted");
            return;
        }

        UpdateLabel();
    }

    private void UpdateLabel()
    {
        if (_timerLabel == null) return;

        int minutes = Mathf.FloorToInt(_remaining / 60f);
        int seconds = Mathf.FloorToInt(_remaining % 60f);
        _timerLabel.text = string.Format("EXTRACTION {0:00}:{1:00}", minutes, seconds);

        _timerLabel.EnableInClassList("extraction--warn", _remaining <= 60f && _remaining > 10f);
        _timerLabel.EnableInClassList("extraction--critical", _remaining <= 10f);
    }
}
