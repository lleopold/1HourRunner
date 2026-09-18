using System;
using System.Collections.Generic;
using UnityEngine;

namespace RayFire
{
    [AddComponentMenu (RFLog.sht_path)]
    [HelpURL (RFLog.sht_link)]
    public class RayfireShatter : MonoBehaviour
    {
        // UI
        public FragType          type;
        public RFVoronoi         voronoi;
        public RFSplinters       splinters;
        public RFSplinters       slabs;
        public RFRadial          radial;
        public RFHexagon         hexagon;
        public RFCustom          custom;
        public RFMirrored        mirrored;
        public RFSlice           slice;
        public RFBricks          bricks;
        public RFVoxels          voxels;
        public RFTets            tets;
        public RFSurface         material;
        public RFShatterCluster  clusters;
        public RFShatterAdvanced advanced;
        public RFShell           shell;
        
        // Components
        public MeshRenderer        meshRenderer;
        public SkinnedMeshRenderer skinnedMeshRend;
        
        // Hidden
        public bool    colorPreview;
        public bool    scalePreview = true;
        public float   previewScale;
        public Bounds  bound;
        public bool    resetState;

        // Interactive
        [NonSerialized] public bool             interactive;
        public                 List<MeshFilter> intMfs;
        public                 List<Renderer>   intMrs;
        
        // RFEngine props
        public RFEngine                       engine;
        public List<RFShatterBatch>           batches;
        
        /// /////////////////////////////////////////////////////////
        /// Getters
        /// /////////////////////////////////////////////////////////
        
        public Vector3    CenterPos  { get { return advanced.CanUseCenter == true ? advanced.centerBias.transform.position : transform.position; }}
        public Quaternion CenterDir  { get { return advanced.CanUseCenter == true ? advanced.centerBias.transform.rotation : transform.rotation; }}

        /// /////////////////////////////////////////////////////////
        /// Constructor
        /// /////////////////////////////////////////////////////////
        
        public RayfireShatter()
        {
            type      = FragType.Voronoi;
            voronoi   = new RFVoronoi();
            splinters = new RFSplinters();
            slabs     = new RFSplinters();
            radial    = new RFRadial();
            hexagon   = new RFHexagon();
            custom    = new RFCustom();
            mirrored  = new RFMirrored();
            slice     = new RFSlice();
            bricks    = new RFBricks();
            voxels    = new RFVoxels();
            tets      = new RFTets();
            material  = new RFSurface();
            clusters  = new RFShatterCluster();
            advanced  = new RFShatterAdvanced();
            shell     = new RFShell();
            batches   = new List<RFShatterBatch>();
        }
        
        /// /////////////////////////////////////////////////////////
        /// Common
        /// /////////////////////////////////////////////////////////
        
        // Reset
        private void Reset()
        {
            InteractiveStop();
        }
        
        /// /////////////////////////////////////////////////////////
        /// Methods
        /// /////////////////////////////////////////////////////////
        
        // Get bounds
        public Bounds GetBound()
        {
            // Mesh renderer
            if (meshRenderer == null)
            {
                meshRenderer = GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                    return meshRenderer.bounds;
            }
            else
                return meshRenderer.bounds;

            // Skinned mesh
            if (skinnedMeshRend == null)
            {
                skinnedMeshRend = GetComponent<SkinnedMeshRenderer>();
                if (skinnedMeshRend != null)
                    return skinnedMeshRend.bounds;
            }

            return new Bounds();
        }

        /// /////////////////////////////////////////////////////////
        /// Methods
        /// /////////////////////////////////////////////////////////

        // Fragment this object by shatter properties  List<GameObject>
        public void Fragment()
        {
            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();
            
            // Engine
            RFEngine.FragmentShatter (this);
            
            stopWatch.Stop();
            RayfireMan.Log(name + RFLog.sht_time + stopWatch.Elapsed.TotalMilliseconds.ToString("F2") + " ms");
        }

        // Fragment by limitations
        public void LimitationFragment(RFShatterBatch batch, int ind)
        {
            RayfireShatter shat = batch.fragments[ind].gameObject.AddComponent<RayfireShatter>();
            shat.voronoi.amount = 10;

            shat.Fragment();

            if (shat.batches[0].fragments.Count > 0)
            {
                // Reparent new frags
                foreach (var frag in batch.fragments)
                    frag.transform.parent = batch.fragments[ind].parent;
                
                // Add to source batch and remove original
                batch.fragments.AddRange (shat.batches[0].fragments);
                batch.fragments.RemoveAt (ind);
                
                // Destroy original and new frags parent
                DestroyImmediate (shat.batches[0].fragRoot.gameObject);
                DestroyImmediate (shat.gameObject);
            }
        }
        
        /// /////////////////////////////////////////////////////////
        /// Copy
        /// /////////////////////////////////////////////////////////

        // Copy shatter component
        public static void CopyRootMeshShatter(RayfireRigid source, List<RayfireRigid> targets)
        {
            // No shatter
            if (source.mshDemol.sht == null)
                return;

            // Copy shatter
            for (int i = 0; i < targets.Count; i++)
            {
                targets[i].mshDemol.sht = targets[i].gameObject.AddComponent<RayfireShatter>();
                targets[i].mshDemol.sht.CopyFrom (source.mshDemol.sht);
            }
        }

        // Copy from
        void CopyFrom(RayfireShatter shatter)
        {
            type = shatter.type;

            voronoi   = new RFVoronoi (shatter.voronoi);
            splinters = new RFSplinters (shatter.splinters);
            slabs     = new RFSplinters (shatter.slabs);
            radial    = new RFRadial (shatter.radial);
            custom    = new RFCustom (shatter.custom);
            slice     = new RFSlice (shatter.slice);
            tets      = new RFTets (shatter.tets);
            
            RFSurface.Copy (material, shatter.material);
            RFShatterCluster.Copy (clusters, shatter.clusters);
            RFShatterAdvanced.Copy (advanced, shatter.advanced);
        }

        /// /////////////////////////////////////////////////////////
        /// Interactive
        /// /////////////////////////////////////////////////////////
        
        // Interactive methods
        public void InteractiveStart()    { RFEngine.InteractiveStart (this);; }
        public void InteractiveChange()
        {
            //System.Diagnostics.Stopwatch stopWatch = RayfireMan.WatchStart();
            RFEngine.InteractiveChange (this);
            //RayfireMan.WatchStop(stopWatch, "Interactive Change ", gameObject);
        }
        public void InteractiveCluster()
        {
            //System.Diagnostics.Stopwatch stopWatch = RayfireMan.WatchStart();
            RFEngine.InteractiveCluster (this);
            //RayfireMan.WatchStop(stopWatch, "Interactive Cluster ", gameObject);
        }
        public void InteractiveScale()    { RFEngine.InteractiveScale (this); }
        public void InteractiveFragment() { RFEngine.InteractiveFragment(this); }
        public void InteractiveStop()     { RFEngine.InteractiveStop (this); }
        
        // Final preview scale
        public float PreviewScale()
        {
            return scalePreview == false ? 1f : Mathf.Lerp (1f, 0.3f, previewScale);
        }
        
        /// /////////////////////////////////////////////////////////
        /// Getters
        /// /////////////////////////////////////////////////////////
        
        public bool HasBatches { get { return batches != null && batches.Count > 0; }}
        public bool HasPoints { get { return clusters.pointRoot != null && clusters.pointRoot.childCount > 1; }}
    }
}