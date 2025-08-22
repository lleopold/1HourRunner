#if (UNITY_EDITOR)
using UnityEngine;
using UnityEditor;

namespace HP.Generics
{
    public class TetrahedronCommand : MonoBehaviour
    {
        [MenuItem("Tools/HP/Tetrahedron Refresh")]
        static void FocusCommandWhenKeyUIsPressed()
        {
            LightProbes.TetrahedralizeAsync();

            if (EditorUtility.DisplayDialog("Process Done",
                "TetrahedralizeAsync process done.", "Continue")){}
        }
    }
}
#endif