//Description: ResetPathEditor: Custom Editor
#if (UNITY_EDITOR)
using UnityEngine;
using UnityEditor;


namespace HP.Generics
{
    [CustomEditor(typeof(ResetPath))]
    public class ResetPathEditor : Editor
    {
        SerializedProperty SeeInspector;                                            // use to draw default Inspector
        SerializedProperty m_resetTransform;
        SerializedProperty m_resetBezierScript;
        SerializedProperty m_resetMesh;
        SerializedProperty m_resetCollider;

        void OnEnable()
        {
            #region
            // Setup the SerializedProperties.
            SeeInspector = serializedObject.FindProperty("seeInspector");
            m_resetTransform = serializedObject.FindProperty("resetTransform");
            m_resetBezierScript = serializedObject.FindProperty("resetBezierScript");
            m_resetMesh = serializedObject.FindProperty("resetMesh");
            m_resetCollider = serializedObject.FindProperty("resetCollider");
            #endregion
        }

        public override void OnInspectorGUI()
        {
            #region
            ResetPath rPath = (ResetPath)target;

            if (SeeInspector.boolValue)
                DrawDefaultInspector();

            serializedObject.Update();

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.PropertyField(SeeInspector, new GUIContent(""), GUILayout.Width(20));
            EditorGUILayout.LabelField("Reset:", GUILayout.Width(50));

            if (rPath.GetComponent<InstantiateObjectUsingBezierCurve>())
            {
                if (GUILayout.Button("Path + Instantiated Objects"))
                {
                    ResetInstantiateObjects();
                    ResetPath();
                }

            }
            else
            if (GUILayout.Button("Path"))
                ResetPath();

            EditorGUILayout.EndHorizontal();

            if (rPath.mirror)
            {
                if (GUILayout.Button("Mirror"))
                    Mirror();
            }

            serializedObject.ApplyModifiedProperties();
            #endregion
        }

        void ResetPath()
        {
            #region
            ResetPath rPath = (ResetPath)target;
            Undo.RegisterFullObjectHierarchyUndo(rPath.gameObject, rPath.name);

            if (m_resetTransform.boolValue)
            {
                rPath.transform.position = Vector3.zero;
                rPath.transform.rotation = Quaternion.identity;
                rPath.transform.localScale = Vector3.one;
            }

            MeshGen meshGen = rPath.gameObject.GetComponent<MeshGen>();
            if (meshGen)
                meshGen.pathPosList.Clear();

            if (m_resetBezierScript.boolValue)
            {
                Bezier bezier = rPath.gameObject.GetComponent<Bezier>();
                bezier.pointsList.Clear();
                bezier.distVecList.Clear();
            }

            if (m_resetMesh.boolValue)
            {
                MeshFilter meshFilter = rPath.gameObject.GetComponent<MeshFilter>();
                meshFilter.sharedMesh = null;
            }

            if (m_resetMesh.boolValue)
            {
                MeshCollider meshCollider = rPath.gameObject.GetComponent<MeshCollider>();
                meshCollider.sharedMesh = null;
            }

            PrefabUtility.RecordPrefabInstancePropertyModifications(rPath);
            #endregion
        }

        void ResetInstantiateObjects()
        {
            #region
            ResetPath rPath = (ResetPath)target;
            Undo.RegisterFullObjectHierarchyUndo(rPath.gameObject, rPath.name);

            GameObject grpInstantiatedObjs = rPath.GetComponent<InstantiateObjectUsingBezierCurve>().grpThatContainInstantiateObjects;

            if (grpInstantiatedObjs)
                Undo.DestroyObjectImmediate(grpInstantiatedObjs);

            PrefabUtility.RecordPrefabInstancePropertyModifications(rPath);
            #endregion
        }

        void Mirror()
        {
            ResetPath rPath = (ResetPath)target;

            switch (rPath.mirrorType)
            {
                case MirrorType.None:

                    break;
                case MirrorType.BarrierAndWall:
                    BarrierAndWall();
                    break;

                case MirrorType.Concrete:
                    Concrete();
                    break;

                case MirrorType.InstantiatedOnly:
                    InstantiatedOnly();
                    break;

            }

        }

        void InstantiatedOnly()
        {
            #region
            ResetPath rPath = (ResetPath)target;
            Bezier bezier = rPath.GetComponent<Bezier>();

            bezier.pointsList.Reverse();

            if (EditorUtility.DisplayDialog("Mirror Done",
                    "To update the object:" +
                    "\n" +
                    "Press Button ''Instantiate Objects''"
                    , "Continue"))
            {
            }
            #endregion
        }

        void Concrete()
        {
            #region
            ResetPath rPath = (ResetPath)target;
            Bezier bezier = rPath.GetComponent<Bezier>();

            bezier.pointsList.Reverse();

            if (EditorUtility.DisplayDialog("Mirror Done",
                    "To update the object:" +
                    "\n" +
                    "Press Button ''Generate Mesh''"
                    , "Continue"))
            {
            }
            #endregion
        }

        void BarrierAndWall()
        {
            #region
            ResetPath rPath = (ResetPath)target;
            Bezier bezier = rPath.GetComponent<Bezier>();

            bezier.pointsList.Reverse();

            if (EditorUtility.DisplayDialog("Mirror Done",
                    "To update the object:" +
                    "\n" +
                    "Press Button ''Generate Mesh''" +
                    "\n" +
                    "and" +
                    "\n" +
                     "Press Button ''Instantiate Objects''", "Continue"))
            {
            }
            #endregion
        }
    }
}

#endif

