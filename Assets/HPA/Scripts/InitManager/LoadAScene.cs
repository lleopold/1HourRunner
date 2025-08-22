using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HP.Generics
{
    public class LoadAScene : MonoBehaviour
    {
       
        public void LoadASceneAsync(string name)
        {
            StartCoroutine(LoadAsyncSceneRoutine(name));
        }

        IEnumerator LoadAsyncSceneRoutine(string name)
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(name);

            while (!asyncLoad.isDone)
            {
                yield return null;
            } 
        }
    }

}
