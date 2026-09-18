using System.Linq;
using UnityEngine;
using UnityEditor;
using RayFire;

namespace RayFireEditor
{
    [CanEditMultipleObjects]
    [CustomEditor (typeof(RFInteractiveHelper))]
    public class RFInteractiveHelperEditor : Editor
    {
        RFInteractiveHelper helper;
        
        public static Color boxColorOut = new Color (0.58f, 0.77f, 1f, 0.5f);
        
        // Serialized properties
        //SerializedProperty sp_acd_tp;

        private void OnEnable()
        {
            // Get component
            helper = (RFInteractiveHelper)target;
            
            // Find properties
            //sp_acd_tp = serializedObject.FindProperty(nameof(helper.acdType));
        }
        
        /// /////////////////////////////////////////////////////////
        /// Inspector
        /// /////////////////////////////////////////////////////////

        public override void OnInspectorGUI()
        {
            // Update changed properties
            serializedObject.Update();

            // Space
            GUILayout.Space (8);
            
            // GUI
            GUI_Properties();

            // Apply changes
            serializedObject.ApplyModifiedProperties();
        }
        
        /// /////////////////////////////////////////////////////////
        /// Properties
        /// /////////////////////////////////////////////////////////

        void GUI_Properties()
        {
            if (GUILayout.Button (TextSht.gui_btn_sel, RFUI.buttonStyle, GUILayout.Height (25)))
            {
                if (helper.shatter != null)
                    Selection.activeGameObject = helper.shatter;
            }
        }

        [DrawGizmo (GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
        static void DrawGizmos(RFInteractiveHelper inter, GizmoType gizmoType)
        {
            if (inter.interactive == false)
                return;

            if (inter.type == RFInteractiveHelper.RFInterType.Preview)
            {
                Gizmos.matrix = Matrix4x4.TRS(inter.transform.position, inter.transform.rotation, Vector3.one);
                Gizmos.color  = inter.color;
                Gizmos.DrawWireCube (Vector3.zero, inter.sh.bound.extents);
            }
            
            if (inter.type != RFInteractiveHelper.RFInterType.Cluster)
                return;
                    
            if (inter.gizmo == RFInteractiveHelper.RFGizmoType.Box)
            {
                // Null check
                if (inter.sh == null || inter.sh.interactive == false) 
                    return;

                if (inter.sh.clusters.enable == false)
                    return;
                
                if (inter.mf == null || inter.mf.sharedMesh == null) 
                    return;
                
                // Get size
                Vector3 ext = inter.mf.sharedMesh.bounds.extents;
                Vector3 scl = inter.transform.lossyScale;  
                ext.x *= scl.x;
                ext.y *= scl.y;
                ext.z *= scl.z;
                
                // Draw gizmo
                Gizmos.matrix = Matrix4x4.TRS(inter.transform.position, inter.transform.rotation, Vector3.one);
                Gizmos.color  = inter.color;
                Gizmos.DrawCube (Vector3.zero, -1f * ext * 2f);

                // Default wire color
                Gizmos.color  = boxColorOut;
                
                // Selection wire color
                if (Selection.count < 9 && Selection.gameObjects.Contains (inter.gameObject) == true)
                    Gizmos.color  = Color.green;
                
                // Draw wire
                Gizmos.DrawWireCube (Vector3.zero, ext * 2f);
            }
        }
    }
}