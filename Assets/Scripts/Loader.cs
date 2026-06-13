using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Loader
{
    public enum Scene
    {
        Level_1,
        Level_2,
        Loading,
        ChoosePlayer,
        ChooseWeapon,
        ChooseLevel
    }
    private class LoadingMonoBehaviour : MonoBehaviour { }
    private static Action OnLoaderCallback;
    private static AsyncOperation loadingAsyncOperation;
    public static void Load(Scene scene)
    {
        Time.timeScale = 1;
        DataHolder.ResetStats();
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
    public static bool AllowActivation { get; set; } = false;

    private static IEnumerator LoadSceneAsync(Scene scene)
    {
        AllowActivation = false;
        yield return null;
        loadingAsyncOperation = SceneManager.LoadSceneAsync(scene.ToString());
        loadingAsyncOperation.allowSceneActivation = false;

        // Wait until Unity has fully loaded the scene data (progress reaches 0.9)
        while (loadingAsyncOperation.progress < 0.9f)
        {
            yield return null;
        }

        // Wait for UITLoadingScreen to signal the bar animation is done
        while (!AllowActivation)
        {
            yield return null;
        }

        loadingAsyncOperation.allowSceneActivation = true;
    }

    public static float GetLoaderProgress()
    {
        if (loadingAsyncOperation == null) return 0f;
        // progress maxes at 0.9 until allowSceneActivation; remap 0–0.9 → 0–1
        return Mathf.Clamp01(loadingAsyncOperation.progress / 0.9f);
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
