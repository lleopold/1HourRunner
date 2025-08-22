using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class UIT_PreMainScreen : MonoBehaviour
{
    [SerializeField] private UIDocument _uiDocument;
    private VisualElement _root;
    private Button _btn_play;
    private Button _btn_language;
    private Button _btn_options;
    private Button _btn_quit;

    private void OnEnable()
    {
        _root = _uiDocument.rootVisualElement;
        _btn_play = _root.Q<Button>("btn_play");
        _btn_language = _root.Q<Button>("btn_language");
        _btn_options = _root.Q<Button>("btn_options");
        _btn_quit = _root.Q<Button>("btn_quit");

        _btn_play.RegisterCallback<ClickEvent>(GoToGameScreen);
        _btn_language.RegisterCallback<ClickEvent>(GoToLanguageScreen);
        _btn_options.RegisterCallback<ClickEvent>(GoToOptionsScreen);
        _btn_quit.RegisterCallback<ClickEvent>(QuitApplication);
    }

    private void GoToGameScreen(ClickEvent evt)
    {
        SceneManager.LoadScene("ChoosePlayer");
    }
    private void GoToLanguageScreen(ClickEvent evt)
    {
        //SceneManager.LoadScene("LanguageScreen");
    }
    private void GoToOptionsScreen(ClickEvent evt)
    {
        //SceneManager.LoadScene("OptionsScreen");
    }
    private void QuitApplication(ClickEvent evt)
    {
        Application.Quit();
    }

}
