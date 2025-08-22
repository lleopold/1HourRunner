// Description : w_CleanRoadCollider: Remove all the collider used to create road.
#if (UNITY_EDITOR)
using UnityEngine;
using UnityEditor;


namespace HP.Generics
{
    public class w_CleanRoadCollider : EditorWindow
    {
        [MenuItem("Tools/HP/Generator/Delete Road Colliders")]
        static void CleanRoadColliders()
        {
            #region
            RoadTag[] roadTags = FindObjectsOfType<RoadTag>();

            foreach (RoadTag rt in roadTags)
            {
                if (rt.ID == 2 || rt.ID == 3 || rt.ID == 4)
                {
                    Undo.RegisterFullObjectHierarchyUndo(rt.gameObject, rt.name);
                    rt.gameObject.GetComponent<MeshFilter>().sharedMesh = null;
                    rt.gameObject.GetComponent<MeshCollider>().sharedMesh = null;
                }

                if (rt.ID == 6)
                {
                    Undo.RegisterFullObjectHierarchyUndo(rt.gameObject, rt.name + "_PathBump");
                   if(rt.gameObject.GetComponent<MeshFilter>()) rt.gameObject.GetComponent<MeshFilter>().sharedMesh = null;
                    if (rt.gameObject.GetComponent<MeshCollider>()) rt.gameObject.GetComponent<MeshCollider>().sharedMesh = null;
                }
            }

            Debug.Log(roadTags.Length + " road(s) cleaned");
            #endregion
        }
    }
}

#endif