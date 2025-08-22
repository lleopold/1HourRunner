using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


namespace HP.Generics
{
    public class InitManager : MonoBehaviour
    {
        public bool             autoStart = true;

        public List<UnityEvent> ActionWhenSceneStarts = new List<UnityEvent>();
        [Space]
        public List<GameObject> waitUntilList = new List<GameObject>();
        [Space]
        public List<UnityEvent> ActionWhenInitProcessDone = new List<UnityEvent>();

        List<IInitable>         interfaceList = new List<IInitable>();


        void Start()
        {
            #region
            if (autoStart)
                SceneStarts(); 
            #endregion
        }

        public void SceneStarts()
        {
            #region Do something when scene starts
            for (var i = 0; i < waitUntilList.Count; i++)
            {
                if (waitUntilList[i])
                    interfaceList.Add(waitUntilList[i].GetComponent<IInitable>());
            }

            for (var i = 0; i < ActionWhenSceneStarts.Count; i++)
                ActionWhenSceneStarts[i].Invoke();

            StartCoroutine(WaitUntilSceneIsInitialized()); 
            #endregion
        }

        IEnumerator WaitUntilSceneIsInitialized()
        {
            #region Wait until all the tested methods returned true
            bool allConditionsTrue = false;

            while (!allConditionsTrue)
            {
                allConditionsTrue = true;

                for (var i = 0; i < interfaceList.Count; i++)
                {
                    if (!interfaceList[i].IsInitDone())
                    {
                        allConditionsTrue = false;
                        break;
                    }
                }

                yield return null;
            }

            StartCoroutine(InitProcessDoneRoutine());

            yield return null; 
            #endregion
        }

        IEnumerator InitProcessDoneRoutine()
        {
            #region Do something after initialization
            for (var i = 0; i < ActionWhenInitProcessDone.Count; i++)
                ActionWhenInitProcessDone[i].Invoke();

            yield return null; 
            #endregion
        }
    }
}