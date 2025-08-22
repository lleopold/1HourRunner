#if (UNITY_EDITOR)
using UnityEngine;
using UnityEditor;

namespace HP.Generics
{
    public class TSMenuCommand : MonoBehaviour
    {
        [MenuItem("GameObject/HP/Move To Root")]
        static void MoveToRoot(MenuCommand menuCommand)
        {
            #region
            if (Selection.objects.Length > 1)
                if (menuCommand.context != Selection.objects[0])
                    return;

            GameObject[] objs = Selection.gameObjects;

            foreach (GameObject obj in objs)
            {
                Undo.SetTransformParent(obj.transform, null, obj.name);
                Undo.RegisterFullObjectHierarchyUndo(obj, obj.name);
                obj.transform.SetAsLastSibling();
            }
            #endregion
        }

        [MenuItem("GameObject/HP/Create Group")]
        static void CreateGroup(MenuCommand menuCommand)
        {
            #region
            if (Selection.objects.Length > 1)
            {
                if (menuCommand.context == Selection.objects[0])
                    GroupObjects();
            }
            else if (menuCommand.context == Selection.objects[0])
                if (EditorUtility.DisplayDialog("This action is not possible", "Select at least 2 objects to create a group", "Continue")) { }

            #endregion
        }

        [MenuItem("GameObject/HP/Group + Move To Root")]
        static void GroupPlusMoveToRoot(MenuCommand menuCommand)
        {
            #region
            if (Selection.objects.Length > 1)
            {
                if (menuCommand.context == Selection.objects[0])
                    GroupObjects(true);
            }
            else if (menuCommand.context == Selection.objects[0])
                if (EditorUtility.DisplayDialog("This action is not possible", "Select at least 2 objects to create a group", "Continue")) { }
            #endregion
        }

        static void GroupObjects(bool isObjectsGrouped = false)
        {
            #region
            GameObject[] objs = Selection.gameObjects;

            // Create folder
            GameObject grpFolder = new GameObject();

            Undo.RegisterCreatedObjectUndo(grpFolder, grpFolder.name);

            Undo.SetTransformParent(grpFolder.transform, objs[0].transform.parent, grpFolder.name);

            Vector3 centerPos = FindCenter(objs);

            grpFolder.transform.position = centerPos;
            grpFolder.name = "Grp_";

            // Move Object to folder
            foreach (GameObject obj in objs)
            {
                Undo.SetTransformParent(obj.transform, grpFolder.transform, obj.name);
                Undo.RegisterFullObjectHierarchyUndo(obj, obj.name);
                obj.transform.SetAsLastSibling();
            }

            if (isObjectsGrouped)
            {
                Undo.SetTransformParent(grpFolder.transform, null, grpFolder.name);
                Undo.RegisterFullObjectHierarchyUndo(grpFolder, grpFolder.name);
                grpFolder.transform.SetAsLastSibling();
            }

                Selection.activeObject = grpFolder;
            #endregion
        }

        public static Vector3 FindCenter(GameObject[] objs)
        {
            #region
            var bound = new Bounds(objs[0].transform.position, Vector3.zero);
            for (int i = 1; i < objs.Length; i++)
                bound.Encapsulate(objs[i].transform.position);
            return bound.center;
            #endregion
        }

        [MenuItem("GameObject/HP/OptiGrid Group")]
        static void CreateGroupOptiGrid(MenuCommand menuCommand)
        {
            #region
            if (Selection.objects.Length > 1)
            {
                if (menuCommand.context == Selection.objects[0])
                    GroupObjectsOptiGrid();
            }
            else if (menuCommand.context == Selection.objects[0])
                if (EditorUtility.DisplayDialog("This action is not possible", "Select at least 2 objects to create a group", "Continue")) { }

          
            #endregion
        }

        [MenuItem("GameObject/HP/OptiGrid + Move To Root")]
        static void GroupPlusMoveToRootOptiGrid(MenuCommand menuCommand)
        {
            #region
            if (Selection.objects.Length > 1)
            {
                if (menuCommand.context == Selection.objects[0])
                    GroupObjectsOptiGrid(true);
            }
            else if (menuCommand.context == Selection.objects[0])
                if (EditorUtility.DisplayDialog("This action is not possible", "Select at least 2 objects to create a group", "Continue")) { }
            #endregion
        }

        static void GroupObjectsOptiGrid(bool isObjectsGrouped = false)
        {
            #region
            GameObject[] objs = Selection.gameObjects;

            // Create folder
            GameObject grpFolder = new GameObject();

            Undo.RegisterCreatedObjectUndo(grpFolder, grpFolder.name);

            Undo.SetTransformParent(grpFolder.transform, objs[0].transform.parent, grpFolder.name);

            Vector3 centerPos = FindCenter(objs);

            grpFolder.transform.position = centerPos;
            grpFolder.name = "Grp_Opti";

            grpFolder.AddComponent<TSStreamGridTag>();


            // Move Object to folder
            foreach (GameObject obj in objs)
            {
                Undo.SetTransformParent(obj.transform, grpFolder.transform, obj.name);
                Undo.RegisterFullObjectHierarchyUndo(obj, obj.name);
                obj.transform.SetAsLastSibling();
            }

            if (isObjectsGrouped)
            {
                Undo.SetTransformParent(grpFolder.transform, null, grpFolder.name);
                Undo.RegisterFullObjectHierarchyUndo(grpFolder, grpFolder.name);
                grpFolder.transform.SetAsLastSibling();
            }


            Selection.activeObject = grpFolder;

            #endregion
        }
    }
}
#endif