//Description: TerrainStarterEditor: Custom Editor
#if (UNITY_EDITOR)
using UnityEngine;
using UnityEditor;

using System.Collections.Generic;

namespace HP.Generics
{
    [CustomEditor(typeof(TerrainStarter))]
    public class TerrainStarterEditor : Editor
    {
        SerializedProperty SeeInspector;                                            // use to draw default Inspector
        SerializedProperty m_terrList;
        SerializedProperty m_editorTabIndex;
        SerializedProperty m_layerParamsList;
        SerializedProperty m_detailListRef;
        SerializedProperty m_terrainSettings;
        SerializedProperty m_treeSettings;

        private bool isProcessDone = true;

        string[] tabs = { "Terrain Settings", "Terrain Layer", "Terrain Detail", "Terrain Tree" };

        void OnEnable()
        {
            #region
            // Setup the SerializedProperties.
            SeeInspector = serializedObject.FindProperty("seeInspector");
            m_terrList = serializedObject.FindProperty("terrList");
            m_editorTabIndex = serializedObject.FindProperty("editorTabIndex");
            m_layerParamsList = serializedObject.FindProperty("layerParamsList");
            m_detailListRef = serializedObject.FindProperty("detailListRef");
            m_terrainSettings = serializedObject.FindProperty("terrainSettings");
            m_treeSettings = serializedObject.FindProperty("treeSettings");
            #endregion
        }

        public override void OnInspectorGUI()
        {
            #region
            if (SeeInspector.boolValue)
                DrawDefaultInspector();

            serializedObject.Update();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Show Inspector: ", GUILayout.Width(100));
            EditorGUILayout.PropertyField(SeeInspector, new GUIContent(""));
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Update the list of terrains", GUILayout.Height(30)))
                UpdateTerrainList();

            //EditorGUILayout.LabelField("");

            //if (GUILayout.Button("Update All", GUILayout.Height(30)))
              //  UpdateAll();

            EditorGUILayout.LabelField("");


            TerrainStarter terrainStarter = (TerrainStarter)target;
            if (terrainStarter.terrList.Count > 0)
            {
                m_editorTabIndex.intValue = GUILayout.SelectionGrid(m_editorTabIndex.intValue, tabs, 1);
                switch (m_editorTabIndex.intValue)
                {
                    case 0:

                        EditorGUILayout.LabelField("");
                        OptionsTerrainSettings();
                        if (GUILayout.Button("Update Terrain Settings", GUILayout.Height(30)))
                            UpdateTerrainSettings();
                        break;
                    case 1:

                        EditorGUILayout.LabelField("");
                        DisplayTerrainLayer();
                        if (GUILayout.Button("Update Terrain Layer", GUILayout.Height(30)))
                            ReplaceTerrainLayer();
                        break;
                    case 2:

                        EditorGUILayout.LabelField("");
                        DisplayTerrainDetail();
                        if (GUILayout.Button("Add Terrain Detail", GUILayout.Height(30)))
                            AddTerrainDetail();
                        if (GUILayout.Button("Update Terrain Detail", GUILayout.Height(30)))
                            UpdateTerrainDetail();
                        break;
                    case 3:
                        EditorGUILayout.LabelField("");
                        DisplayTerrainTree();
                        if (GUILayout.Button("Add Terrain Tree", GUILayout.Height(30)))
                            AddTerrainTree();
                        if (GUILayout.Button("Update Terrain Tree", GUILayout.Height(30)))
                            UpdateTerrainTree();
                        break;
                }
            }

            serializedObject.ApplyModifiedProperties();
            #endregion
        }

        void DisplayTerrainTree()
        {
            #region
            EditorGUILayout.HelpBox("Actually there is no undo for this section", MessageType.Warning);
            EditorGUILayout.HelpBox("IMPORTANT: If you want to use Mesh for terrain detail, your first detail in the list must be set up as a mesh detail.", MessageType.Warning);

            for (var i = 0; i < m_treeSettings.arraySize; i++)
            {
                SerializedProperty m_name = m_treeSettings.GetArrayElementAtIndex(i).FindPropertyRelative("name");
                SerializedProperty m_isShown = m_treeSettings.GetArrayElementAtIndex(i).FindPropertyRelative("isShown");



                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.PropertyField(m_isShown, new GUIContent(""), GUILayout.Width(20));
                EditorGUILayout.PropertyField(m_name, new GUIContent(""));

                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    m_treeSettings.DeleteArrayElementAtIndex(i);
                    break;
                }

                EditorGUILayout.EndHorizontal();

                if (m_isShown.boolValue)
                {

                    SerializedProperty m_objTree = m_treeSettings.GetArrayElementAtIndex(i).FindPropertyRelative("objTree");
                    SerializedProperty m_bendFactor = m_treeSettings.GetArrayElementAtIndex(i).FindPropertyRelative("bendFactor");

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("", GUILayout.Width(20));
                    EditorGUILayout.LabelField("Tree:", GUILayout.Width(80));
                    EditorGUILayout.PropertyField(m_objTree, new GUIContent(""));
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("", GUILayout.Width(20));
                    EditorGUILayout.LabelField("Bend Factor:", GUILayout.Width(80));
                    EditorGUILayout.PropertyField(m_bendFactor, new GUIContent(""));
                    EditorGUILayout.EndHorizontal();
                }
            }
            #endregion
        }

