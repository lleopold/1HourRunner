using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class UIT_EndGamePopUp : MonoBehaviour
{
    [SerializeField] private UIDocument _uiDocument;
    public VisualElement _root { get; set; }
    Label _game_epilog;
    Label _time_passed;
    Label _enemies_defeated;
    Label _expirience_earned;
    Button _btn_ok;


    private void OnEnable()
    {
        _root = _uiDocument.rootVisualElement;
        _game_epilog = _root.Q<Label>("lab_game_epilog");
        _time_passed = _root.Q<Label>("lab_time_passed");
        _enemies_defeated = _root.Q<Label>("lab_enemies_defeated");
        _expirience_earned = _root.Q<Label>("lab_expirience_earned");
        _btn_ok = _root.Q<Button>("btn_ok");
        _btn_ok.clickable.clicked += GoToMainScreen;
    }

    private void GoToMainScreen()
    {
        if (GameObject.Find("Player") != null)
        {
            Destroy(GameObject.Find("Player"));
        }
        SceneManager.LoadScene("ChoosePlayer");
    }

    public void SetEndGamePopUp(string game_epilog, string time_passed, string enemies_defeated, string expirience_earned)
    {
        _game_epilog.text = game_epilog;
        _time_passed.text = time_passed;
        _enemies_defeated.text = enemies_defeated;
        _expirience_earned.text = expirience_earned;
    }
}
