using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HP.Generics
{
    public class InitManagerExample : MonoBehaviour, HP.Generics.IInitable
    {
        public bool toggle = false;

        public void DisplayTextWhenSceneStart()
        {
            Debug.Log("Gameplay scene starts");
        }

        public void DoSomethingWhenSceneIsInitialized()
        {
            Debug.Log("Gameplay scene is initialized");
        }

        public bool IsInitDone() { 
            // Do something to check
            // if this object is initialized

            // Return the current value
            // of the boolean that give the
            // info the object is initialized

            // In this example it returns the value of toggle variable.
            // In your case return the value used to keep track
            // of the initialization state of your object
            return toggle; 
        }
    }

}
