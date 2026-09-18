using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RayFire
{
    /// <summary>
    /// Rayfire advanced clustering data class.
    /// </summary>
    [Serializable]
    public class RFAcd
    {
        public static string[] options = new string[] {"Bound", "Solo", "Outer", "Inner", "Points"};
        public enum RFAcdType
        {
            Bound  = 0,
            Solo   = 1,
            Outer  = 2,
            Inner  = 3,
            Points = 4
        }

        public bool             st;
        public int              id;
        public GameObject       go;
        public RFAcdType        tp;
        public bool             gz;
        public Color            col;
        public List<MeshFilter> mfs;

        [NonSerialized] public Vector3                   scl;
        [NonSerialized] public Vector3                   cnt;
        [NonSerialized] public Vector3                   ext;
        [NonSerialized] public Quaternion                rot;
        [NonSerialized] public List<RFInteractiveHelper> hlp;

        public static int pointGroup = -1;
        public static int soloGroup  = 0;
        public static int innerGroup = 100;

        /// /////////////////////////////////////////////////////////
        /// Constructor
        /// /////////////////////////////////////////////////////////

        public RFAcd()
        {
            id  = 1;
            tp  = RFAcdType.Bound;
            gz  = true;
            col = new Color (1.0f, 0.5f, 0f, 0.4f);
            mfs = new List<MeshFilter>();
        }

        /// /////////////////////////////////////////////////////////
        /// Static
        /// /////////////////////////////////////////////////////////

        // Remove interactive helper component for volumes
        public static void VolumeGizmo(RFAcd acd)
        {
            if (acd.hlp != null)
                for (int h = acd.hlp.Count - 1; h >= 0; h--)
                    if (acd.go != null)
                        acd.hlp[h].gizmo = acd.gz == true ? RFInteractiveHelper.RFGizmoType.Box : RFInteractiveHelper.RFGizmoType.Disabled;
        }

        // Remove interactive helper component for volumes
        public static void ColorGizmo(RFAcd acd)
        {
            if (acd.hlp != null)
                for (int h = acd.hlp.Count - 1; h >= 0; h--)
                    if (acd.go != null)
                        acd.hlp[h].color = acd.col;
        }

        // Remove interactive helper component for volumes
        public static void RemoveVolumeHelpers(RFAcd acd)
        {
            if (acd.hlp != null)
                for (int h = acd.hlp.Count - 1; h >= 0; h--)
                    if (acd.go != null)
                        acd.hlp[h].shatter = null;
        }

        // Point to OBB check
        public static bool PointOBB(Vector3 point, Vector3 ex, Matrix4x4 matrix)
        {
            // Convert world matrix point to OBB matrix point
            point = matrix.inverse.MultiplyPoint3x4 (point);

            // Check if inside bounds
            return point.x <= ex.x && point.x > -ex.x && point.y <= ex.y && point.y > -ex.y && point.z <= ex.z && point.z > -ex.z;
        }

        // Frag mesh deep copy
        public static void GetAabbCenters(ref Vector3[][] aabbCenters, Tuple<Vector3, Vector3>[][] source, Matrix4x4 tn)
        {
            aabbCenters = new Vector3[source.Length][];
            for (int i = 0; i < source.Length; i++)
            {
                aabbCenters[i] = new Vector3[source[i].Length];
                for (int j = 0; j < source[i].Length; j++)
                    aabbCenters[i][j] = tn.inverse.MultiplyPoint (Vector3.Lerp (source[i][j].Item1, source[i][j].Item2, 0.5f));
            }
        }
        
        // Reset custom groups id
        public static void ResetCustomGroups(int[][] customGroups)
        {
            for (int i = 0; i < customGroups.Length; i++)
                for (int t = 0; t < customGroups[i].Length; t++)
                    customGroups[i][t] = pointGroup;
        }
        
        /// /////////////////////////////////////////////////////////
        /// Getters
        /// /////////////////////////////////////////////////////////
        
        // Group ID
        public int Id {
            get { return id; }
            set {
                id = value;
                if (id < 1)
                    id = 1; 
                if (id > 99)
                    id = 99; }}

        public int IdByType {
            get {
                if (tp == RFAcdType.Bound)
                    return id;
                if (tp == RFAcdType.Inner)
                    return innerGroup;
                if (tp == RFAcdType.Solo)
                    return soloGroup;
                if (tp == RFAcdType.Points)
                    return pointGroup;
                if (tp == RFAcdType.Outer)
                    return id;
                return pointGroup; }
        }
    }
    
    /// <summary>
    /// Rayfire Shatter pos fragmentation cluster class.
    /// </summary>
    [Serializable]
    public class RFShatterCluster
    {
        public bool  enable;
        public int   count;
        public int   seed;
        public float relax;
        public int   amount;
        public int   layers;
        public float scale;
        public int   min;
        public int   max;
        public bool  red;
        public bool  outer;
        public bool  inner;
        public bool  tsf;
        
        public List<RFAcd> acdList;
        public Transform   pointRoot; 

        /// /////////////////////////////////////////////////////////
        /// Constructor
        /// /////////////////////////////////////////////////////////
        
        public RFShatterCluster()
        {
            enable = false;
            count  = 10;
            seed   = 1;
            relax  = 0;
            layers = 0;
            amount = 0;
            scale  = 1f;
            min    = 1;
            max    = 3;
            red    = true;
            outer  = false;
            inner  = false;
            tsf    = false;
        }
        
        public RFShatterCluster (RFShatterCluster src)
        {
            enable = src.enable;
            count  = src.count;
            seed   = src.seed;
            relax  = src.relax;
            layers = src.layers;
            amount = src.amount;
            scale  = src.scale;
            min    = src.min;
            max    = src.max;
            red    = src.red;
            outer  = src.outer;
            inner  = src.inner;
            tsf    = src.tsf;
        }
        
        public static void Copy (RFShatterCluster trg, RFShatterCluster src)
        {
            trg.enable = src.enable;
            trg.count  = src.count;
            trg.seed   = src.seed;
            trg.relax  = src.relax;
            trg.layers = src.layers;
            trg.amount = src.amount;
            trg.scale  = src.scale;
            trg.min    = src.min;
            trg.max    = src.max;
            trg.red    = src.red;
            trg.outer  = src.outer;
            trg.inner  = src.inner;
            trg.tsf    = src.tsf;
        }
        
        /// /////////////////////////////////////////////////////////
        /// Acd
        /// /////////////////////////////////////////////////////////
        
        // Add acd
        public void AddAcd()
        {
            if (acdList == null)
                acdList = new List<RFAcd>();
            
            // Create new
            RFAcd acd = new RFAcd();
            
            // Pick set random color
            if (acdList.Count >= 1)
                acd.col = new Color(Random.Range (0.3f, 0.8f), Random.Range (0.3f, 0.8f), Random.Range (0.3f, 0.8f), acd.col.a);

            // Increment id
            acd.id = acdList.Count + 1;
            
            // Collect
            acdList.Add (acd); 
        }
        
        // Add acd
        public void RemoveAcd(int ind)
        {
            RFAcd.RemoveVolumeHelpers (acdList[ind]);
            acdList.RemoveAt (ind);
        }

        public int GetLowestAcdId()
        {
            int ind = 0;
            int id = 1000;
            for (int i = 0; i < acdList.Count; i++)
                if (acdList[i].st == false && acdList[i].id < id)
                {
                    id  = acdList[i].id;
                    ind = i;
                }
            
            acdList[ind].st = true;
            return ind;
        }
        
        /// /////////////////////////////////////////////////////////
        /// Static
        /// /////////////////////////////////////////////////////////
        
        // Reset acd list priority 
        public static void ResetPriorityState(List<RFAcd> acdList)
        {
            for (int a = 0; a < acdList.Count; a++)
                acdList[a].st = false;
        }
        
        /// /////////////////////////////////////////////////////////
        /// Getters
        /// /////////////////////////////////////////////////////////
        
        // Get seed
        public int Seed { get {
            if (seed == 0)
                return Random.Range (0, 1000);
            return seed;
        }}
        
        // Get Count
        public int Count { get {
            if (enable == false)
                return 0;
            return count;
        }}
        
        // Get Reduce
        public bool Reduce { get {
            if (enable == false)
                return false;
            return red;
        }}
        
        // Get Reduce
        public bool AcdState { get {
            if (acdList == null || acdList.Count == 0)
                return false;
            
            /*
            bool hasVolume = false;
            for (int i = 0; i < acdList.Count; i++)
            {
                if (acdList[i].go != null )
                {
                    hasVolume = true;
                    break;
                }
            }
            */
            
            return true;
        }}
        
        
      

    }
}

