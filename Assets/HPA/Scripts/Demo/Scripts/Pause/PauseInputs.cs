using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HP.Generics{
    public class PauseInputs : MonoBehaviour
    {
        public KeyCode[]        btnsPause = new KeyCode[2];
        ConditionsToPauseGame   conditions;
        bool                    isButtonAllowed = true;

        void Awake()
        {
            conditions = GetComponent<ConditionsToPauseGame>();
        }
        void Update()
        {
            for (var i = 0; i < btnsPause.Length; i++)
            {
                if (Input.GetKeyDown(btnsPause[i]) && isButtonAllowed)
                {
                    StartCoroutine(CheckConditionRoutine());
                }
            }
        }

        IEnumerator CheckConditionRoutine()
        {
            isButtonAllowed = false;
            conditions.isProcessDone = false;
            conditions.StartCoroutine(conditions.IsPauseAllowedRoutine());

            yield return new WaitUntil(() => conditions.isProcessDone);

           // if (conditions.isPauseAllowed)
            //{
               UpdatePause();
            //}

            isButtonAllowed = true;
            yield return null;
        }

        void UpdatePause()
        {
            PauseManager.instance.Bool_IsGamePaused = !PauseManager.instance.Bool_IsGamePaused;
            //-> Pause the game
            if (PauseManager.instance.Bool_IsGamePaused)// Check if the game is paused
            { // Check if Game page is displayed in gamplay scene
                PauseManager.instance.PauseGame(0);
            }
            //-> Unpause the game
            else
            { // Check if Menu page is displayed in gamplay scene
                PauseManager.instance.UnpauseGame(0);
            }
        }
    }

}
