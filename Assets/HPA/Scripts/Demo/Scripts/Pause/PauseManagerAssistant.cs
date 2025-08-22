using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HP.Generics
{
    public class PauseManagerAssistant : MonoBehaviour
    {

        public void DisableCharacterMovement()
        {
            #region
            TSCharacterTag[] tSCharacters = FindObjectsOfType<TSCharacterTag>();

            for(var i = 0;i< tSCharacters.Length; i++)
            {
                if (tSCharacters[i].GetComponent<Rigidbody>())
                    tSCharacters[i].GetComponent<Rigidbody>().isKinematic = true;
                if (tSCharacters[i].GetComponent<characterMovement>())
                    tSCharacters[i].GetComponent<characterMovement>().isMovementAllowed = false;
            }
            #endregion
        }

        public void EnableCharacterMovement()
        {
            #region
            TSCharacterTag[] tSCharacters = FindObjectsOfType<TSCharacterTag>();

            for (var i = 0; i < tSCharacters.Length; i++)
            {
                if (tSCharacters[i].GetComponent<Rigidbody>())
                    tSCharacters[i].GetComponent<Rigidbody>().isKinematic = false;
                if (tSCharacters[i].GetComponent<characterMovement>())
                    tSCharacters[i].GetComponent<characterMovement>().isMovementAllowed = true;
            }
            #endregion
        }

        public void UnlockCursor()
        {
            #region 
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true; 
            #endregion
        }
        public void LockCursor()
        {
            #region
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false; 
            #endregion
        }

    }

}
