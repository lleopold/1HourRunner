using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace HP.Generics
{
    public class ConditionsToPauseGame : MonoBehaviour
    {
        public List<UnityEvent> ActionsConditionsToPauseGame = new List<UnityEvent>();
        [HideInInspector]
        public bool             isProcessDone = true;
        [HideInInspector]
        public bool             isSubProcessDone = true;
        [HideInInspector]
        public bool             isPauseAllowed = false;

        public IEnumerator IsPauseAllowedRoutine()
        {
            #region
            isProcessDone = false;
            for (var i = 0; i < ActionsConditionsToPauseGame.Count; i++)
            {
                yield return new WaitUntil(() => isSubProcessDone);
                isSubProcessDone = false;
                ActionsConditionsToPauseGame[i]?.Invoke();
            }

            isProcessDone = true; 
            #endregion
        }
    }
}