        void AddTerrainTree()
        {
            #region
            m_treeSettings.InsertArrayElementAtIndex(0);

            SerializedProperty m_name = m_treeSettings.GetArrayElementAtIndex(0).FindPropertyRelative("name");
            m_name.stringValue = Mathf.Clamp(m_treeSettings.arraySize - 1, 0, m_treeSettings.arraySize) + "_";

            SerializedProperty m_objTree = m_treeSettings.GetArrayElementAtIndex(0).FindPropertyRelative("objTree");
            SerializedProperty m_bendFactor = m_treeSettings.GetArrayElementAtIndex(0).FindPropertyRelative("bendFactor");

            m_objTree.objectReferenceValue = null;
            m_bendFactor.floatValue = 0;

            m_treeSettings.MoveArrayElement(0, Mathf.Clamp(m_treeSettings.arraySize - 1, 0, m_treeSettings.arraySize));
            #endregion
        }

        void UpdateTerrainTree()
        {
            #region

            string objMissingList = "";
            for (var i = 0; i < m_treeSettings.arraySize; i++)
            {
                SerializedProperty m_objTree = m_treeSettings.GetArrayElementAtIndex(i).FindPropertyRelative("objTree");

                if (!m_objTree.objectReferenceValue)
                {
                    SerializedProperty m_name = m_treeSettings.GetArrayElementAtIndex(i).FindPropertyRelative("name");
                    objMissingList += m_name.stringValue + " | ";
                }
            }



            if (objMissingList != "" && EditorUtility.DisplayDialog("Action Not Available.",
                   "One or more trees are missing: " + "\n" + objMissingList, "Continue"))
            {
            }
            else
            {
                isProcessDone = false;

                UpdateTerrainList();

                while (!isProcessDone) { }

                isProcessDone = false;

                TerrainStarter myScript = (TerrainStarter)target;

                int progress = 0;
                while (progress != myScript.terrList.Count)
                {
                    int m = progress;
                    float progressPercentage = float.Parse(progress.ToString()) / myScript.terrList.Count;
                    EditorUtility.DisplayProgressBar("Update terrain Tree", "Process: " + Mathf.RoundToInt(progressPercentage * 100) + "%", progressPercentage);

                    Undo.RegisterCompleteObjectUndo(myScript.terrList[m].terrainData, myScript.terrList[m].gameObject.name);

                    if (myScript.terrList[m])
                    {
                        List<TreePrototype> detailPrototypeCollection = new List<TreePrototype>();

                        for (var i = 0; i < m_treeSettings.arraySize; i++)
                        {
                            TreePrototype detailPrototype = new TreePrototype();

                            SerializedProperty m_objTree = m_treeSettings.GetArrayElementAtIndex(i).FindPropertyRelative("objTree");
                            SerializedProperty m_bendFactor = m_treeSettings.GetArrayElementAtIndex(i).FindPropertyRelative("bendFactor");

                            if (!m_objTree.objectReferenceValue)
                            {
                                detailPrototype.prefab = null;
                                detailPrototype.bendFactor = 0;
                            }
                            else
                            {
                                detailPrototype.prefab = (GameObject)m_objTree.objectReferenceValue;
                                detailPrototype.bendFactor = m_bendFactor.floatValue;
                            }

                            detailPrototypeCollection.Add(detailPrototype);
                        }

                        myScript.terrList[m].terrainData.treePrototypes = detailPrototypeCollection.ToArray();

                        myScript.terrList[m].terrainData.RefreshPrototypes();
                        myScript.terrList[m].Flush();
                    }
                    progress++;
                }
                EditorUtility.ClearProgressBar();

                isProcessDone = true;
            }


            #endregion
        }


