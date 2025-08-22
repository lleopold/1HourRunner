#if (UNITY_EDITOR)
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace HP.Generics
{
    [CustomEditor(typeof(MeshGen))]
    public class MeshGenEditor : Editor
    {
        SerializedProperty  SeeInspector;                                            // use to draw default Inspector
        SerializedProperty  m_moreOptions;
        
        SerializedProperty  m_OverrideStartPos;
        SerializedProperty  m_OverrideEndPos;

        SerializedProperty  m_pathPosList;
        SerializedProperty  m_endPathPos;
        SerializedProperty  m_startPathPos;

        SerializedProperty  m_generateOnlyASpecialCollider;

        private bool        isProcessDone = true;

        void OnEnable()
        {
            #region
            SeeInspector                    = serializedObject.FindProperty("seeInspector");
            m_moreOptions                   = serializedObject.FindProperty("moreOptions");
            m_OverrideStartPos              = serializedObject.FindProperty("overrideStartPos");
            m_OverrideEndPos                = serializedObject.FindProperty("overrideEndPos");

            m_startPathPos                  = serializedObject.FindProperty("startPathPos");
            m_endPathPos                    = serializedObject.FindProperty("endPathPos");
            m_pathPosList                   = serializedObject.FindProperty("pathPosList");

            m_generateOnlyASpecialCollider  = serializedObject.FindProperty("generateOnlyASpecialCollider");
            #endregion
        }

        public override void OnInspectorGUI()
        {
            #region
            if (SeeInspector.boolValue)
                DrawDefaultInspector();

            serializedObject.Update();

            EditorGUILayout.BeginHorizontal();
            if (!SeeInspector.boolValue)
            {
                EditorGUILayout.LabelField("Show Inspector: ", GUILayout.Width(100));
                EditorGUILayout.PropertyField(SeeInspector, new GUIContent(""), GUILayout.Width(20));
                EditorGUILayout.LabelField("More Options: ", GUILayout.Width(100));
                EditorGUILayout.PropertyField(m_moreOptions, new GUIContent(""), GUILayout.Width(20));
            }
            EditorGUILayout.EndHorizontal();

           
                
            if (!m_generateOnlyASpecialCollider.boolValue)
            {
                if (GUILayout.Button("Generate Mesh", GUILayout.Height(30)))
                {
                    if (m_endPathPos.intValue - m_startPathPos.intValue == 0 && m_moreOptions.boolValue)
                    {
                        if (EditorUtility.DisplayDialog("Wrong Selection", "Selection must be greater that 0.", "Continue"))
                            GenerateMesh(false);
                    }
                    else
                        GenerateMesh();
                }
            }
            else
            {
                if (GUILayout.Button("Generate Collider", GUILayout.Height(30)))
                {
                    UpdatePathPosList();
                    while (!isProcessDone) { }
                    GenerateCollider(false);
                }
            }

            


            SectionOverrideStartAndEndPosition();

            if (m_moreOptions.boolValue)
            {
                SelectAPartOfThePath();
                
            }
            SetOffsetDistanceFromThePath();
            EditorGUILayout.LabelField("");
            serializedObject.ApplyModifiedProperties();
            #endregion
        }

        void SelectAPartOfThePath()
        {
            #region
            EditorGUILayout.LabelField("");
            EditorGUILayout.LabelField("OPTIONS: ", EditorStyles.boldLabel);

            MeshGen meshGen = (MeshGen)target;

            if (GUILayout.Button("Update Path", GUILayout.Height(30)))
                GenerateMesh(false);


            if(meshGen.pathPosList.Count > 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Start: ", GUILayout.Width(100));
                m_startPathPos.intValue = EditorGUILayout.IntSlider(m_startPathPos.intValue, 0, m_pathPosList.arraySize - 1);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("End: ", GUILayout.Width(100));
                m_endPathPos.intValue = EditorGUILayout.IntSlider(m_endPathPos.intValue, 0, m_pathPosList.arraySize - 1);
                EditorGUILayout.EndHorizontal();
            }

            // Prevent Bugs
            if (m_startPathPos.intValue > m_endPathPos.intValue) m_startPathPos.intValue = m_endPathPos.intValue;
            if (m_endPathPos.intValue < m_startPathPos.intValue) m_endPathPos.intValue = m_startPathPos.intValue;

            if (m_startPathPos.intValue >= meshGen.pathPosList.Count) m_startPathPos.intValue = Mathf.Clamp(m_pathPosList.arraySize - 1,0, m_pathPosList.arraySize);
            if (m_endPathPos.intValue >= meshGen.pathPosList.Count) m_endPathPos.intValue = Mathf.Clamp(m_pathPosList.arraySize - 1, 0, m_pathPosList.arraySize);

            #endregion
        }

        void SetOffsetDistanceFromThePath()
        {
            #region
            MeshGen meshGen = (MeshGen)target;
            SerializedObject serializedObject2 = new SerializedObject(meshGen.GetComponent<InstantiateObjectUsingBezierCurve>());
            SerializedProperty m_distVecOffset = serializedObject2.FindProperty("distVecOffset");

            EditorGUI.BeginChangeCheck();
            serializedObject2.Update();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Path Offset: ", GUILayout.Width(100));
            EditorGUILayout.PropertyField(m_distVecOffset, new GUIContent(""), GUILayout.MinWidth(30));
            EditorGUILayout.EndHorizontal();
            serializedObject2.ApplyModifiedProperties();


            if(EditorGUI.EndChangeCheck())
                GenerateMesh(false);

            #endregion
        }

        void SectionOverrideStartAndEndPosition()
        {
            #region
            MeshGen meshGen = (MeshGen)target;
            EditorGUILayout.LabelField("Override: ", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Start Position: ", GUILayout.Width(100));
                EditorGUILayout.PropertyField(m_OverrideStartPos, new GUIContent(""));
                if (GUILayout.Button("Apply", GUILayout.MinWidth(50)))
                    OverrideFirstOrLastPosition(meshGen.overrideStartPos, 0, 1);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("End Position: ", GUILayout.Width(100));
                EditorGUILayout.PropertyField(m_OverrideEndPos, new GUIContent(""));
                if (GUILayout.Button("Apply", GUILayout.MinWidth(50)))
                {
                    Bezier bezier = meshGen.GetComponent<Bezier>();
                    OverrideFirstOrLastPosition(meshGen.overrideEndPos, bezier.pointsList.Count - 1, bezier.pointsList.Count - 2);
                }
            EditorGUILayout.EndHorizontal();
            #endregion
        }

        void OverrideFirstOrLastPosition(Transform trans,int firstIndex,int secondIndex)
        {
            #region
            MeshGen meshGen = (MeshGen)target;

            Undo.RegisterFullObjectHierarchyUndo(meshGen.gameObject, meshGen.name);

            Bezier bezier = meshGen.GetComponent<Bezier>();

            Vector3 pointPosWorld = bezier.pointsList[firstIndex].points + bezier.transform.position;
            float dist = Vector3.Distance(pointPosWorld, trans.position);
            Vector3 dir = (trans.position - pointPosWorld).normalized;

            bezier.pointsList[firstIndex].points += dist * dir;
            bezier.pointsList[secondIndex].points = bezier.pointsList[firstIndex].points + trans.forward *2;

            SceneView.RepaintAll();
            #endregion
        }

        void GenerateMesh(bool isMeshGenerated = true)
        {
            #region
            MeshGen meshGen = (MeshGen)target;

            UpdatePathPosList();
            while (!isProcessDone) { }

            int howManyIteration    = meshGen.pathPosList.Count - 1;
            if(m_moreOptions.boolValue )
                howManyIteration = m_endPathPos.intValue;

            int firstPathPos =0;
            if (m_moreOptions.boolValue)
                firstPathPos = m_startPathPos.intValue;

            meshGen.tmpShapePosList.Clear();
            meshGen.tmpShapePosList = GenerateShapeListForCalcualation(meshGen);
               
            int howManyPoints       = meshGen.tmpShapePosList.Count;

            Vector3[] vertices      = new Vector3[howManyPoints * howManyIteration  + meshGen.uvPos.Count + meshGen.uvPos.Count+1];
            Vector3[] normals       = new Vector3[howManyPoints * howManyIteration + meshGen.uvPos.Count + meshGen.uvPos.Count + 1];
            Vector2[] uvsIndex      = new Vector2[howManyPoints * howManyIteration + meshGen.uvPos.Count + meshGen.uvPos.Count+1];
            

            int trianglesStartFace  = meshGen.startFaceList.Count;


            int[] triangles         = new int[
                6 * Mathf.Clamp(howManyPoints - 1, 0, howManyPoints) + 
                6 * (howManyPoints) * Mathf.Clamp(howManyIteration - 1, 0, howManyIteration) + 
                trianglesStartFace*2];

            if (isMeshGenerated)
            {
                vertices = ReturnVerticesArray(meshGen, vertices, howManyIteration, howManyPoints, firstPathPos,meshGen.shapePosList);
                uvsIndex = ReturnUvsIndexArray(meshGen, uvsIndex, vertices, howManyIteration, howManyPoints, meshGen.shapePosList);
                triangles = ReturnTrianglesArray(meshGen, triangles, howManyIteration, howManyPoints, trianglesStartFace, firstPathPos, meshGen.startFaceList, meshGen.shapePosList);
                normals = ReturnNormalArray(meshGen, vertices, normals, howManyIteration, howManyPoints, meshGen.shapePosList);

                CreateTheMesh(meshGen, vertices, uvsIndex, triangles, normals);
            }
            else
            {
                meshGen.startPathPos = 0;
                meshGen.endPathPos = Mathf.Clamp(meshGen.pathPosList.Count - 1, 0, meshGen.pathPosList.Count);
            }
           
            #endregion
        }

        void UpdatePathPosList()
        {
            #region
            isProcessDone = false;

            MeshGen meshGen = (MeshGen)target;
            Undo.RegisterFullObjectHierarchyUndo(meshGen, "mesh");

            if (meshGen.instantiateObjectUsingBezierCurve)
            {
                InstantiateObjectUsingBezierCurve OBC = meshGen.instantiateObjectUsingBezierCurve;
                InstantiateObjectVarious.UpdateSpawnPoints(OBC);
                while (InstantiateObjectVarious.isProcessDone) { }
                SceneView.RepaintAll();

                meshGen.pathPosList.Clear();
                int counter = 0;
                for (var i = 0; i < OBC.distVecListPlusOffsetFinal.Count; i++)
                {
                    if (counter % meshGen.interval == 0)
                        meshGen.pathPosList.Add(OBC.distVecListPlusOffsetFinal[i].spotPos + OBC.transform.position);
                    counter++;
                }
            }
            isProcessDone = true;
            #endregion
        }

        List<Vector3> GenerateShapeListForCalcualation(MeshGen meshGen)
        {
            #region
            List<Vector3> newShapePosList = new List<Vector3>();

            for (var i = 0; i < meshGen.shapePosList.Count; i++)
            {
                if (i == 0 || i == meshGen.shapePosList.Count - 1)
                    newShapePosList.Add(meshGen.shapePosList[i]);
                else
                    for (var j = 0; j < 2; j++)
                        newShapePosList.Add(meshGen.shapePosList[i]);
            }

            return newShapePosList;
            #endregion
        }

        Vector2[] ReturnUvsIndexArray(MeshGen meshGen, Vector2[] uvsIndex, Vector3[] vertices, int howManyIteration, int howManyPoints,List<Vector3> shapePos)
        {
            #region
            float totalwidth = 0;

            for (var i = 0; i < shapePos.Count-1; i++)
                totalwidth += Vector3.Distance(shapePos[i + 1], shapePos[i]);

            int counter = 0;
            float posY = 0;

            for (var j = 0; j < howManyIteration; j++)
            {
                float posX = 0;

                for (var i = 0; i < howManyPoints; i++)
                {
                    if (i > 0)
                    {
                        float distRatioX = Vector3.Distance(meshGen.tmpShapePosList[i], meshGen.tmpShapePosList[i - 1]) / totalwidth;
                        // Height
                        posX += distRatioX;
                    }

                    if (j > 0)
                    {
                        float distRatioY = Vector3.Distance(vertices[0 + (j - 1) * howManyPoints], vertices[0 + j * howManyPoints]) / meshGen.tileSizeZ;
                        // width
                        posY = uvsIndex[i + (j - 1) * howManyPoints].x + distRatioY;// .05f;// 0.8335952f;
                    }

                    float invertTex = 1;
                    if (meshGen.flipTexture) invertTex = -1;

                    uvsIndex[i + j * howManyPoints] = new Vector2(posY, invertTex * posX);
                    counter++;
                }
            }

            //Front Face
            int faceFirstIndex = howManyPoints * howManyIteration;
            for (var i = 0; i < shapePos.Count; i++)
                uvsIndex[faceFirstIndex + i] = meshGen.uvPos[i] * meshGen.flipFaceUVFront;

            // Back Face
            int backFirstIndex = howManyPoints * howManyIteration + meshGen.uvPos.Count;
            for (var i = 0; i < shapePos.Count; i++)
                uvsIndex[backFirstIndex + i] =  meshGen.uvPos[i] * meshGen.flipFaceUVBack;

            return uvsIndex;

            #endregion
        }

        Vector3[] ReturnVerticesArray(MeshGen meshGen, Vector3[] vertices, int howManyIteration, int howManyPoints, int firstPathPos,List<Vector3> shapePos)
        {
            #region
            Vector3 leftDir = Vector3.zero;
            Vector3 forwardDir = Vector3.zero;
            Vector3 upDir = Vector3.zero;

            int counter = 0;
            for (var j = firstPathPos; j < howManyIteration; j++)
            {
                Vector3 knownedDir = (meshGen.pathPosList[j + 1] - meshGen.pathPosList[j]).normalized;
                Vector3 up = Vector3.up;

                leftDir = Vector3.Cross(knownedDir, up).normalized;
                upDir = Vector3.Cross(leftDir, knownedDir).normalized;

                for (var i = 0; i < howManyPoints; i++)
                {
                    Vector3 pos = Vector3.zero;

                    if (meshGen.tmpShapePosList.Count > 0)

                        if (meshGen.instantiateObjectUsingBezierCurve)
                        {
                            float invertMeshY = 1;
                            if (meshGen.flipMesh) invertMeshY = -1;

                            // First line or use startPathPos if there is a specific path lenght slection
                            if (j == 0 || (meshGen.moreOptions && j ==  meshGen.startPathPos))
                            {
                                float yPos = meshGen.tmpShapePosList[i].y;
                                if(meshGen.flatFront)
                                    yPos = 0;

                                pos = meshGen.pathPosList[j]  - invertMeshY * leftDir * meshGen.tmpShapePosList[i].x + upDir * yPos - meshGen.transform.position;
                            }
                            // Last line
                            else if (j == howManyIteration - 1)
                            {
                                Bezier bezier = meshGen.GetComponent<Bezier>();
                                if (bezier.loop)
                                {
                                    knownedDir = (meshGen.pathPosList[1] - meshGen.pathPosList[0]).normalized;
                                    up = Vector3.up;
                                    leftDir = Vector3.Cross(knownedDir, up).normalized;
                                    upDir = Vector3.Cross(leftDir, knownedDir).normalized;
                                    pos = meshGen.pathPosList[0] - invertMeshY * leftDir * meshGen.tmpShapePosList[i].x + upDir * meshGen.tmpShapePosList[i].y - meshGen.transform.position;
                                }
                                else
                                {
                                    float yPos = meshGen.tmpShapePosList[i].y;
                                    if (meshGen.flatBack)
                                        yPos = 0;
                                    pos = meshGen.pathPosList[howManyIteration] - invertMeshY * leftDir * meshGen.tmpShapePosList[i].x + upDir * yPos - meshGen.transform.position;
                                }

                            }
                            else
                            {
                                float yPos = meshGen.tmpShapePosList[i].y;
                                if (meshGen.flatFront && j < meshGen.smoothFlatFront)
                                    yPos = meshGen.tmpShapePosList[i].y * ((float)j / (float)meshGen.smoothFlatFront);

                                if (meshGen.flatBack && j > howManyIteration - 1 - meshGen.smoothFlatBack)
                                    yPos = meshGen.tmpShapePosList[i].y * (float)(howManyIteration - j) / (float)meshGen.smoothFlatBack;
                                  
                                pos = meshGen.pathPosList[j] - invertMeshY * leftDir * meshGen.tmpShapePosList[i].x + upDir * yPos - meshGen.transform.position;
                            }
                        }
                   
                    vertices[i + j * howManyPoints] = pos;
                    counter++;
                }
            }

            counter += firstPathPos * meshGen.tmpShapePosList.Count;

            // Front Face 
            for (var i = 0; i < shapePos.Count; i++)
            {
                if (!meshGen.flatFront)
                {
                    Vector3 knownedDir = (meshGen.pathPosList[firstPathPos] - meshGen.pathPosList[firstPathPos + 1]).normalized;
                    Vector3 up = Vector3.up;

                    leftDir = Vector3.Cross(knownedDir, up).normalized;
                    upDir = Vector3.Cross(leftDir, knownedDir).normalized;
                    Vector3 pos = meshGen.pathPosList[firstPathPos] + leftDir * shapePos[i].x + upDir * shapePos[i].y - meshGen.transform.position;

                    vertices[counter] = pos;
                }
                   
                counter++;
            }
            // Back Face 
            for (var i = 0; i < shapePos.Count; i++)
            {
                if (!meshGen.flatBack)
                {
                    Vector3 knownedDir = (meshGen.pathPosList[howManyIteration] - meshGen.pathPosList[howManyIteration - 1]).normalized;
                    Vector3 up = Vector3.up;

                    leftDir = Vector3.Cross(knownedDir, up).normalized;
                    upDir = Vector3.Cross(leftDir, knownedDir).normalized;

                    Vector3 pos = Vector3.zero;
                    Bezier bezier = meshGen.GetComponent<Bezier>();
                    if (bezier.loop)
                    {
                        knownedDir = (meshGen.tmpShapePosList[1] - meshGen.tmpShapePosList[0]).normalized;
                        up = Vector3.up;
                        leftDir = Vector3.Cross(knownedDir, up).normalized;
                        upDir = Vector3.Cross(leftDir, knownedDir).normalized;
                        pos = shapePos[0] - leftDir * shapePos[i].x + upDir * shapePos[i].y - meshGen.transform.position;
                    }
                    else
                        pos = meshGen.pathPosList[howManyIteration] - leftDir * shapePos[i].x + upDir * shapePos[i].y - meshGen.transform.position;


                    vertices[counter] = pos;
                }
                   
                counter++;
            }

            return vertices;
            #endregion
        }

        Vector3[] ReturnNormalArray(MeshGen meshGen, Vector3[] vertices, Vector3[] normals, int howManyIteration, int howManyPoints, List<Vector3> shapePos)
        {
            #region
            Vector3 leftDir = Vector3.zero;
            Vector3 forwardDir = Vector3.zero;
            Vector3 upDir = Vector3.zero;

            int[] triOrder = new int[6] { 0, howManyPoints, 1, 1, howManyPoints, howManyPoints + 1 };
            int howManySquare = howManyPoints / 2;

            for (var k = 0; k < howManyIteration - 1; k++)
            {
                for (var j = 0; j < howManySquare; j++)
                {
                    Vector3[] pos = new Vector3[6];
                    for (var i = 0; i < triOrder.Length; i++)
                    {
                        int id = triOrder[i] + 2 * j + (howManyPoints) * k;
                        pos[i] = vertices[id];
                    }

                    for(var m = 0; m < 2; m++)
                    {
                        Vector3 side1 = pos[1 + 3*m] - pos[0 + 3 * m];
                        Vector3 side2 = pos[2 + 3 * m] - pos[0 + 3 * m];

                        Vector3 perp = Vector3.Cross(side1, side2).normalized;

                        for (var i = 3 * m; i < 3 + 3 * m; i++)
                        {
                            int id = triOrder[i] + 2 * j + (howManyPoints) * k;
                            normals[id] = perp;
                        }
                    }
                }
            }

            // Front Face
            int faceFirstIndex = howManyPoints * howManyIteration;

            Vector3 side01 = vertices[1 + faceFirstIndex] - vertices[0 + faceFirstIndex];
            Vector3 side02 = vertices[2 + faceFirstIndex] - vertices[0 + faceFirstIndex];

            Vector3 perpFace = Vector3.Cross(side01, side02).normalized;

            for (var i = 0; i < shapePos.Count; i++)
                normals[faceFirstIndex + i] = perpFace;


            // Back Face
            int backFirstIndex = howManyPoints * howManyIteration + meshGen.uvPos.Count;

            side01 = vertices[1 + backFirstIndex] - vertices[0 + backFirstIndex];
            side02 = vertices[2 + backFirstIndex] - vertices[0 + backFirstIndex];

             perpFace = Vector3.Cross(side01, side02).normalized;

            for (var i = 0; i < shapePos.Count; i++)
                normals[backFirstIndex + i] = perpFace;


            return normals;
            #endregion
        }

        int[] ReturnTrianglesArray(MeshGen meshGen, int[] triangles, int howManyIteration, int howManyPoints, int trianglesStartFace, int firstPathPos, List<int> startFace,List<Vector3> shapePos)
        {
            #region
            int[] triOrder = new int[6] { 0, howManyPoints, 1,1,howManyPoints, howManyPoints +1};

            int howManySquare = howManyPoints/2;

            int counter = 0;
            for (var k = firstPathPos; k < howManyIteration - 1; k++)
            {
                for (var j = 0; j < howManySquare; j++)
                {
                    for (var i = 0; i < triOrder.Length; i++)
                    {
                        int id = triOrder[i] + 2 * j + (howManyPoints) * k;
                        triangles[counter] = id;
                        counter++;
                    }
                }
            }

            triOrder = startFace.ToArray();

            int firstPos =  triangles.Length - trianglesStartFace - trianglesStartFace;
            int faceFirstIndex =  meshGen.tmpShapePosList.Count * howManyIteration;
            for (var i = 0; i < trianglesStartFace; i++)
            {
                triangles[i + firstPos] = faceFirstIndex + triOrder[i];
            }
              

            firstPos = triangles.Length - trianglesStartFace;
            int backFirstIndex = howManyPoints * howManyIteration + shapePos.Count;
       
            for (var i = 0; i < trianglesStartFace; i++)
                triangles[i + firstPos] = backFirstIndex + triOrder[trianglesStartFace-1-i];
            
            return triangles;
            #endregion
        }

        void CreateTheMesh(MeshGen meshGen,Vector3[] vertices, Vector2[] uvsIndex,int[] triangles,Vector3[]normals)
        {
            #region
            Mesh mesh = new Mesh();
            mesh.vertices   = vertices;
            mesh.triangles  = triangles;
            mesh.uv         = uvsIndex;
            mesh.normals    = normals;
            mesh.RecalculateTangents( );
            Unwrapping.GenerateSecondaryUVSet(mesh);

            Undo.RegisterFullObjectHierarchyUndo(meshGen, "mesh");

            meshGen.GetComponent<MeshFilter>().sharedMesh = mesh;

            if (meshGen.GetComponent<MeshCollider>())
            {
                if (meshGen.colliderType == MeshGen.ColliderType.SameAsMesh)
                {
                    meshGen.GetComponent<MeshCollider>().enabled = true;
                    meshGen.GetComponent<MeshCollider>().sharedMesh = mesh;
                }
                     
                else if (meshGen.colliderType == MeshGen.ColliderType.Special)
                {
                    meshGen.GetComponent<MeshCollider>().enabled = true;
                    GenerateCollider();
                }
                   
                else
                    meshGen.GetComponent<MeshCollider>().enabled = false;
            }
               
            #endregion
        }

        void OnSceneGUI()
        {
            #region
            MeshGen meshGen = (MeshGen)target;
            UVFaceViewHandles();
            DisplayPathSelection();
            #endregion
        }

        void UVFaceViewHandles()
        {
            #region
            MeshGen meshGen = (MeshGen)target;

            if (meshGen.pathPosList.Count > 1)
            {
                float size = HandleUtility.GetHandleSize(meshGen.pathPosList[0]);
               
                Vector3 newTargetPosition = Vector3.zero;

                Vector3 leftDir = Vector3.zero;
                Vector3 forwardDir = Vector3.zero;
                Vector3 upDir = Vector3.zero;

                Vector3 knownedDir = (meshGen.pathPosList[1] - meshGen.pathPosList[0]).normalized;
                Vector3 up = Vector3.up;

                leftDir = Vector3.Cross(knownedDir, up).normalized;

                upDir = Vector3.Cross(leftDir, knownedDir).normalized;

                for (var i = 0; i < meshGen.shapePosList.Count; i++)
                {
                    Vector3 pointPos = meshGen.pathPosList[0] + meshGen.shapePosList[i];
                    #if UNITY_2022_OR_NEWER
                    newTargetPosition = Handles.FreeMoveHandle(meshGen.pathPosList[0] -leftDir * meshGen.shapePosList[i].x + upDir * meshGen.shapePosList[i].y, size * .1f, Vector3.zero, Handles.SphereHandleCap);
                    #else
                    var fmh_662_162_638326399980088462 = Quaternion.identity; newTargetPosition = Handles.FreeMoveHandle(meshGen.pathPosList[0] - leftDir * meshGen.shapePosList[i].x + upDir * meshGen.shapePosList[i].y, size * .1f, Vector3.zero, Handles.SphereHandleCap);
                    #endif
                }
            }
        #endregion
        }

        void DisplayPathSelection()
        {
        #region
            MeshGen meshGen = (MeshGen)target;

            if (meshGen.pathPosList.Count > m_endPathPos.intValue)
            {
                float size = HandleUtility.GetHandleSize(meshGen.pathPosList[0]);
                Handles.color = Color.green;
                #if UNITY_2022_OR_NEWER
                Handles.FreeMoveHandle(meshGen.pathPosList[m_startPathPos.intValue], size * .1f, Vector3.zero, Handles.SphereHandleCap);
                Handles.FreeMoveHandle(meshGen.pathPosList[m_endPathPos.intValue], size * .1f, Vector3.zero, Handles.SphereHandleCap);
                #else
                var fmh_682_86_638326399980093392 = Quaternion.identity; Handles.FreeMoveHandle(meshGen.pathPosList[m_startPathPos.intValue], size * .1f, Vector3.zero, Handles.SphereHandleCap);
                var fmh_683_84_638326399980099442 = Quaternion.identity; Handles.FreeMoveHandle(meshGen.pathPosList[m_endPathPos.intValue], size * .1f, Vector3.zero, Handles.SphereHandleCap);
                #endif

            }
        #endregion
        }

        void GenerateCollider(bool isMeshGenerated = true)
        {
        #region
            MeshGen meshGen = (MeshGen)target;

            //UpdatePathPosList();
            //while (!isProcessDone) { }

            int howManyIteration = meshGen.pathPosList.Count - 1;
            if (m_moreOptions.boolValue)
                howManyIteration = m_endPathPos.intValue;

            int firstPathPos = 0;
            if (m_moreOptions.boolValue)
                firstPathPos = m_startPathPos.intValue;

            meshGen.tmpShapePosList.Clear();
            meshGen.tmpShapePosList = GenerateShapeListForSpecialColliderCalculation(meshGen);

            int howManyPoints = meshGen.tmpShapePosList.Count;

            Vector3[] vertices = new Vector3[howManyPoints * howManyIteration + meshGen.uvPos.Count + meshGen.uvPos.Count + 1];
            Vector3[] normals = new Vector3[howManyPoints * howManyIteration + meshGen.uvPos.Count + meshGen.uvPos.Count + 1];
            Vector2[] uvsIndex = new Vector2[howManyPoints * howManyIteration + meshGen.uvPos.Count + meshGen.uvPos.Count + 1];


            int trianglesStartFace = meshGen.colliderStartFaceList.Count;


            int[] triangles = new int[
                6 * Mathf.Clamp(howManyPoints - 1, 0, howManyPoints) +
                6 * (howManyPoints) * Mathf.Clamp(howManyIteration - 1, 0, howManyIteration) +
                trianglesStartFace * 2];

            vertices = ReturnVerticesArray(meshGen, vertices, howManyIteration, howManyPoints, firstPathPos,meshGen.shapeColliderPosList);
            uvsIndex = ReturnUvsIndexArray(meshGen, uvsIndex, vertices, howManyIteration, howManyPoints, meshGen.shapeColliderPosList);
            triangles = ReturnTrianglesArray(meshGen, triangles, howManyIteration, howManyPoints, trianglesStartFace, firstPathPos, meshGen.colliderStartFaceList, meshGen.shapeColliderPosList);
            normals = ReturnNormalArray(meshGen, vertices, normals, howManyIteration, howManyPoints, meshGen.shapeColliderPosList);

            
            CreateTheCollider(meshGen, vertices, uvsIndex, triangles, normals);

#endregion
        }

        List<Vector3> GenerateShapeListForSpecialColliderCalculation(MeshGen meshGen)
        {
        #region
            List<Vector3> newShapePosList = new List<Vector3>();

            for (var i = 0; i < meshGen.shapeColliderPosList.Count; i++)
            {
                if (i == 0 || i == meshGen.shapeColliderPosList.Count - 1)
                    newShapePosList.Add(meshGen.shapeColliderPosList[i]);
                else
                    for (var j = 0; j < 2; j++)
                        newShapePosList.Add(meshGen.shapeColliderPosList[i]);
            }

            return newShapePosList;
#endregion
        }

        void CreateTheCollider(MeshGen meshGen, Vector3[] vertices, Vector2[] uvsIndex, int[] triangles, Vector3[] normals)
        {
        #region
            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvsIndex;
            mesh.normals = normals;
            mesh.RecalculateTangents();

            if (meshGen.GetComponent<MeshCollider>()) meshGen.GetComponent<MeshCollider>().sharedMesh = mesh;
#endregion
        }
    }
}
#endif

