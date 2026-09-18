using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RayFire
{
    /// <summary>
    /// Interactive helper component for Rayfire Shatter component.
    /// </summary>
    [ExecuteInEditMode]
    public class RFInteractiveHelper : MonoBehaviour
    {
        public enum RFInterType
        {
            Preview  = 0,
            Fragment = 1,
            Cluster  = 2
        }
        
        public enum RFGizmoType
        {
            Disabled = 0,
            Box      = 1,
            Sphere   = 2,
            Point    = 3
        }
        
        public GameObject     shatter;
        public RFInterType    type;
        public RFGizmoType    gizmo;
        public bool           interactive;
        public MeshFilter     mf;
        public RayfireShatter sh;
        public Color          color;
        
        [NonSerialized] bool  ofsState;
        readonly float ofsVal = 0.05f;
        
        /// /////////////////////////////////////////////////////////
        /// Common
        /// /////////////////////////////////////////////////////////
        
        // Update is called once per frame
        void Update()
        {
            InitInteractiveUpdate();
        }
        
        //void OnDisable()
        void OnDestroy()
        {
            interactive = false;
            
            if (sh == null) 
                return;
            
            if (type == RFInterType.Preview) 
                return;
            
            // Init refrag or cluster update
            InitInteractiveUpdate();
        }
        
        void OnEnable()
        {
            mf = GetComponent<MeshFilter>();
            RandomPointOffset();
        }
        
        /*
        void OnTransformParentChanged()
        {
            if (type == RFInterType.Cluster)
            {
                // TODO check parent and destroy if should not be interactive
                // Debug.Log (name);
            }
        }
        */
        
        void OnTransformChildrenChanged()
        {
            if (type == RFInterType.Cluster)
            {
                AddChild();
            }
        }
        
        
        /// /////////////////////////////////////////////////////////
        /// Shatter update and auto remove
        /// /////////////////////////////////////////////////////////
        
        void InitInteractiveUpdate()
        {
            if (interactive == false)
                return;

            // Shatter object destroyed
            if (shatter == null || sh == null || sh.interactive == false)
            {
                DestroyPreview();
                return;
            }
            
            // No update for Interactive preview object
            if (type == RFInterType.Preview)
                return;

            // Interactive helper transform changed
            if (transform.hasChanged == false)
                return;

            // Prevent Cluster update 
            PreventClusterUpdate();
            
            // Init refrag or cluster update
            if (type != RFInterType.Cluster)
                sh.InteractiveChange();
            else
                sh.InteractiveCluster();
            
            // Set changed state
            transform.hasChanged = false;
        }

        void DestroyPreview()
        {
            if (type == RFInterType.Preview)
            {
                if (shatter != null)
                    shatter.SetActive (true);
                DestroyImmediate (gameObject);
            }
            DestroyImmediate (this);
        }

        /// /////////////////////////////////////////////////////////
        /// Static
        /// /////////////////////////////////////////////////////////
        
        // Set interactive data to use at properties change
        public static void InteractiveSetup(RayfireShatter sh, List<Transform> fragments)
        {
            if (sh.engine.interactive == false)
                return;
            
            // Disable own Renderers
            sh.gameObject.SetActive (false);
            
            // Get meshfilters to input changed meshes
            if (sh.intMfs == null) 
                sh.intMfs = new List<MeshFilter>();
            else 
                sh.intMfs.Clear();
            if (sh.intMrs == null) 
                sh.intMrs = new List<Renderer>();
            else 
                sh.intMrs.Clear();
            for (int i = 0; i < fragments.Count; i++)
            {
                sh.intMfs.Add (fragments[i].GetComponent<MeshFilter>());
                sh.intMrs.Add (fragments[i].GetComponent<Renderer>());
            }
        }
        
        // Check if acd object already in list
        public static bool InListCheck(RayfireShatter shatter, int i)
        {
            for (int j = 0; j < shatter.clusters.acdList.Count; j++)
                if (i != j && shatter.clusters.acdList[j].go == shatter.clusters.acdList[i].go)
                    return  true;
            return false;
        }
        
        /// /////////////////////////////////////////////////////////
        /// Helpers
        /// /////////////////////////////////////////////////////////
        
        // Add helpers
        public static void AddHelpers(RayfireShatter sh)
        {
            AddInteractiveHelper (sh, sh.engine.mainRoot.transform, RFInterType.Preview);
            if (sh.advanced.centerBias != null)
                AddInteractiveHelper (sh, sh.advanced.centerBias);
            if (sh.advanced.ab_obj != null)
                AddInteractiveHelper (sh, sh.advanced.ab_obj);
            if (sh.slice.sliceList != null && sh.slice.sliceList.Count > 0)
                for (int i = 0; i < sh.slice.sliceList.Count; ++i)
                    if (sh.slice.sliceList[i] != null)
                        AddInteractiveHelper (sh, sh.slice.sliceList[i]);
            if (sh.custom.transforms != null && sh.custom.transforms.Count > 0)
                for (int i = 0; i < sh.custom.transforms.Count; ++i)
                    if (sh.custom.transforms[i] != null)
                        AddInteractiveHelper (sh, sh.custom.transforms[i]);
            
            // Advanced clustering points
            AddPointHelpers (sh, sh.clusters.pointRoot);
            
            // Advanced clustering volume
            if (sh.clusters.AcdState == true)
                for (int i = 0; i < sh.clusters.acdList.Count; ++i)
                    AddVolumeHelpers (sh, sh.clusters.acdList[i]);
        }
        
        // Add interactive helper component
        public static void AddInteractiveHelper(RayfireShatter shatter, Transform target, RFInterType Type = RFInterType.Fragment, RFGizmoType Gizmo = RFGizmoType.Disabled)
        {
            if (target == null)
                return;
            
            RFInteractiveHelper helper = target.gameObject.GetComponent<RFInteractiveHelper>();
            if (helper == null)
                helper = target.gameObject.AddComponent<RFInteractiveHelper>();
                
            helper.interactive = true;
            helper.type        = Type;
            helper.gizmo       = Gizmo;
            helper.sh          = shatter;
            helper.shatter     = shatter.gameObject;
            
            // Prevent instant update refrag
            helper.transform.hasChanged = false;
        }
        
        // Add pint helper
        public static void AddPointHelpers(RayfireShatter shatter, Transform tm)
        {
            if (shatter.interactive == false)
                return;
            
            if (tm == null)
                return;
            
            AddInteractiveHelper (shatter, tm, RFInterType.Cluster, RFGizmoType.Point);
            if (tm.childCount > 0)
                for (int i = 0; i < tm.childCount; ++i)
                    AddInteractiveHelper (shatter, tm.GetChild (i), RFInterType.Cluster, RFGizmoType.Point);
        }
        
        // Add interactive helper component for volumes
        public static void AddVolumeHelpers(RayfireShatter shatter, RFAcd acd)
        {
            if (acd.go == null)
                return;
            
            acd.hlp = new List<RFInteractiveHelper> {AddVolumeHelper (shatter, acd, acd.go)};
            if (acd.go.transform.childCount > 0)
                for (int c = 0; c < acd.go.transform.childCount; ++c)
                    acd.hlp.Add (AddVolumeHelper (shatter, acd, acd.go.transform.GetChild (c).gameObject));
        }
        
        // Add interactive volume helper component
        static RFInteractiveHelper AddVolumeHelper(RayfireShatter shatter, RFAcd acd, GameObject go)
        {
            RFInteractiveHelper helper = go.GetComponent<RFInteractiveHelper>();
            if (helper == null)
                helper = go.gameObject.AddComponent<RFInteractiveHelper>();
                
            helper.interactive = true;
            helper.type        = RFInterType.Cluster;
            helper.gizmo       = acd.gz == true ? RFGizmoType.Box : RFGizmoType.Disabled;
            helper.color       = acd.col;
            helper.sh          = shatter;
            helper.shatter     = shatter.gameObject;
            
            // Prevent instant update refrag
            helper.transform.hasChanged = false;
            
            return helper;
        }

        // Remove helpers
        public static void RemoveInteractiveHelper(GameObject go)
        {
            if (go != null)
            {
                RFInteractiveHelper inter = go.GetComponent<RFInteractiveHelper>();
                if (inter != null)
                    inter.sh = null;
                RFInteractiveHelper[] inters = go.GetComponentsInChildren<RFInteractiveHelper>();
                if (inters != null)
                    for (int j = 0; j < inters.Length; j++)
                        inters[j].sh = null;
            }
        }

        // Add child point
        void AddChild()
        {
            if (gizmo == RFGizmoType.Point)
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform tm = transform.GetChild(i);
                    RFInteractiveHelper inter = transform.GetChild (i).GetComponent<RFInteractiveHelper>();
                    if (inter == null)
                        AddInteractiveHelper (sh, tm, RFInterType.Cluster, gizmo);
                    else
                    {
                        inter.sh = sh;
                        inter.shatter = shatter;
                        inter.gizmo = gizmo;
                    }
                }
                
                sh.InteractiveCluster();
            }
        }
        
        /// /////////////////////////////////////////////////////////
        /// Other
        /// /////////////////////////////////////////////////////////

        // Prevent update by multiple children/selection
        void PreventClusterUpdate()
        {
            if (type == RFInterType.Cluster)
            {
                // Prevent multiple updates by children
                if (transform.childCount > 0)
                    for (int i = 0; i < transform.childCount; i++)
                        transform.GetChild (i).hasChanged = false;
                
                // Prevent multiple update by selection
                #if UNITY_EDITOR
                for (int i = 0; i < UnityEditor.Selection.transforms.Length; i++)
                    UnityEditor.Selection.transforms[i].hasChanged = false;
                #endif
            }
        }
        
        // Random offset for duplicated point helpers
        void RandomPointOffset()
        {
            if (type == RFInterType.Cluster && gizmo == RFGizmoType.Point && ofsState == false)
            {
                transform.Translate (new Vector3(Random.Range (ofsVal, -ofsVal),Random.Range (ofsVal, -ofsVal),Random.Range (ofsVal, -ofsVal)));
                ofsState = true;
            }
        }
    }
}