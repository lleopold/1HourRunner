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

        // Use named methods instead of lambdas for reliable unsubscription
        if (_btn_play != null) _btn_play.clicked += OnPlayClicked;
        if (_btn_language != null) _btn_language.clicked += OnLanguageClicked;
        if (_btn_options != null) _btn_options.clicked += OnOptionsClicked;
        if (_btn_quit != null) _btn_quit.clicked += OnQuitClicked;

        // Focus the first button for controller/keyboard support
        _root.schedule.Execute(() => _btn_play?.Focus());
    }

    void OnDisable()
    {
        // Unsubscribe to prevent memory leaks and duplicate triggers
        if (_btn_play != null) _btn_play.clicked -= OnPlayClicked;
        if (_btn_language != null) _btn_language.clicked -= OnLanguageClicked;
        if (_btn_options != null) _btn_options.clicked -= OnOptionsClicked;
        if (_btn_quit != null) _btn_quit.clicked -= OnQuitClicked;
    }

    private void OnPlayClicked() => SceneManager.LoadScene("ChoosePlayer");
    private void OnLanguageClicked() { /* Add language logic here */ }
    private void OnOptionsClicked() { /* Add options logic here */ }

    private void OnQuitClicked()
    {
        Debug.Log("Quit button clicked!");
#if UNITY_EDITOR
        // This stops the game while running in the Unity Editor
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // This closes the application in a build
        Application.Quit();
#endif
    }
}