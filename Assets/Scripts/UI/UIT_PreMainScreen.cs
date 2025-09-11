using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class UIT_PreMainScreen : MonoBehaviour
{
    [SerializeField] private UIDocument _uiDocument;
    private VisualElement _root;
    private Button _btn_play, _btn_language, _btn_options, _btn_quit;

    void OnEnable()
    {
        _root = _uiDocument.rootVisualElement;

        _btn_play = _root.Q<Button>("btn_play");
        _btn_language = _root.Q<Button>("btn_language");
        _btn_options = _root.Q<Button>("btn_options");
        _btn_quit = _root.Q<Button>("btn_quit");

        // Use Button.clicked -> works with mouse, keyboard Submit, and gamepad South (A)
        _btn_play.clicked += () => SceneManager.LoadScene("ChoosePlayer");
        _btn_language.clicked += () => { /* SceneManager.LoadScene("LanguageScreen"); */ };
        _btn_options.clicked += () => { /* SceneManager.LoadScene("OptionsScreen");  */ };
        _btn_quit.clicked += Application.Quit;

        // Give focus so D-pad/Left Stick navigate and Submit activates
        _root.schedule.Execute(() => _btn_play.Focus());
    }

    void OnDisable()
    {
        // Unsubscribe to avoid duplicate bindings if the document is re-enabled
        if (_btn_play != null) _btn_play.clicked -= () => SceneManager.LoadScene("ChoosePlayer");
        if (_btn_language != null) _btn_language.clicked -= () => { };
        if (_btn_options != null) _btn_options.clicked -= () => { };
        if (_btn_quit != null) _btn_quit.clicked -= Application.Quit;
    }
}
