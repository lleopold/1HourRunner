// Description: TSLightOptiInit. Init all the objects using TSLightOpti script.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace HP.Generics
{
    public class TSLightOptiInit : MonoBehaviour
    {
        public UnityEvent   WaitForPlayerEvent;

        bool                wait = true;

        void Start()
        {
            #region 
            StartCoroutine(InitRoutine()); 
            #endregion
        }

        IEnumerator InitRoutine()
        {
            #region
            while (wait)
            {
                WaitForPlayerEvent?.Invoke();
                yield return null;
            }

            TSLightOpti[] targets = FindObjectsOfType<TSLightOpti>();
           // Debug.Log("TSLightOpti: " + targets.Length);
            foreach (TSLightOpti target in targets)
                target.StartCoroutine(target.InitRoutine());

            yield return null; 
            #endregion
        }

        public void WaitForPlayer()
        {
            #region 
            wait = false; 
            #endregion
        }

        public void WaitForTwoPlayers()
        {
            #region
            TSCharacterTag[] targets = FindObjectsOfType<TSCharacterTag>();
            if (targets.Length == 2)
                wait = false; 
            #endregion
        }
       
    }

}
