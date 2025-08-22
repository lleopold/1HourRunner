// Description: SpawnSystem. Allows to teleport the player from a position to another position in the scene.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace HP.Generics
{
    public class SpawnSystem : MonoBehaviour,IValidateAction<bool>
    {
        public bool                 isInitializedWhenSceneStarts = true;
        public bool                 checkInput = true;
        public Transform            Chara;

        [HideInInspector]
        public bool                 isInitDone = false;

        public KeyCode              nextDestinationKey = KeyCode.N;

        [System.Serializable]
        public class SpawnPosParam
        {
            public string       spawnName = "Name";
            public Transform    spawnPos;
            public KeyCode      key;
            public bool         alreadyVisited = false;
            //public string       locationName = "New Location";
        }

        public List<SpawnPosParam>  spawnList = new List<SpawnPosParam>();
       
        [HideInInspector]
        public bool                 isNewSpawnPosInProgress = false;

        public List<UnityEvent>     ActionsWhenProcessStarts = new List<UnityEvent>();
        public List<UnityEvent>     ActionsWhenProcessEnded = new List<UnityEvent>();
        
        [HideInInspector]
        public bool                 isActionProcessDone = false;

        [HideInInspector]
        public int                  currentSpawnID = 0;

        private void Start()
        {
            #region
            if (isInitializedWhenSceneStarts)
                StartCoroutine(InitSpawnSystemRoutine());
            #endregion
        }

        public void InitSpawnSystem()
        {
            #region
            StartCoroutine(InitSpawnSystemRoutine());
            #endregion
        }

        public IEnumerator InitSpawnSystemRoutine()
        {
            #region
            if (Chara ==  null)
            {
                TSCharacterTag target = FindObjectOfType<TSCharacterTag>();
                yield return new WaitUntil(() => target);
                Chara = target.transform;
            }
           
            StartCoroutine(SpawnRoutine(0));
            yield return null;
            #endregion
        }

        void Update()
        {
            #region
            if (checkInput)
                CheckInput();
            #endregion
        }

        public void CheckInput()
        {
            #region
            if (!isNewSpawnPosInProgress && isInitDone)
            {
                for (var i = 0; i < spawnList.Count; i++)
                {
                    if (Input.GetKeyDown(spawnList[i].key) && i != currentSpawnID && spawnList[i].key != KeyCode.None)
                    {
                        GoToNewSpawnPosition(i);
                        break;
                    }


                }
                if (Input.GetKeyDown(nextDestinationKey))
                {
                    GoToNextDestination();
                }
            }
            #endregion
        }

        public void GoToNewSpawnPosition(int spawnID)
        {
            #region
            if (!isNewSpawnPosInProgress && isInitDone)
                StartCoroutine(SpawnRoutine(spawnID));
            #endregion
        }

        IEnumerator SpawnRoutine(int spawnID)
        {
            #region
            isNewSpawnPosInProgress = true;

            currentSpawnID = spawnID;

            spawnList[currentSpawnID].alreadyVisited = true;

            TSOptiGrid.instance.isInitDone = false;

           // Debug.Log("End 0: " + currentSpawnID);
            for (var i = 0;i< ActionsWhenProcessStarts.Count; i++)
            {
                isActionProcessDone = false;
                ActionsWhenProcessStarts[i].Invoke();

                yield return new WaitUntil(() => isActionProcessDone);
            }

            yield return new WaitUntil(() => Chara.transform.position == spawnList[currentSpawnID].spawnPos.position);
            yield return new WaitUntil(() => Chara.transform.rotation == spawnList[currentSpawnID].spawnPos.rotation);


            yield return new WaitUntil(() => TSOptiGrid.instance.ForceOptimizationGridUpdate());
            yield return new WaitUntil(() => TSOptiGrid.instance.objsDistanceList.Count == 0);



            for (var i = 0; i < ActionsWhenProcessEnded.Count; i++)
            {
                isActionProcessDone = false;
                ActionsWhenProcessEnded[i].Invoke();

                yield return new WaitUntil(() => isActionProcessDone);
            }

            isNewSpawnPosInProgress = false;


            isInitDone = true;
            yield return null;
            #endregion
        }

        public void ValidateAction(bool actionState)
        {
            #region
            isActionProcessDone = true;
            #endregion
        }

        public void GoToNextDestination()
        {
            #region 
            bool newDestinationAvailable = false;
            while (!newDestinationAvailable)
            {
                currentSpawnID++;
                currentSpawnID %= spawnList.Count;

                if (currentSpawnID == 0)
                {
                    for (var i = 0; i < spawnList.Count; i++)
                    {
                        spawnList[i].alreadyVisited = false;
                    }
                }

                if (!spawnList[currentSpawnID].alreadyVisited)
                    newDestinationAvailable = true;
            }

            GoToNewSpawnPosition(currentSpawnID); 
            #endregion
        }
    }
}
