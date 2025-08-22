using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

public class UIT_LevelUp : MonoBehaviour
{
    private Button _btn_powerUp1;
    private Button _btn_powerUp2;
    private Button _btn_powerUp3;
    private Button _btn_ok;
    private Button _btn_cancel;

    [SerializeField] private UIDocument _uiDocument;
    public VisualElement _root { get; set; }
    public GameObject _player { get; set; }


    private void OnEnable()
    {
        _root = _uiDocument.rootVisualElement;
        _btn_powerUp1 = _root.Q<Button>("btn_pwr1");
        _btn_powerUp2 = _root.Q<Button>("btn_pwr2");
        _btn_powerUp3 = _root.Q<Button>("btn_pwr3");
        _btn_ok = _root.Q<Button>("btn_ok");
        _btn_cancel = _root.Q<Button>("btn_cancel");


        _btn_powerUp1.text = "PowerUp1";
        _btn_powerUp2.text = "PowerUp2";
        _btn_powerUp3.text = "PowerUp3";

        _btn_powerUp1.clickable.clicked += UsePowerUp1;
        _btn_powerUp2.clickable.clicked += UsePowerUp2;
        _btn_powerUp3.clickable.clicked += UsePowerUp3;

        _btn_ok.clickable.clicked += LogError;
        _btn_cancel.clickable.clicked += LogError;
    }

    private void LogError()
    {
        Debug.LogError("OK/CANCEL");
    }

    private void LateUpdate()
    {
        if (_player == null)
        {
            _player = GameObject.Find("Player");
        }
        //_btn_powerUp1.RegisterCallback<ClickEvent>(ev => UsePowerUp1());
        //_btn_powerUp2.RegisterCallback<ClickEvent>(ev => UsePowerUp1());
        //_btn_powerUp3.RegisterCallback<ClickEvent>(ev => UsePowerUp1());

    }

    private void UsePowerUp3()
    {
        Debug.LogError("PowerUp3");
        if (_player == null)
        {
            _player = GameObject.Find("Player");
        }
        _player.GetComponent<PlayerControllerInput>().PauseGame(false);
        //_player.GetComponent<PlayerControllerInput>().HidePowerUpScreen();
        Destroy(gameObject);
    }

    private void UsePowerUp2()
    {
        Debug.LogError("PowerUp2");
        if (_player == null)
        {
            _player = GameObject.Find("Player");
        }
        _player.GetComponent<PlayerControllerInput>().PauseGame(false);
        //_player.GetComponent<PlayerControllerInput>().HidePowerUpScreen();
        Destroy(gameObject);
    }

    private void UsePowerUp1()
    {
        Debug.LogError("PowerUp1");
        if (_player == null)
        {
            _player = GameObject.Find("Player");
        }
        _player.GetComponent<PlayerControllerInput>().PauseGame(false);
        //_player.GetComponent<PlayerControllerInput>().HidePowerUpScreen();
        Destroy(gameObject);
    }

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
