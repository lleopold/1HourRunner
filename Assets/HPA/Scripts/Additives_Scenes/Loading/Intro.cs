using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HP.Generics
{
    public class Intro : MonoBehaviour
    {
        public string scenName;

        void Start()
        {
            LoadAsyncScenes();
        }

        public void LoadAsyncScenes()
        {
            StartCoroutine(LoadAsyncScenesRoutine());
        }

        IEnumerator LoadAsyncScenesRoutine()
        {
            #region
            yield return new WaitForSeconds(2);
            AsyncOperation asyncLoad;
            asyncLoad = SceneManager.LoadSceneAsync(scenName);
            while (!asyncLoad.isDone) { yield return null; }
            yield return null;
            #endregion
        }
    }
}

