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
    private Label _previewTitle, _previewDesc;
    private Button _btnPlay, _btnBack;

    private readonly List<LevelDef> _levels = new List<LevelDef>
    {
        new LevelDef("Level_1", "Container Port",  "Container port at sunset.", "OUTDOOR", 0.35f, 0.40f, 0.55f),
        new LevelDef("Level_2", "Industrial Yard", "Industrial yard, tighter combat.", "INDOOR",  0.60f, 0.70f, 0.35f),
    };

    private int _current = -1;       // hover preview index (visual only)
    private int _selectedIndex = 0;  // clicked selection — what Play launches
    private LevelButton _activeBtn;
    private readonly List<LevelButton> _buttons = new();

    private VisualElement _difficultyBar, _densityBar, _areaBar;
    private Label _difficultyVal, _densityVal, _areaVal, _bestTimeVal;

    private void Awake()
    {
        _root = _uiDocument.rootVisualElement;

        _levelsList = _root.Q<ScrollView>("levels-list");
        _previewImage = _root.Q<Image>("preview-image");
        _previewTitle = _root.Q<Label>("preview-title");
        _previewDesc = _root.Q<Label>("preview-desc");
        _btnPlay = _root.Q<Button>("btn_play_level");
        _btnBack = _root.Q<Button>("btn_back");
        _difficultyBar = _root.Q<VisualElement>("difficulty-bar");
        _densityBar = _root.Q<VisualElement>("density-bar");
        _areaBar = _root.Q<VisualElement>("area-bar");
        _difficultyVal = _root.Q<Label>("difficulty-val");
        _densityVal = _root.Q<Label>("density-val");
        _areaVal = _root.Q<Label>("area-val");
        _bestTimeVal = _root.Q<Label>("best-time-val");

        BuildLevelButtons();

        _btnPlay?.RegisterCallback<ClickEvent>(_ => PlayCurrent());
        _btnBack?.RegisterCallback<ClickEvent>(_ => GoBack());
    }

    private void BuildLevelButtons()
    {
        _levelsList.Clear();
        _buttons.Clear();

        for (int i = 0; i < _levels.Count; i++)
        {
            int idx = i;
            var def = _levels[i];

            var lb = new LevelButton();
            lb.name = $"level_btn_{i}";
            lb.SetTexts("MAP", def.DisplayName.ToUpper(), $"{i + 1:00}", def.Tag);
            lb.Clicked += btn => SelectLevel(idx, btn);
            lb.RegisterCallback<PointerEnterEvent>(_ => ShowPreview(idx));

            _levelsList.Add(lb);
            _buttons.Add(lb);
        }

        if (_buttons.Count > 0)
            SelectLevel(0, _buttons[0]);
    }

    private void SelectLevel(int index, LevelButton btn)
    {
        _selectedIndex = index;
        _activeBtn?.SetActive(false);
        _activeBtn = btn;
        _activeBtn.SetActive(true);
        ShowPreview(index);
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

        if (_previewTitle != null) _previewTitle.text = def.DisplayName.ToUpper();
        if (_previewDesc != null) _previewDesc.text = def.Description;

        if (_difficultyBar != null) _difficultyBar.style.width = Length.Percent(def.Difficulty * 100f);
        if (_densityBar != null) _densityBar.style.width = Length.Percent(def.Density * 100f);
        if (_areaBar != null) _areaBar.style.width = Length.Percent(def.AreaSize * 100f);

        if (_difficultyVal != null) _difficultyVal.text = DifficultyToLabel(def.Difficulty);
        if (_densityVal != null) _densityVal.text = DifficultyToLabel(def.Density);
        if (_areaVal != null) _areaVal.text = DifficultyToLabel(def.AreaSize);
        if (_bestTimeVal != null) _bestTimeVal.text = "---";
    }

    private static string DifficultyToLabel(float v)
    {
        if (v < 0.33f) return "LOW";
        if (v < 0.66f) return "MED";
        if (v < 0.85f) return "HIGH";
        return "MAX";
    }

    private void PlayCurrent()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _levels.Count) return;
        var sceneName = _levels[_selectedIndex].SceneName;
        if (System.Enum.TryParse<Loader.Scene>(sceneName, ignoreCase: true, out var target))
            Loader.Load(target);
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
        public string Tag;
        public float Difficulty;
        public float Density;
        public float AreaSize;

        public LevelDef(string sceneName, string displayName, string description, string tag,
                        float difficulty, float density, float areaSize)
        {
            SceneName = sceneName;
            DisplayName = displayName;
            Description = description;
            Tag = tag;
            Difficulty = difficulty;
            Density = density;
            AreaSize = areaSize;
        }
    }
}
