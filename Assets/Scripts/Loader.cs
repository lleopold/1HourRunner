using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Loader
{
    public enum Scene
    {
        Level_1,
        Loading,
        ChoosePlayer,
        ChooseWeapon
    }
    private class LoadingMonoBehaviour : MonoBehaviour { }
    private static Action OnLoaderCallback;
    private static AsyncOperation loadingAsyncOperation;
    public static void Load(Scene scene)
    {
        Time.timeScale = 1;
        OnLoaderCallback = () =>
        {
            GameObject loadingGameObject = new GameObject("Loading game object");
            loadingGameObject.AddComponent<LoadingMonoBehaviour>().StartCoroutine(LoadSceneAsync(scene));
        };
        SceneManager.LoadScene(Scene.Loading.ToString());

    }
    public static void LoaderProgress()
    {
        if (OnLoaderCallback != null)
        {
            OnLoaderCallback();
            OnLoaderCallback = null;
        }
    }
    private static IEnumerator LoadSceneAsync(Scene scene)
    {
        yield return null;
        loadingAsyncOperation = SceneManager.LoadSceneAsync(scene.ToString());
        while (!loadingAsyncOperation.isDone)
        {
            yield return null;
        }
    }
    public static float GetLoaderProgress()
    {
        if (loadingAsyncOperation != null)
        {
            return loadingAsyncOperation.progress;
        }
        else
        {
            return 0f;
        }
    }
    public static void LoaderCallback()
    {
        if (OnLoaderCallback != null)
        {
            OnLoaderCallback();
            OnLoaderCallback = null;
        }
        //SceneManager.LoadScene(Scene.Loading.ToString());
    }
}
