// Description: MeshGen: Generate mesh along a path using a custom shape.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace HP.Generics{
    public class MeshGen : MonoBehaviour
    {
        public bool                                 seeInspector = true;
        public bool                                 moreOptions = false;
        public InstantiateObjectUsingBezierCurve    instantiateObjectUsingBezierCurve;
        public List<Vector3>                        shapePosList = new List<Vector3>();

        [HideInInspector]
        public List<Vector3>                        tmpShapePosList = new List<Vector3>();

        [HideInInspector]
        public List<Vector3>                        pathPosList = new List<Vector3>();
        public int                                  interval = 10;
        public int                                  startPathPos = 0;
        public int                                  endPathPos = 0;
       
        public bool                                 flipTexture = false;
        public bool                                 flipMesh = false;

        [HideInInspector]
        public Transform                            overrideStartPos;
        [HideInInspector]
        public Transform                            overrideEndPos;

        [Header("UV Face and Back")]
        public bool                                 flatFront = false;
        public int                                  smoothFlatFront = 1;
        public bool                                 flatBack = false;
        public int                                  smoothFlatBack = 1;
        public List<int>                            startFaceList = new List<int>();
        public bool                                 enableUVPosition = true;
        public Vector3                              uvOffset = new Vector3();
        public float                                atlasFaceSize = 10;
        public Vector2                              flipFaceUVFront = Vector2.one;
        public Vector2                              flipFaceUVBack = new Vector2(-1,1);

        public List<Vector2>                        uvPos = new List<Vector2>();

        public bool                                 isNormalsDisplayed = false;
        public bool                                 isTangentsDisplayed = false;

        public float                                tileSizeZ = 3;

        public bool                                 generateOnlyASpecialCollider = false;
        public List<Vector3>                        shapeColliderPosList = new List<Vector3>();
        public List<int>                            colliderStartFaceList = new List<int>();

        public enum ColliderType { SameAsMesh,Special,NoCollider}
        public ColliderType colliderType = ColliderType.NoCollider;

        private void OnDrawGizmosSelected()
        {
            #region
            if (AllowUvModification())
            {
                DisplayUVSquareInSceneView();
                ReturnVerticesPositionInUV();
            }

            if (isNormalsDisplayed)
                ShowNormals();

            if (isTangentsDisplayed)
                ShowTangents();

            #endregion
        }

        bool AllowUvModification()
        {
            #region
            if (enableUVPosition && pathPosList.Count > 1 && shapePosList.Count > 0)
                return true;
            else
                return false;
            #endregion
        }

        public void DisplayUVSquareInSceneView()
        {
            #region
            Vector3 dirForward = (pathPosList[1] - pathPosList[0]).normalized;
            Vector3 dirLeft = Vector3.Cross(dirForward, Vector3.up).normalized;

            Vector3 firstPos = pathPosList[0] + shapePosList[0].y * Vector3.up - shapePosList[0].x * dirLeft;

            Vector3 offsetPos = uvOffset.x * dirLeft + uvOffset.y * Vector3.up;

            Vector3 botomLeft   = firstPos + offsetPos + dirLeft * atlasFaceSize * .5f - Vector3.up * atlasFaceSize * .5f;
            Vector3 botomRight  = firstPos + offsetPos - dirLeft * atlasFaceSize * .5f - Vector3.up * atlasFaceSize * .5f;
            Vector3 upLeft      = firstPos + offsetPos + dirLeft * atlasFaceSize * .5f + Vector3.up * atlasFaceSize * .5f;
            Vector3 upRight     = firstPos + offsetPos - dirLeft * atlasFaceSize * .5f + Vector3.up * atlasFaceSize * .5f;

            Gizmos.DrawLine(botomLeft, botomRight);
            Gizmos.DrawLine(botomRight, upRight);
            Gizmos.DrawLine(upRight, upLeft);
            Gizmos.DrawLine(upLeft, botomLeft);
            #endregion
        }

        public List<Vector2> ReturnVerticesPositionInUV()
        {
            #region
            uvPos.Clear();

            float scaledOffsetValueX = uvOffset.x / atlasFaceSize;
            float scaledOffsetValueY = -uvOffset.y / atlasFaceSize;

            float posX = .5f + scaledOffsetValueX;
            float posY = .5f + scaledOffsetValueY;

            Vector2 firstPosUV = new Vector2(posX, posY);
            uvPos.Add(firstPosUV);

            for (var i = 1; i < shapePosList.Count; i++)
            {
                float disX = (shapePosList[i].x - shapePosList[0].x) / atlasFaceSize;
                float disY = (shapePosList[i].y - shapePosList[0].y) / atlasFaceSize;

                Vector2 shapePos = firstPosUV + new Vector2(disX, disY);
                uvPos.Add(shapePos);
            }

            return uvPos;
            #endregion
        }

        private void ShowNormals()
        {
            #region
            Mesh mesh = GetComponent<MeshFilter>().sharedMesh;

            // Gizmo to fit the position, rotation and scale of the gameObject
            Matrix4x4 cubeTransform = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);      
            Gizmos.matrix = cubeTransform;

            if (mesh)
                for (var i = 0; i < mesh.vertexCount; i++)
                    Gizmos.DrawRay(mesh.vertices[i], mesh.normals[i] * .5f);

            #endregion
        }

        private void ShowTangents()
        {
            #region
            Mesh mesh = GetComponent<MeshFilter>().sharedMesh;
            
            // Gizmo to fit the position, rotation and scale of the gameObject
            Matrix4x4 cubeTransform = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);      
            Gizmos.matrix = cubeTransform;

            if (mesh)
                for (var i = 0; i < mesh.vertexCount; i++)
                    Gizmos.DrawRay(mesh.vertices[i], mesh.tangents[i] * .5f);
            #endregion
        }
    }

}
