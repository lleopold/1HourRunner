#if (UNITY_EDITOR)
using UnityEngine;
using UnityEditor;

namespace TSHP.All
{
    public class MenuCommandAllAssets : MonoBehaviour
    {
        
         [MenuItem("Tools/Shortcuts/Command/Focus on Scene view U Command _u")]
         static void FocusCommandWhenKeyUIsPressed()
         {
             #region
              if (Selection.activeGameObject && Selection.activeGameObject.GetComponent<TSHP.All.FocusTagCommandAllAssets>())
             {
                 SceneView.lastActiveSceneView.Focus();
                 Tools.current = Tool.Move;
             }
             #endregion
         }

        [MenuItem("Tools/Shortcuts/Command/Focus on Scene view N Command _n")]
        static void FocusCommandWhenKeyNIsPressed()
        {
            #region
            if (Selection.activeGameObject && Selection.activeGameObject.GetComponent<TSHP.All.FocusTagCommandAllAssets>())
            {
                SceneView.lastActiveSceneView.Focus();
                Tools.current = Tool.Move;
            }
            #endregion
        }

        [MenuItem("Tools/Shortcuts/Command/Focus on Scene view Shift+S Command _#s")]
        static void FocusCommandWhenKeyShiftSIsPressed()
        {
            #region
            if (Selection.activeGameObject && Selection.activeGameObject.GetComponent<TSHP.All.FocusTagCommandAllAssets>())
            {
                SceneView.lastActiveSceneView.Focus();
                Tools.current = Tool.Move;
            }
            #endregion
        }

    }
}
#endif