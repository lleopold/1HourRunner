using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HP.Generics
{
    public class GoToMainMenu : MonoBehaviour
    {
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.P))
                GetComponent<HP.Generics.LoadAScene>().LoadASceneAsync("MainMenu");
        }
    }

}
