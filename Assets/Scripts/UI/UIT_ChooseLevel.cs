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
        new LevelDef("Level_1", "Abandoned Outskirts", "Slums at dawn; lighter enemy density."),
        new LevelDef("Level_1", "Industrial Yard",     "Closer quarters, higher spawn rates."),
        new LevelDef("Level_1", "Old Town",            "Maze-like alleys, tougher elites."),
    };

    private int _current = -1;

    private void Awake()
    {
        _root = _uiDocument.rootVisualElement;

        // Optional: auto-attach style if you put it under Resources/UI/ChooseLevel.uss
        var style = Resources.Load<StyleSheet>("UI/ChooseLevel");
        if (style != null) _root.styleSheets.Add(style);

        _levelsList = _root.Q<ScrollView>("levels-list");
        _previewImage = _root.Q<Image>("preview-image");
        _previewRTSurface = _root.Q<VisualElement>("preview-rt-surface");
        _previewTitle = _root.Q<Label>("preview-title");
        _previewDesc = _root.Q<Label>("preview-desc");
        _btnPlay = _root.Q<Button>("btn_play_level");
        _btnBack = _root.Q<Button>("btn_back");

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

            var btn = new Button { name = $"btn_{def.SceneName}", text = def.DisplayName };
            btn.AddToClassList("level-btn");

            // Hover/focus to update preview
            btn.RegisterCallback<PointerEnterEvent>(_ => ShowPreview(idx));
            btn.RegisterCallback<FocusInEvent>(_ => ShowPreview(idx));

            // Click to start the level
            btn.RegisterCallback<ClickEvent>(_ => { _current = idx; PlayCurrent(); });

            _levelsList.Add(btn);
        }

        if (_levels.Count > 0) ShowPreview(0);
    }

    private void ShowPreview(int index)
    {
        if (index < 0 || index >= _levels.Count) return;
        _current = index;

        var def = _levels[index];

        // Load a Texture2D (NOT Sprite) so it can be assigned to Image.image
        var tex = Resources.Load<Texture2D>($"UI/LevelShots/{def.SceneName}");
        if (tex != null)
        {
            _previewImage.image = tex;                 // OK: Texture2D -> Texture
            _previewImage.style.display = DisplayStyle.Flex;
        }
        else
        {
            _previewImage.image = null;
            _previewImage.style.display = DisplayStyle.None;
        }

        _previewTitle.text = def.DisplayName;
        _previewDesc.text = def.Description;
    }

    private void PlayCurrent()
    {
        if (_current < 0 || _current >= _levels.Count) return;
        var sceneName = _levels[_current].SceneName;
        SceneManager.LoadScene(sceneName); // Or use your Loader helper if you prefer
    }

    private void GoBack()
    {
        // Return to previous screen
        SceneManager.LoadScene("ChooseWeapon");
    }

    // Use a simple class instead of 'record' (avoids IsExternalInit requirement)
    private class LevelDef
    {
        public string SceneName;
        public string DisplayName;
        public string Description;

        public LevelDef(string sceneName, string displayName, string description)
        {
            SceneName = sceneName;
            DisplayName = displayName;
            Description = description;
        }
    }
}
