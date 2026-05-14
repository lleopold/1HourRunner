// Editor-only helper: always start Play mode from the Preload scene (index 0).
// This file lives in an Editor folder so it is never included in builds.
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class PlayFromPreload
{
    private const string PreloadScenePath = "Assets/Scenes/Preload.unity";

    static PlayFromPreload()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            // Save the currently open scene so we can return to it after Play
            EditorPrefs.SetString("PlayFromPreload_PreviousScene", EditorSceneManager.GetActiveScene().path);

            // If we are NOT already in Preload, switch to it before Play starts
            if (EditorSceneManager.GetActiveScene().path != PreloadScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    // User cancelled — abort Play
                    EditorApplication.isPlaying = false;
                    return;
                }
                EditorSceneManager.OpenScene(PreloadScenePath);
            }
        }

        if (state == PlayModeStateChange.EnteredEditMode)
        {
            // Restore the scene the developer was working on
            string prev = EditorPrefs.GetString("PlayFromPreload_PreviousScene", "");
            if (!string.IsNullOrEmpty(prev) && prev != PreloadScenePath)
                EditorSceneManager.OpenScene(prev);
        }
    }
}
#endif
