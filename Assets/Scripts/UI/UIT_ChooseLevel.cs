using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class UIT_ChooseLevel : MonoBehaviour
{
    [SerializeField] private UIDocument _uiDocument;

    private VisualElement _root;
    private ScrollView _levelsList;
    private Image _previewImage;
    private VisualElement _previewRTSurface;
    private Label _previewTitle, _previewDesc;
    private Button _btnPlay, _btnBack;

    // Define your levels here. The SceneName must match your scene file names.
    // Place screenshots as PNG in: Assets/Resources/UI/LevelShots/<SceneName>.png
    private readonly List<LevelDef> _levels = new List<LevelDef>
    {
        new LevelDef("Level_1", "Container Port",  "Container port at sunset.",                  0.35f, 0.40f, 0.55f),
        new LevelDef("Level_1", "Industrial Yard", "Closer quarters, higher spawn rates.",        0.60f, 0.70f, 0.35f),
        new LevelDef("Level_1", "Old Town",        "Maze-like alleys, tougher elites.",           0.85f, 0.65f, 0.50f),
    };

    private int _current = -1;

    // Pulse state
    private Button _activeBtn;
    private VisualElement _activeAccentLine;
    private float _pulseTime;
    private readonly Dictionary<Button, VisualElement> _accentLines = new();

    // Info panel bars
    private VisualElement _difficultyBar, _densityBar, _areaBar;

    private void Awake()
    {
        _root = _uiDocument.rootVisualElement;

        _levelsList     = _root.Q<ScrollView>("levels-list");
        _previewImage   = _root.Q<Image>("preview-image");
        _previewRTSurface = _root.Q<VisualElement>("preview-rt-surface");
        _previewTitle   = _root.Q<Label>("preview-title");
        _previewDesc    = _root.Q<Label>("preview-desc");
        _btnPlay        = _root.Q<Button>("btn_play_level");
        _btnBack        = _root.Q<Button>("btn_back");
        _difficultyBar  = _root.Q<VisualElement>("difficulty-bar");
        _densityBar     = _root.Q<VisualElement>("density-bar");
        _areaBar        = _root.Q<VisualElement>("area-bar");

        BuildLevelButtons();

        _btnPlay?.RegisterCallback<ClickEvent>(_ => PlayCurrent());
        _btnBack?.RegisterCallback<ClickEvent>(_ => GoBack());
    }

    private void BuildLevelButtons()
    {
        _levelsList.Clear();

        for (int i = 0; i < _levels.Count; i++)
        {
            int idx = i;
            var def = _levels[i];

            var btn = new Button { name = $"btn_level_{i}" };
            btn.text = def.DisplayName;
            btn.AddToClassList("level-btn");
            btn.style.position = Position.Relative;
            btn.style.overflow = Overflow.Visible;

            // Dark topbar strip
            var topbar = new VisualElement();
            topbar.AddToClassList("level-btn__topbar");
            btn.Add(topbar);

            // "MAP" label on strip
            var tagLabel = new Label("MAP");
            tagLabel.AddToClassList("level-btn__label");
            btn.Add(tagLabel);

            // Dark bottombar strip
            var bottombar = new VisualElement();
            bottombar.AddToClassList("level-btn__bottombar");
            btn.Add(bottombar);

            // Level number bottom-right
            var numLabel = new Label($"{i + 1:00}");
            numLabel.AddToClassList("level-btn__number");
            btn.Add(numLabel);

            // Hover line — left side
            var hoverLine = new VisualElement();
            hoverLine.AddToClassList("level-btn__hover-line");
            btn.Add(hoverLine);

            // Accent line — right side, pulsed by Update() when selected
            var accentLine = new VisualElement();
            accentLine.AddToClassList("level-btn__accent-line");
            btn.Add(accentLine);
            _accentLines[btn] = accentLine;

            btn.RegisterCallback<ClickEvent>(_ => { SelectLevel(idx, btn); });
            btn.RegisterCallback<PointerEnterEvent>(_ => ShowPreview(idx));

            _levelsList.Add(btn);
        }

        if (_levels.Count > 0)
        {
            // Select first button
            var firstBtn = _levelsList.Q<Button>("btn_level_0");
            SelectLevel(0, firstBtn);
        }
    }

    private void SelectLevel(int index, Button btn)
    {
        ShowPreview(index);
        SetActiveButton(btn);
    }

    private void ShowPreview(int index)
    {
        if (index < 0 || index >= _levels.Count) return;
        _current = index;

        var def = _levels[index];

        var tex = Resources.Load<Texture2D>($"UI/LevelShots/{def.SceneName}");
        if (tex != null)
        {
            _previewImage.image = tex;
            _previewImage.style.display = DisplayStyle.Flex;
        }
        else
        {
            _previewImage.image = null;
            _previewImage.style.display = DisplayStyle.None;
        }

        if (_previewTitle != null) _previewTitle.text = def.DisplayName;
        if (_previewDesc  != null) _previewDesc.text  = def.Description;

        if (_difficultyBar != null) _difficultyBar.style.width = Length.Percent(def.Difficulty * 100f);
        if (_densityBar    != null) _densityBar.style.width    = Length.Percent(def.Density    * 100f);
        if (_areaBar       != null) _areaBar.style.width       = Length.Percent(def.AreaSize   * 100f);
    }

    private void SetActiveButton(Button btn)
    {
        if (_activeBtn != null)
        {
            _activeBtn.RemoveFromClassList("is-active");
            if (_accentLines.TryGetValue(_activeBtn, out var old))
                old.style.opacity = 0f;
        }

        _activeBtn = btn;
        _pulseTime = 0f;

        if (_activeBtn != null)
            _activeBtn.AddToClassList("is-active");
    }

    private void Update()
    {
        if (_activeBtn != null && _accentLines.TryGetValue(_activeBtn, out var accent))
        {
            _pulseTime += Time.deltaTime;
            float opacity = 0.3f + 0.7f * (0.5f + 0.5f * Mathf.Sin(_pulseTime * 3.5f));
            accent.style.opacity = opacity;
        }
    }

    private void PlayCurrent()
    {
        if (_current < 0 || _current >= _levels.Count) return;

        var sceneName = _levels[_current].SceneName;

        if (System.Enum.TryParse<Loader.Scene>(sceneName, ignoreCase: true, out var target))
        {
            Loader.Load(target);
        }
        else
        {
            Debug.LogWarning($"[ChooseLevel] Scene '{sceneName}' not found in Loader.Scene enum; loading by name.");
            SceneManager.LoadScene(sceneName);
        }
    }

    private void GoBack()
    {
        SceneManager.LoadScene("ChooseWeapon");
    }

    private class LevelDef
    {
        public string SceneName;
        public string DisplayName;
        public string Description;
        public float Difficulty;
        public float Density;
        public float AreaSize;

        public LevelDef(string sceneName, string displayName, string description,
                        float difficulty, float density, float areaSize)
        {
            SceneName   = sceneName;
            DisplayName = displayName;
            Description = description;
            Difficulty  = difficulty;
            Density     = density;
            AreaSize    = areaSize;
        }
    }
}
