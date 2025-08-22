using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HP.Generics
{
    public class ConditionsToPauseGameAssistant : MonoBehaviour
    {
        public void CheckSpawnSystem(ConditionsToPauseGame conditions)
        {
            #region 
            // Check the state of something. In this example check if the player is spawning to a new location on the map.
            SpawnSystem spawnSystem = FindObjectOfType<SpawnSystem>();
            bool state = !spawnSystem.isNewSpawnPosInProgress;


            // Update the state of isPauseAllowed in ConditionsToPauseGame script
            conditions.isPauseAllowed = state;

            // End all condition methods with the following line
            conditions.isSubProcessDone = true; 
            #endregion
        }
    }

}