        void DisplayTerrainDetail()
        {
            #region
            EditorGUILayout.HelpBox("Actually there is no undo for this section", MessageType.Warning);
            EditorGUILayout.HelpBox("IMPORTANT: If you want to use Mesh for terrain detail, your first detail in the list must be set up as a mesh detail.", MessageType.Warning);

            for (var i = 0; i < m_detailListRef.arraySize; i++)
            {
                SerializedProperty m_name = m_detailListRef.GetArrayElementAtIndex(i).FindPropertyRelative("name");
                SerializedProperty m_isShown = m_detailListRef.GetArrayElementAtIndex(i).FindPropertyRelative("isShown");
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.PropertyField(m_isShown, new GUIContent(""), GUILayout.Width(20));
                EditorGUILayout.PropertyField(m_name, new GUIContent(""));

                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    for (var j = 0; j < m_layerParamsList.arraySize; j++)
                    {
                        SerializedProperty detailList = m_layerParamsList.GetArrayElementAtIndex(j).FindPropertyRelative("detailList");
                        detailList.DeleteArrayElementAtIndex(i);
                    }

                    m_detailListRef.DeleteArrayElementAtIndex(i);
                    break;
                }

                EditorGUILayout.EndHorizontal();

                if (m_isShown.boolValue)
                {
                    SerializedProperty m_detailTexture = m_detailListRef.GetArrayElementAtIndex(i).FindPropertyRelative("detailTexture");
                    SerializedProperty m_detailObject = m_detailListRef.GetArrayElementAtIndex(i).FindPropertyRelative("detailObject");
                    SerializedProperty m_minWidth = m_detailListRef.GetArrayElementAtIndex(i).FindPropertyRelative("minWidth");
                    SerializedProperty m_maxWidth = m_detailListRef.GetArrayElementAtIndex(i).FindPropertyRelative("maxWidth");
                    SerializedProperty m_minHeight = m_detailListRef.GetArrayElementAtIndex(i).FindPropertyRelative("minHeight");
                    SerializedProperty m_maxHeight = m_detailListRef.GetArrayElementAtIndex(i).FindPropertyRelative("maxHeight");
                    SerializedProperty m_noiseSpread = m_detailListRef.GetArrayElementAtIndex(i).FindPropertyRelative("noiseSpread");
                    SerializedProperty m_holeEdgePadding = m_detailListRef.GetArrayElementAtIndex(i).FindPropertyRelative("holeEdgePadding");
                    SerializedProperty m_healthyColor = m_detailListRef.GetArrayElementAtIndex(i).FindPropertyRelative("healthyColor");
                    SerializedProperty m_dryColor = m_detailListRef.GetArrayElementAtIndex(i).FindPropertyRelative("dryColor");
                    SerializedProperty m_billboardGrass = m_detailListRef.GetArrayElementAtIndex(i).FindPropertyRelative("detailRenderModeGrass");
                    SerializedProperty m_billboardObj = m_detailListRef.GetArrayElementAtIndex(i).FindPropertyRelative("detailRenderModeObj");

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("", GUILayout.Width(20));
                    EditorGUILayout.LabelField("Detail Texture:", GUILayout.Width(100));
                    EditorGUILayout.LabelField("Texture:", GUILayout.Width(50));
                    EditorGUILayout.PropertyField(m_detailTexture, new GUIContent(""));

                    EditorGUILayout.LabelField("or Object:", GUILayout.Width(60));
                    EditorGUILayout.PropertyField(m_detailObject, new GUIContent(""));
                    EditorGUILayout.EndHorizontal();

                    if (m_detailTexture.objectReferenceValue && m_detailObject.objectReferenceValue)
                        EditorGUILayout.HelpBox("ERROR: You must choose a Texture or and Object not both.", MessageType.Error);
                    else
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("", GUILayout.Width(20));
                        EditorGUILayout.LabelField("Render mode:", GUILayout.Width(100));
                        EditorGUILayout.LabelField("", GUILayout.Width(50));
                        EditorGUILayout.PropertyField(m_billboardGrass, new GUIContent(""));
                        EditorGUILayout.LabelField("", GUILayout.Width(50));
                        EditorGUILayout.PropertyField(m_billboardObj, new GUIContent(""));
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.LabelField("");

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("", GUILayout.Width(20));
                        EditorGUILayout.LabelField("Min Width:", GUILayout.Width(100));
                        EditorGUILayout.PropertyField(m_minWidth, new GUIContent(""));
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("", GUILayout.Width(20));
                        EditorGUILayout.LabelField("Max Width:", GUILayout.Width(100));
                        EditorGUILayout.PropertyField(m_maxWidth, new GUIContent(""));
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.LabelField("");

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("", GUILayout.Width(20));
                        EditorGUILayout.LabelField("Min Height:", GUILayout.Width(100));
                        EditorGUILayout.PropertyField(m_minHeight, new GUIContent(""));
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("", GUILayout.Width(20));
                        EditorGUILayout.LabelField("Nax Height:", GUILayout.Width(100));
                        EditorGUILayout.PropertyField(m_maxHeight, new GUIContent(""));
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("", GUILayout.Width(20));
                        EditorGUILayout.LabelField("Noise Spread:", GUILayout.Width(100));
                        EditorGUILayout.PropertyField(m_noiseSpread, new GUIContent(""));
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.LabelField("");

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("", GUILayout.Width(20));
                        EditorGUILayout.LabelField("Hole Edge Padding:", GUILayout.Width(100));
                        EditorGUILayout.PropertyField(m_holeEdgePadding, new GUIContent(""));
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("", GUILayout.Width(20));
                        EditorGUILayout.LabelField("Healthy Color:", GUILayout.Width(100));
                        EditorGUILayout.PropertyField(m_healthyColor, new GUIContent(""));
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("", GUILayout.Width(20));
                        EditorGUILayout.LabelField("Dry Color:", GUILayout.Width(100));
                        EditorGUILayout.PropertyField(m_dryColor, new GUIContent(""));
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.LabelField("");
                    }
                }
            }
            #endregion
        }
        void AddTerrainDetail()
        {
            #region
            m_detailListRef.InsertArrayElementAtIndex(0);

            SerializedProperty m_name = m_detailListRef.GetArrayElementAtIndex(0).FindPropertyRelative("name");
            m_name.stringValue = Mathf.Clamp(m_detailListRef.arraySize - 1, 0, m_detailListRef.arraySize) + "_";

            SerializedProperty m_probabilityRef = m_detailListRef.GetArrayElementAtIndex(0).FindPropertyRelative("probability");
            SerializedProperty m_thresholdRef = m_detailListRef.GetArrayElementAtIndex(0).FindPropertyRelative("threshold");
            SerializedProperty m_detailIntensityRef = m_detailListRef.GetArrayElementAtIndex(0).FindPropertyRelative("detailIntensity");

            SerializedProperty m_detailTexture = m_detailListRef.GetArrayElementAtIndex(0).FindPropertyRelative("detailTexture");
            SerializedProperty m_detailObject = m_detailListRef.GetArrayElementAtIndex(0).FindPropertyRelative("detailObject");

            SerializedProperty m_healthyColor = m_detailListRef.GetArrayElementAtIndex(0).FindPropertyRelative("healthyColor");
            SerializedProperty m_dryColor = m_detailListRef.GetArrayElementAtIndex(0).FindPropertyRelative("dryColor");
            SerializedProperty m_billboardGrass = m_detailListRef.GetArrayElementAtIndex(0).FindPropertyRelative("detailRenderModeGrass");
            SerializedProperty m_billboardObj = m_detailListRef.GetArrayElementAtIndex(0).FindPropertyRelative("detailRenderModeObj");

            m_detailTexture.objectReferenceValue = null;
            m_detailObject.objectReferenceValue = null;

            m_healthyColor.colorValue = Color.white;
            m_dryColor.colorValue = Color.white;

            m_billboardGrass.enumValueIndex = 0;
            m_billboardObj.enumValueIndex = 1;


            for (var i = 0; i < m_layerParamsList.arraySize; i++)
            {
                SerializedProperty detailList = m_layerParamsList.GetArrayElementAtIndex(i).FindPropertyRelative("detailList");
                detailList.InsertArrayElementAtIndex(0);

                SerializedProperty m_DetailName = detailList.GetArrayElementAtIndex(0).FindPropertyRelative("name");
                SerializedProperty m_probability = detailList.GetArrayElementAtIndex(0).FindPropertyRelative("probability");
                SerializedProperty m_threshold = detailList.GetArrayElementAtIndex(0).FindPropertyRelative("threshold");
                SerializedProperty m_detailIntensity = detailList.GetArrayElementAtIndex(0).FindPropertyRelative("detailIntensity");

                m_DetailName.stringValue = m_name.stringValue;
                m_probability.intValue = m_probabilityRef.intValue;
                m_threshold.floatValue = m_thresholdRef.floatValue;
                m_detailIntensity.intValue = m_detailIntensityRef.intValue;

                detailList.MoveArrayElement(0, Mathf.Clamp(detailList.arraySize - 1, 0, detailList.arraySize));
            }
            m_detailListRef.MoveArrayElement(0, Mathf.Clamp(m_detailListRef.arraySize - 1, 0, m_detailListRef.arraySize));
            #endregion
        }

        void UpdateTerrainDetail()
        {
            #region
            isProcessDone = false;

            UpdateTerrainList();

            while (!isProcessDone) { }

            isProcessDone = false;

            TerrainStarter myScript = (TerrainStarter)target;

            int progress = 0;
            while (progress != myScript.terrList.Count)
            {
                int m = progress;
                float progressPercentage = float.Parse(progress.ToString()) / myScript.terrList.Count;
                EditorUtility.DisplayProgressBar("Update terrain Layer", "Process: " + Mathf.RoundToInt(progressPercentage * 100) + "%", progressPercentage);

                Undo.RegisterCompleteObjectUndo(myScript.terrList[m].terrainData, myScript.terrList[m].gameObject.name);

                if (myScript.terrList[m])
                {
                    List<DetailPrototype> detailPrototypeCollection = new List<DetailPrototype>();

                    for (var i = 0; i < m_detailListRef.arraySize; i++)
                    {
                        DetailPrototype detailPrototype = new DetailPrototype();

                        SerializedProperty m_detailTexture = m_detailListRef.GetArrayElementAtIndex(i).FindPropertyRelative("detailTexture");
                        SerializedProperty m_detailObject = m_detailListRef.GetArrayElementAtIndex(i).FindPropertyRelative("detailObject");
                        SerializedProperty m_minWidth = m_detailListRef.GetArrayElementAtIndex(i).FindPropertyRelative("minWidth");
                        SerializedProperty m_maxWidth = m_detailListRef.GetArrayElementAtIndex(i).FindPropertyRelative("maxWidth");
                        SerializedProperty m_minHeight = m_detailListRef.GetArrayElementAtIndex(i).FindPropertyRelative("minHeight");
                        SerializedProperty m_maxHeight = m_detailListRef.GetArrayElementAtIndex(i).FindPropertyRelative("maxHeight");
                        SerializedProperty m_noiseSpread = m_detailListRef.GetArrayElementAtIndex(i).FindPropertyRelative("noiseSpread");
                        SerializedProperty m_holeEdgePadding = m_detailListRef.GetArrayElementAtIndex(i).FindPropertyRelative("holeEdgePadding");
                        SerializedProperty m_healthyColor = m_detailListRef.GetArrayElementAtIndex(i).FindPropertyRelative("healthyColor");
                        SerializedProperty m_dryColor = m_detailListRef.GetArrayElementAtIndex(i).FindPropertyRelative("dryColor");

                        SerializedProperty m_billboardGrass = m_detailListRef.GetArrayElementAtIndex(i).FindPropertyRelative("detailRenderModeGrass");
                        SerializedProperty m_billboardObj = m_detailListRef.GetArrayElementAtIndex(i).FindPropertyRelative("detailRenderModeObj");

                        if (m_detailTexture.objectReferenceValue)
                        {
                            detailPrototype.usePrototypeMesh = false;
                            detailPrototype.prototypeTexture = (Texture2D)m_detailTexture.objectReferenceValue;
                            detailPrototype.renderMode = (DetailRenderMode)m_billboardGrass.enumValueIndex;
                        }
                        else
                        {
                            detailPrototype.usePrototypeMesh = true;
                            detailPrototype.useInstancing = true;
                            detailPrototype.prototype = (GameObject)m_detailObject.objectReferenceValue;
                            detailPrototype.renderMode = (DetailRenderMode)m_billboardObj.enumValueIndex;
                        }

                        detailPrototype.minWidth = m_minWidth.floatValue;
                        detailPrototype.maxWidth = m_maxWidth.floatValue;
                        detailPrototype.minHeight = m_minHeight.floatValue;
                        detailPrototype.maxHeight = m_maxHeight.floatValue;
                        detailPrototype.noiseSpread = m_noiseSpread.floatValue;
                        detailPrototype.holeEdgePadding = m_holeEdgePadding.floatValue;
                        detailPrototype.healthyColor = m_healthyColor.colorValue;
                        detailPrototype.dryColor = m_dryColor.colorValue;

                        detailPrototypeCollection.Add(detailPrototype);
                    }

                    myScript.terrList[m].terrainData.detailPrototypes = detailPrototypeCollection.ToArray();

                    myScript.terrList[m].terrainData.RefreshPrototypes();
                    myScript.terrList[m].Flush();
                }
                progress++;
            }
            EditorUtility.ClearProgressBar();

            isProcessDone = true;
            #endregion
        }

        void DisplayTerrainLayer()
        {
            #region
            EditorGUILayout.HelpBox("Actually there is no undo for this section", MessageType.Warning);
            EditorGUILayout.LabelField("Terrain Layers:", EditorStyles.boldLabel);

            if (m_layerParamsList.arraySize == 0)
            {
                if (GUILayout.Button("Add First Layer"))
                {
                    m_layerParamsList.InsertArrayElementAtIndex(0);
                    SerializedProperty m_NewName = m_layerParamsList.GetArrayElementAtIndex(0).FindPropertyRelative("name");
                    SerializedProperty m_NewTerrainLayer = m_layerParamsList.GetArrayElementAtIndex(0).FindPropertyRelative("terrainLayer");

                    m_NewName.stringValue = "New Layer";
                    m_NewTerrainLayer.objectReferenceValue = null;

                    serializedObject.ApplyModifiedProperties();
                    serializedObject.Update();

                    for (var i = 0; i < m_detailListRef.arraySize; i++)
                    {
                        SerializedProperty m_probabilityRef = m_detailListRef.GetArrayElementAtIndex(i).FindPropertyRelative("probability");
                        SerializedProperty m_thresholdRef = m_detailListRef.GetArrayElementAtIndex(i).FindPropertyRelative("threshold");
                        SerializedProperty m_detailIntensityRef = m_detailListRef.GetArrayElementAtIndex(i).FindPropertyRelative("detailIntensity");

                        m_layerParamsList.GetArrayElementAtIndex(0).FindPropertyRelative("detailList").InsertArrayElementAtIndex(0);

                        SerializedProperty m_probability = m_layerParamsList.GetArrayElementAtIndex(0).FindPropertyRelative("detailList").GetArrayElementAtIndex(0).FindPropertyRelative("probability");
                        SerializedProperty m_threshold = m_layerParamsList.GetArrayElementAtIndex(0).FindPropertyRelative("detailList").GetArrayElementAtIndex(0).FindPropertyRelative("threshold");
                        SerializedProperty m_detailIntensity = m_layerParamsList.GetArrayElementAtIndex(0).FindPropertyRelative("detailList").GetArrayElementAtIndex(0).FindPropertyRelative("detailIntensity");

                        m_probability.intValue = m_probabilityRef.intValue;
                        m_threshold.floatValue = m_thresholdRef.floatValue;
                        m_detailIntensity.intValue = m_detailIntensityRef.intValue;

                        if (m_layerParamsList.GetArrayElementAtIndex(0).FindPropertyRelative("detailList").arraySize > 0)
                            m_layerParamsList.GetArrayElementAtIndex(0).FindPropertyRelative("detailList").MoveArrayElement(0, m_layerParamsList.GetArrayElementAtIndex(0).FindPropertyRelative("detailList").arraySize - 1);
                    }
                }
            }

            for (var i = 0; i < m_layerParamsList.arraySize; i++)
            {
                SerializedProperty m_name = m_layerParamsList.GetArrayElementAtIndex(i).FindPropertyRelative("name");
                SerializedProperty m_terrainLayer = m_layerParamsList.GetArrayElementAtIndex(i).FindPropertyRelative("terrainLayer");

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(i + ":", GUILayout.Width(20));
                EditorGUILayout.PropertyField(m_name, new GUIContent(""), GUILayout.MinWidth(100));
                EditorGUILayout.PropertyField(m_terrainLayer, new GUIContent(""), GUILayout.MinWidth(50));
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    m_layerParamsList.DeleteArrayElementAtIndex(i);
                    break;
                }
                if (GUILayout.Button("Add", GUILayout.Width(40)))
                {
                    m_layerParamsList.InsertArrayElementAtIndex(i);
                    SerializedProperty m_NewName = m_layerParamsList.GetArrayElementAtIndex(i).FindPropertyRelative("name");
                    SerializedProperty m_NewTerrainLayer = m_layerParamsList.GetArrayElementAtIndex(i).FindPropertyRelative("terrainLayer");

                    m_NewName.stringValue = "New Layer";
                    m_NewTerrainLayer.objectReferenceValue = null;

                    m_layerParamsList.MoveArrayElement(i, i + 1);

                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
            #endregion
        }

        void ReplaceTerrainLayer()
        {
            #region
            isProcessDone = false;

            UpdateTerrainList();

            while (!isProcessDone) { }

            isProcessDone = false;
            TerrainStarter myScript = (TerrainStarter)target;

            int progress = 0;
            while (progress != myScript.terrList.Count)
            {
                int m = progress;
                float progressPercentage = float.Parse(progress.ToString()) / myScript.terrList.Count;
                EditorUtility.DisplayProgressBar("Update terrain Layer", "Process: " + Mathf.RoundToInt(progressPercentage * 100) + "%", progressPercentage);


                Undo.RegisterCompleteObjectUndo(myScript.terrList[m].terrainData, myScript.terrList[m].gameObject.name);

                if (myScript.terrList[m])
                {
                    List<TerrainLayer> terrainLayerCollection = new List<TerrainLayer>();

                    for (var i = 0; i < myScript.layerParamsList.Count; i++)
                        terrainLayerCollection.Add(myScript.layerParamsList[i].terrainLayer);

                    myScript.terrList[m].terrainData.terrainLayers = terrainLayerCollection.ToArray();
                }
                progress++;
            }
            EditorUtility.ClearProgressBar();

            isProcessDone = true;
            #endregion
        }

        void UpdateTerrainList()
        {
            #region
            isProcessDone = false;
            TerrainStarter terrainStarter = (TerrainStarter)target;
            Terrain[] terrainsArr = terrainStarter.GetComponentsInChildren<Terrain>();

            m_terrList.ClearArray();

            for (var i = 0; i < terrainsArr.Length; i++)
            {
                m_terrList.InsertArrayElementAtIndex(0);
                m_terrList.GetArrayElementAtIndex(0).objectReferenceValue = terrainsArr[i];
            }
            serializedObject.ApplyModifiedProperties();
            while (terrainStarter.terrList.Count != m_terrList.arraySize) { }

            isProcessDone = true;
            #endregion
        }

        public bool IsBetween(float value, float min, float max)
        {
            #region
            return (value >= min && value <= max);
            #endregion
        }

        void OptionsTerrainSettings()
        {
            #region
            EditorGUILayout.HelpBox("Actually there is no undo for this section", MessageType.Warning);

            SerializedProperty m_windSpeed = m_terrainSettings.FindPropertyRelative("windSpeed");
            SerializedProperty m_windSize = m_terrainSettings.FindPropertyRelative("windSize");
            SerializedProperty m_windBending = m_terrainSettings.FindPropertyRelative("windBending");
            SerializedProperty m_windGrassTint = m_terrainSettings.FindPropertyRelative("windGrassTint");

            SerializedProperty m_terrainWidth = m_terrainSettings.FindPropertyRelative("terrainWidth");
            SerializedProperty m_terrainLength = m_terrainSettings.FindPropertyRelative("terrainLength");
            SerializedProperty m_terrainHeight = m_terrainSettings.FindPropertyRelative("terrainHeight");
            SerializedProperty m_detailResolutionPatch = m_terrainSettings.FindPropertyRelative("detailResolutionPatch");
            SerializedProperty m_detailResolution = m_terrainSettings.FindPropertyRelative("detailResolution");

            SerializedProperty m_baseTextureResolution = m_terrainSettings.FindPropertyRelative("baseTextureResolution");

            SerializedProperty m_scaleInLightmap = m_terrainSettings.FindPropertyRelative("scaleInLightmap");
            SerializedProperty m_detailObjectDistance = m_terrainSettings.FindPropertyRelative("detailObjectDistance");


            SerializedProperty m_basemapDistance = m_terrainSettings.FindPropertyRelative("basemapDistance");
            SerializedProperty m_heightmapPixelError = m_terrainSettings.FindPropertyRelative("heightmapPixelError");
            SerializedProperty m_treeBillboardDistance = m_terrainSettings.FindPropertyRelative("treeBillboardDistance");
            SerializedProperty m_treeCrossFadeLength = m_terrainSettings.FindPropertyRelative("treeCrossFadeLength");

            /* EditorGUILayout.BeginHorizontal();
             EditorGUILayout.LabelField("Wind Speed", GUILayout.Width(150));
             EditorGUILayout.PropertyField(m_windSpeed, new GUIContent(""));
             EditorGUILayout.EndHorizontal();

             EditorGUILayout.BeginHorizontal();
             EditorGUILayout.LabelField("Wind Size", GUILayout.Width(150));
             EditorGUILayout.PropertyField(m_windSize, new GUIContent(""));
             EditorGUILayout.EndHorizontal();

             EditorGUILayout.BeginHorizontal();
             EditorGUILayout.LabelField("Wind Bending", GUILayout.Width(150));
             EditorGUILayout.PropertyField(m_windBending, new GUIContent(""));
             EditorGUILayout.EndHorizontal();

             EditorGUILayout.BeginHorizontal();
             EditorGUILayout.LabelField("Wind Grass Tint", GUILayout.Width(150));
             EditorGUILayout.PropertyField(m_windGrassTint, new GUIContent(""));
             EditorGUILayout.EndHorizontal();
            */


            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Terrain Width", GUILayout.Width(150));
            EditorGUILayout.PropertyField(m_terrainWidth, new GUIContent(""));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Terrain Length", GUILayout.Width(150));
            EditorGUILayout.PropertyField(m_terrainLength, new GUIContent(""));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Terrain Height", GUILayout.Width(150));
            EditorGUILayout.PropertyField(m_terrainHeight, new GUIContent(""));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Detail Resolution Patch", GUILayout.Width(150));
            EditorGUILayout.PropertyField(m_detailResolutionPatch, new GUIContent(""));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Detail Resolution", GUILayout.Width(150));
            EditorGUILayout.PropertyField(m_detailResolution, new GUIContent(""));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Base Texture Resolution", GUILayout.Width(150));
            EditorGUILayout.PropertyField(m_baseTextureResolution, new GUIContent(""));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Scale In Lightmap", GUILayout.Width(150));
            EditorGUILayout.PropertyField(m_scaleInLightmap, new GUIContent(""));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Base Map Distance", GUILayout.Width(150));
            EditorGUILayout.PropertyField(m_basemapDistance, new GUIContent(""));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Pixel Error", GUILayout.Width(150));
            EditorGUILayout.PropertyField(m_heightmapPixelError, new GUIContent(""));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Detail Distance", GUILayout.Width(150));
            EditorGUILayout.PropertyField(m_detailObjectDistance, new GUIContent(""));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Billboard Start", GUILayout.Width(150));
            EditorGUILayout.PropertyField(m_treeBillboardDistance, new GUIContent(""));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Fade Length", GUILayout.Width(150));
            EditorGUILayout.PropertyField(m_treeCrossFadeLength, new GUIContent(""));
            EditorGUILayout.EndHorizontal();


            #endregion
        }

        void UpdateTerrainSettings()
        {
            #region
            isProcessDone = false;

            UpdateTerrainList();

            while (!isProcessDone) { }

            isProcessDone = false;

            TerrainStarter myScript = (TerrainStarter)target;

            SerializedProperty m_windSpeed = m_terrainSettings.FindPropertyRelative("windSpeed");
            SerializedProperty m_windSize = m_terrainSettings.FindPropertyRelative("windSize");
            SerializedProperty m_windBending = m_terrainSettings.FindPropertyRelative("windBending");
            SerializedProperty m_windGrassTint = m_terrainSettings.FindPropertyRelative("windGrassTint");

            SerializedProperty m_terrainWidth = m_terrainSettings.FindPropertyRelative("terrainWidth");
            SerializedProperty m_terrainLength = m_terrainSettings.FindPropertyRelative("terrainLength");
            SerializedProperty m_terrainHeight = m_terrainSettings.FindPropertyRelative("terrainHeight");
            SerializedProperty m_detailResolutionPatch = m_terrainSettings.FindPropertyRelative("detailResolutionPatch");
            SerializedProperty m_detailResolution = m_terrainSettings.FindPropertyRelative("detailResolution");

            SerializedProperty m_baseTextureResolution = m_terrainSettings.FindPropertyRelative("baseTextureResolution");

            SerializedProperty m_scaleInLightmap = m_terrainSettings.FindPropertyRelative("scaleInLightmap");

            SerializedProperty m_detailObjectDistance = m_terrainSettings.FindPropertyRelative("detailObjectDistance");

            SerializedProperty m_basemapDistance = m_terrainSettings.FindPropertyRelative("basemapDistance");
            SerializedProperty m_heightmapPixelError = m_terrainSettings.FindPropertyRelative("heightmapPixelError");
            SerializedProperty m_treeBillboardDistance = m_terrainSettings.FindPropertyRelative("treeBillboardDistance");
            SerializedProperty m_treeCrossFadeLength = m_terrainSettings.FindPropertyRelative("treeCrossFadeLength");


            int progress = 0;
            while (progress != myScript.terrList.Count)
            {
                int m = progress;
                float progressPercentage = float.Parse(progress.ToString()) / myScript.terrList.Count;
                EditorUtility.DisplayProgressBar("Update terrain Layer", "Process: " + Mathf.RoundToInt(progressPercentage * 100) + "%", progressPercentage);

                if (myScript.terrList[m])
                {
                    Undo.RegisterCompleteObjectUndo(myScript.terrList[m].terrainData, myScript.terrList[m].gameObject.name);

                    myScript.terrList[m].terrainData.wavingGrassSpeed = m_windSize.floatValue;
                    myScript.terrList[m].terrainData.wavingGrassAmount = m_windBending.floatValue;
                    myScript.terrList[m].terrainData.wavingGrassStrength = m_windSpeed.floatValue;
                    myScript.terrList[m].terrainData.wavingGrassTint = m_windGrassTint.colorValue;

                    myScript.terrList[m].terrainData.size = new Vector3(m_terrainWidth.floatValue, m_terrainHeight.floatValue, m_terrainLength.floatValue);

                    myScript.terrList[m].terrainData.SetDetailResolution(m_detailResolution.intValue, m_detailResolutionPatch.intValue);

                    myScript.terrList[m].terrainData.baseMapResolution = m_baseTextureResolution.intValue;

                    myScript.terrList[m].detailObjectDistance = m_detailObjectDistance.floatValue;

                    myScript.terrList[m].basemapDistance = m_basemapDistance.intValue;
                    myScript.terrList[m].heightmapPixelError = m_heightmapPixelError.floatValue;
                    myScript.terrList[m].treeBillboardDistance = m_treeBillboardDistance.floatValue;
                    myScript.terrList[m].treeCrossFadeLength = m_treeCrossFadeLength.floatValue;

                    // Create SerializedObject from Terrain component
                    SerializedObject s = new SerializedObject(myScript.terrList[m]);
                    s.FindProperty("m_ScaleInLightmap").floatValue = m_scaleInLightmap.floatValue;
                    s.ApplyModifiedProperties();
                }
                progress++;
            }
            EditorUtility.ClearProgressBar();

            isProcessDone = true;
            #endregion
        }

        void UpdateAll()
        {
            #region 
            UpdateTerrainSettings();
            while (!isProcessDone) { }
            ReplaceTerrainLayer();
            while (!isProcessDone) { }
            UpdateTerrainDetail();
            while (!isProcessDone) { }
            ReplaceTerrainLayer();
            while (!isProcessDone) { }
            UpdateTerrainTree();

            if (EditorUtility.DisplayDialog("Process Done",
                    "All terrains have been updated", "Continue"))
            {
            }
            #endregion
        }
    }
}

#endif

