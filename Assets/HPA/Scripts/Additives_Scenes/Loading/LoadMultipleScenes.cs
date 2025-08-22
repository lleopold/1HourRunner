// Description: LoadMultipleScenes. This script allows to load multiple scenes.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HP.Generics
{
    public class LoadMultipleScenes : MonoBehaviour
    {
        public bool             initAuto = true;

        public List<string>     scenesList = new List<string>();

        public string           loadScene = "";

        public Camera           camToDelete;

        public GameObject       loadingObj;

        [HideInInspector]
        public bool             isLoading = false;

        [Space]
        [Space]
        public UnityEvent       doSomethingAtTheEndOfProcessEvent;

        public Text             txtLoading;
        public Image            imLoading;


        void Start()
        {
            #region
            if (initAuto)
                LoadAsyncScenes();

            LightProbes.tetrahedralizationCompleted += OnTetrahedralization;


            #endregion
        }
        //{}
        void OnTetrahedralization(){
        }
        public void LoadAsyncScenes()
        {
            #region 
            StartCoroutine(LoadAsyncScenesRoutine()); 
            #endregion
        }
        IEnumerator LoadAsyncScenesRoutine()
        {
         
            #region
            isLoading = true;

            if (loadingObj)
            {
                loadingObj.SetActive(true);
                yield return new WaitUntil(() => loadingObj.activeSelf);
            }

            for (var i = 0; i < scenesList.Count; i++)
            {
                AsyncOperation asyncLoad;
                asyncLoad = SceneManager.LoadSceneAsync(scenesList[i], LoadSceneMode.Additive);
                while (!asyncLoad.isDone) 
                {
                    LoadingBar(i, asyncLoad);
                    yield return null; 
                }

                //Debug.Log(i + ": Loading Scene " + scenesList[i] + " is finished");
                DetectCamera();

                yield return new WaitForEndOfFrame();
            }


            if (loadingObj)
            {
                loadingObj.SetActive(false);
                yield return new WaitUntil(() => !loadingObj.activeSelf);
            }

            isLoading = false;

            SceneManager.SetActiveScene(SceneManager.GetSceneByName(scenesList[scenesList.Count - 1]));

            doSomethingAtTheEndOfProcessEvent?.Invoke();
            
         
            AsyncOperation asyncUnLoad;
            asyncUnLoad = SceneManager.UnloadSceneAsync(loadScene);

           
         
            


            

            yield return null;
            #endregion
        }
        void DetectCamera()
        {
            #region
            Camera[] allCam = GameObject.FindObjectsOfType<Camera>();

            foreach (Camera cam in allCam)
            {
                if (cam != camToDelete)
                {
                    camToDelete.gameObject.SetActive(false);
                    break;
                }
            }
            #endregion
        }
        public void DoSomethingAtTheEndOfTheProcess()
        {
            #region
            Debug.Log("Do something when the loaded process ended.");
            #endregion
        }


        public void LoadingBar(int i, AsyncOperation asyncLoad)
        {
            int totalLoadLength = 100;
            int oneLoadSection = totalLoadLength / scenesList.Count;

            float progression = oneLoadSection * i + oneLoadSection * asyncLoad.progress;
            progression = Mathf.Round(progression);

            if (txtLoading)
            {
                txtLoading.text = progression + "%";
            }

            if (imLoading)
            {
                imLoading.fillAmount = progression * .01f;
            }

            //Debug.Log((i + 1) + ": " + progression + "%");
        }
    }
}