// Description : w_CalculateLightmapHelper: Calculate lightmap easily for additive scenes.
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace HP.Generics
{
    public class w_CalculateLightmapHelper : EditorWindow
    {
        private Vector2 scrollPosAll;

        CalculateLightmapData data;
        SerializedObject serializedObject;
        SerializedProperty _helpBox;
        SerializedProperty _moreOptions;
        SerializedProperty _projetTabScenePath;
        SerializedProperty _sceneThatContainsLight;
        SerializedProperty _sceneAutoSave;
        SerializedProperty _lightName;
        SerializedProperty _sceneToCalculateList;
        SerializedProperty _otherSceneToOpenList;
        public static bool isProcessDone = true;

        [MenuItem("Tools/HP/Calculate Lightmaps")]
        public static void ShowWindow()
        {
            #region
            //Show existing window instance. If one doesn't exist, make one.
            EditorWindow.GetWindow(typeof(w_CalculateLightmapHelper));
            #endregion
        }

        void OnEnable()
        {
            #region
            EnableCallback();
            string objectPath = "Assets/HPA/Scripts/Additives_Scenes/Calculate_Lightmaps/CalculateLightmapData.asset";
            data = AssetDatabase.LoadAssetAtPath(objectPath, typeof(UnityEngine.Object)) as CalculateLightmapData;

            if (data)
            {
                serializedObject        = new UnityEditor.SerializedObject(data);
                _helpBox                = serializedObject.FindProperty("HelpBox");
                _moreOptions            = serializedObject.FindProperty("MoreOptions");
                _projetTabScenePath     = serializedObject.FindProperty("projetTabScenePath");
                _sceneThatContainsLight = serializedObject.FindProperty("sceneThatContainsLight");
                _sceneAutoSave          = serializedObject.FindProperty("sceneAutoSave");
                _lightName              = serializedObject.FindProperty("lightName");
                _sceneToCalculateList   = serializedObject.FindProperty("sceneToCalculateList");
                _otherSceneToOpenList   = serializedObject.FindProperty("otherSceneToOpenList");

                serializedObject.Update();
                serializedObject.ApplyModifiedProperties();
            }
            #endregion
        }

        private void OnDisable()
        {
            #region
            DisableCallback();
            #endregion
        }

        void EnableCallback()
        {
            #region
            EditorSceneManager.sceneOpened += OnEditorSceneManagerSceneOpened;
            EditorSceneManager.sceneClosed += OnEditorSceneManagerSceneClosed;
            EditorSceneManager.sceneSaved += OnEditorSceneManagerSceneSaved;
            Lightmapping.bakeCompleted += OnEditorBakeCompleted;
            #endregion
        }

        void DisableCallback()
        {
            #region
            EditorSceneManager.sceneOpened -= OnEditorSceneManagerSceneOpened;
            EditorSceneManager.sceneClosed -= OnEditorSceneManagerSceneClosed;
            EditorSceneManager.sceneSaved -= OnEditorSceneManagerSceneSaved;
            Lightmapping.bakeCompleted -= OnEditorBakeCompleted;
            #endregion
        }

        void OnEditorSceneManagerSceneSaved(Scene scene)
        {
            #region 
            Debug.Log("Scene Saved");
            isProcessDone = true;
            #endregion
        }

        void OnEditorSceneManagerSceneOpened(Scene scene, OpenSceneMode mode)
        {
            #region
            Debug.Log("Scene Opened");
            isProcessDone = true;
            #endregion
        }

        void OnEditorSceneManagerSceneClosed(Scene scene)
        {
            #region 
            Debug.Log("Scene Closed");
            isProcessDone = true;
            #endregion
        }

        void OnEditorBakeCompleted()
        {
            Debug.Log("Bake Complete");
            RemoveTemporarySun();
            //isProcessDone = true;
        }

        void OnGUI()
        {
            #region
            scrollPosAll = EditorGUILayout.BeginScrollView(scrollPosAll);
            EditorGUILayout.LabelField("");
            serializedObject.Update();
            if (data)
            {
                
                if (!Lightmapping.isRunning)
                {
                    DisplayInfo();

                    EditorGUILayout.LabelField("");
                  
                    StartCalculationButtonList();

                    EditorGUILayout.LabelField("");
                    EditorGUILayout.LabelField("");
                    OpenAllSceneButton();
                }
                else
                {
                    //EditorGUILayout.HelpBox("Inspector Padlock is automatically locked during Batch Process", MessageType.Warning);
                      if (GUILayout.Button("Cancel"))
                      {
                        CancelProcess();
                      }
                }



            }
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.EndScrollView();
            #endregion
        }

        void DisplayInfo()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Scenes Path:", GUILayout.Width(140));
            EditorGUILayout.PropertyField(_projetTabScenePath, new GUIContent(""), GUILayout.MinWidth(120));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Sunlight scene name:", GUILayout.Width(140));
            EditorGUILayout.PropertyField(_sceneThatContainsLight, new GUIContent(""), GUILayout.MinWidth(120));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Sunlight object name:", GUILayout.Width(140));
            EditorGUILayout.PropertyField(_lightName, new GUIContent(""), GUILayout.MinWidth(120));
            EditorGUILayout.EndHorizontal();


            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Scene are Auto Saved:", GUILayout.Width(140));
            EditorGUILayout.PropertyField(_sceneAutoSave, new GUIContent(""));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("List of scenes:", GUILayout.Width(140));
            EditorGUILayout.PropertyField(_sceneToCalculateList, new GUIContent(""), GUILayout.MinWidth(120));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Other scenes:", GUILayout.Width(140));
            EditorGUILayout.PropertyField(_otherSceneToOpenList, new GUIContent(""), GUILayout.MinWidth(120));
            EditorGUILayout.EndHorizontal();
        }


        void StartCalculationButtonList()
        {
            EditorGUILayout.LabelField("Generate Lightmap for:");

            for (var i =0;i< _sceneToCalculateList.arraySize; i++)
            {
                string name = _sceneToCalculateList.GetArrayElementAtIndex(i).stringValue;
                if (GUILayout.Button(name))
                {
                    //Debug.Log("IsGamePlaySceneInTheHierarchy: " + IsThisSceneInTheHierarchy(_sceneThatContainsLight.stringValue));
                    CalculateLightmap(name);

                }
               
            }
        }

        void CalculateLightmap(string sceneToCalculate)
        {
            // Step 1: Load the scene with the sunlight
            var path = "";
            if (!IsThisSceneInTheHierarchy(_sceneThatContainsLight.stringValue))
            {
                path = _projetTabScenePath.stringValue + _sceneThatContainsLight.stringValue + ".unity";
                EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                while (!isProcessDone) { }
                isProcessDone = false;
            }
            else
            {
                Debug.Log(_sceneThatContainsLight.stringValue + " already exist in the Hierarchy");
            }


            // Step 2: Save and Unload all the scenes except the scene with the sunlight
            List<string> sceneInTheHierarchy = new List<string>();
            for (var i = 0; i < EditorSceneManager.sceneCount; i++)
                if (EditorSceneManager.GetSceneAt(i).name != _sceneThatContainsLight.stringValue)
                    sceneInTheHierarchy.Add(EditorSceneManager.GetSceneAt(i).name);


            for (var i = 0; i < sceneInTheHierarchy.Count; i++)
                Debug.Log(sceneInTheHierarchy[i]);

            for (var i = 0; i < sceneInTheHierarchy.Count; i++)
            {
                if (_sceneAutoSave.boolValue)
                {
                    EditorSceneManager.SaveScene(EditorSceneManager.GetSceneByName(sceneInTheHierarchy[i]));
                    while (!isProcessDone) { }
                    isProcessDone = false;
                }

                EditorSceneManager.CloseScene(SceneManager.GetSceneByName(sceneInTheHierarchy[i]), true);

                while (!isProcessDone) { }
                isProcessDone = false;
            }

            // Step 3: Load The scene to calculate 
            path = _projetTabScenePath.stringValue + sceneToCalculate + ".unity";
            EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            while (!isProcessDone) { }
            isProcessDone = false;
            EditorSceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneToCalculate));

            while (EditorSceneManager.GetActiveScene() != SceneManager.GetSceneByName(sceneToCalculate)) { }


            // Step 4: Move The sun inside the scene to calculate
            Light[] lights = GameObject.FindObjectsByType<Light>(FindObjectsSortMode.None);
            GameObject sunParent = new GameObject();
            sunParent.name = "SunParent";

            foreach(Light light in lights)
            {
                if(light.name == _lightName.stringValue)
                {
                    light.transform.SetParent(sunParent.transform);
                }
            }

            // Step 5: Close the scene containing the sun
            EditorSceneManager.CloseScene(SceneManager.GetSceneByName(_sceneThatContainsLight.stringValue), true);

            while (!isProcessDone) { }
            isProcessDone = false;

            // Step 6: Calculate Lightmap
            BakeScene();
       /*     while (!isProcessDone) { }
            isProcessDone = false;


            // Step 7: Remove Sun
            DestroyImmediate(sunParent.gameObject);*/
        }

        void BakeScene()
        {
            #region 
            Lightmapping.BakeAsync();
            #endregion
        }

        void RemoveTemporarySun()
        {
            Light[] lights = GameObject.FindObjectsByType<Light>(FindObjectsSortMode.None);
           // GameObject sunParent = new GameObject();
           // sunParent.name = "SunParent";

            foreach (Light light in lights)
            {
                if (light.name == _lightName.stringValue)
                {
                     DestroyImmediate(light.transform.parent.gameObject);
                    break;
                }
            }

           

        }

        bool IsThisSceneInTheHierarchy( string sceneName)
        {
            #region
            for (var i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                if (EditorSceneManager.GetSceneAt(i).name == sceneName)
                    return true;
            }
            return false; 
            #endregion
        }

        void CancelProcess()
        {
            Lightmapping.Cancel();
            RemoveTemporarySun();
        }

        void OpenAllSceneButton()
        {
            if (GUILayout.Button("Open All Scene"))
            {
                var path = "";
                var sceneName = "";

                sceneName = _sceneThatContainsLight.stringValue;
                path = _projetTabScenePath.stringValue + sceneName + ".unity";
                EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                while (!isProcessDone) { }
                isProcessDone = false;


                for (var i = 0; i < _sceneToCalculateList.arraySize; i++)
                {
                    sceneName = _sceneToCalculateList.GetArrayElementAtIndex(i).stringValue;
                    path = _projetTabScenePath.stringValue + sceneName + ".unity";
                    EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                    while (!isProcessDone) { }
                    isProcessDone = false;
                }

                for (var i = 0; i < _otherSceneToOpenList.arraySize; i++)
                {
                    sceneName = _otherSceneToOpenList.GetArrayElementAtIndex(i).stringValue;
                    path = _projetTabScenePath.stringValue + sceneName + ".unity";
                    EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                    while (!isProcessDone) { }
                    isProcessDone = false;
                }
            }
           
        }

    }
}

#endif

