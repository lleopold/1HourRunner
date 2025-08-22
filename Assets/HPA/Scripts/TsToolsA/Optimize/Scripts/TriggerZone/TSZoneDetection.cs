// Description: TSZoneDetection. Allows calling methods when the player enter and exit a trigger (Collider) 
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace HP.Generics
{
    public class TSZoneDetection : MonoBehaviour
    {
        public UnityEvent ActionOnColliderEnter;
        public UnityEvent ActionOnColliderExit;

        private void OnTriggerEnter(Collider other)
        {
            #region
            if (other.GetComponent<TSCharacterTag>())
                ActionOnColliderEnter.Invoke();
            #endregion
        }

        private void OnTriggerExit(Collider other)
        {
            #region
            if (other.GetComponent<TSCharacterTag>())
                ActionOnColliderExit.Invoke();
            #endregion
        }
    }

}
