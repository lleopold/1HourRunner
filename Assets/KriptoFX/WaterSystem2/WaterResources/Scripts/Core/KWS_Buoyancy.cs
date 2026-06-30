using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace KWS
{
    [RequireComponent(typeof(Rigidbody))]
    // [RequireComponent(typeof(Collider))]
    public class KWS_Buoyancy : MonoBehaviour
    {
        public ModelSourceEnum VolumeSource = ModelSourceEnum.Mesh;
        public Renderer    OverrideVolumeMesh;
        public MeshCollider    OverrideVolumeCollider;
        public Transform       OverrideCenterOfMass;

        [Range(50, 1000)] public float Density        = 500;
        [Range(1,  6)]    public int   SlicesPerAxisX = 2;
        [Range(1,  6)]    public int   SlicesPerAxisY = 2;
        [Range(1,  6)]    public int   SlicesPerAxisZ = 2;
        public                   bool  isConcave      = false;

        //[Range(2, 100)]
        //public int VoxelsLimit = 16;
        public float WaterDrag = 0.5f;

        public float Drag = 0.1f;
        // [Range(0, 1)]
        //public float NormalForce = 0.35f;

        [Range(0, 30f)] public float VelocityForce = 10f;

        private const float DAMPFER       = 0.1f;
        private const float WATER_DENSITY = 950;

        private Vector3       localArchimedesForce;
        private List<Vector3> voxels;
        private Vector3[]     _voxelsWorldPos;

        private bool            isMeshCollider;
        private List<Vector3[]> debugForces; // For drawing force gizmos

        private Rigidbody _rigidBody;
        private Collider  _collider;

        private float bounceMaxSize;
        private float bounceMaxHeight;

        private int _lastSlicesPerAxisX;
        private int _lastSlicesPerAxisY;
        private int _lastSlicesPerAxisZ;

        public enum ModelSourceEnum
        {
            Collider,
            Mesh
        }

        /// <summary>
        /// Provides initialization.
        /// </summary>
        private void OnEnable()
        {
            debugForces = new List<Vector3[]>(); // For drawing force gizmos

            _rigidBody = GetComponent<Rigidbody>();
            _collider  = GetComponent<Collider>();


            // Store original rotation and position
            var originalRotation = transform.rotation;
            var originalPosition = transform.position;
            transform.rotation = Quaternion.identity;
            transform.position = Vector3.zero;

            try
            {
                isMeshCollider = GetComponent<MeshCollider>() != null;


                if (OverrideCenterOfMass) _rigidBody.centerOfMass = transform.InverseTransformPoint(OverrideCenterOfMass.transform.position);
                //else rigidBody.centerOfMass = new Vector3(0, -bounds.extents.y * 0f, 0) + transform.InverseTransformPoint(bounds.center);

                voxels = SliceIntoVoxels(isMeshCollider && isConcave);

            }
            finally
            {
                // Restore original rotation and position
                transform.rotation = originalRotation;
                transform.position = originalPosition;
            }
            
         

            float volume = _rigidBody.mass / Density;

            //WeldPoints(voxels, VoxelsLimit);

            float archimedesForceMagnitude = WATER_DENSITY * Mathf.Abs(Physics.gravity.y) * volume;
            localArchimedesForce = new Vector3(0, archimedesForceMagnitude, 0) / voxels.Count;

            _voxelsWorldPos = new Vector3[voxels.Count];
        }

        private void OnDisable()
        {
        }

        Bounds GetCurrentBounds()
        {
            Bounds bounds = new Bounds();
            if (VolumeSource == ModelSourceEnum.Mesh)
            {
                bounds = OverrideVolumeMesh != null ? OverrideVolumeMesh.bounds : GetComponent<Renderer>().bounds;
            }
            else if (VolumeSource == ModelSourceEnum.Collider)
            {
                var meshCollider = OverrideVolumeCollider != null ? OverrideVolumeCollider : GetComponent<MeshCollider>();

                if (meshCollider != null)
                {
                    bounds = meshCollider.sharedMesh.bounds;
                }
                else bounds = GetComponent<Collider>().bounds;
            }

            return bounds;
        }

        private List<Vector3> SliceIntoVoxels(bool concave)
        {
            var points = new List<Vector3>(SlicesPerAxisX * SlicesPerAxisY * SlicesPerAxisZ);

            var bounds = GetCurrentBounds();
            bounceMaxSize   = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            bounceMaxHeight = bounds.size.y * 0.5f + bounds.center.y;

            if (concave)
            {
                var meshCol = OverrideVolumeCollider ? OverrideVolumeCollider : GetComponent<MeshCollider>();

                var convexValue = meshCol.convex;
                meshCol.convex = false;

                // Concave slicing

                for (int ix = 0; ix < SlicesPerAxisX; ix++)
                {
                    for (int iy = 0; iy < SlicesPerAxisY; iy++)
                    {
                        for (int iz = 0; iz < SlicesPerAxisZ; iz++)
                        {
                            float x = bounds.min.x + bounds.size.x / SlicesPerAxisX * (0.5f + ix);
                            float y = bounds.min.y + bounds.size.y / SlicesPerAxisY * (0.5f + iy);
                            float z = bounds.min.z + bounds.size.z / SlicesPerAxisZ * (0.5f + iz);

                            var p = transform.InverseTransformPoint(new Vector3(x, y, z));

                            if (PointIsInsideMeshCollider(meshCol, p))
                            {
                                points.Add(p);
                            }
                        }
                    }
                }

                if (points.Count == 0)
                {
                    points.Add(bounds.center);
                }

                meshCol.convex = convexValue;
            }
            else
            {
                // Convex slicing

                for (int ix = 0; ix < SlicesPerAxisX; ix++)
                {
                    for (int iy = 0; iy < SlicesPerAxisY; iy++)
                    {
                        for (int iz = 0; iz < SlicesPerAxisZ; iz++)
                        {
                            float x = bounds.min.x + bounds.size.x / SlicesPerAxisX * (0.5f + ix);
                            float y = bounds.min.y + bounds.size.y / SlicesPerAxisY * (0.5f + iy);
                            float z = bounds.min.z + bounds.size.z / SlicesPerAxisZ * (0.5f + iz);

                            var p = transform.InverseTransformPoint(new Vector3(x, y, z));

                            points.Add(p);
                        }
                    }
                }
            }

            return points;
        }

        private static bool PointIsInsideMeshCollider(Collider c, Vector3 p)
        {
            Vector3[] directions = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };

            foreach (var ray in directions)
            {
                RaycastHit hit;
                if (c.Raycast(new Ray(p - ray * 1000, ray), out hit, 1000f) == false)
                {
                    return false;
                }
            }

            return true;
        }


        private static void FindClosestPoints(IList<Vector3> list, out int firstIndex, out int secondIndex)
        {
            float minDistance = float.MaxValue, maxDistance = float.MinValue;
            firstIndex  = 0;
            secondIndex = 1;

            for (int i = 0; i < list.Count - 1; i++)
            {
                for (int j = i + 1; j < list.Count; j++)
                {
                    float distance = Vector3.Distance(list[i], list[j]);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        firstIndex  = i;
                        secondIndex = j;
                    }

                    if (distance > maxDistance)
                    {
                        maxDistance = distance;
                    }
                }
            }
        }


        private static void WeldPoints(IList<Vector3> list, int targetCount)
        {
            if (list.Count <= 2 || targetCount < 2)
            {
                return;
            }

            while (list.Count > targetCount)
            {
                int first, second;
                FindClosestPoints(list, out first, out second);

                var mixed = (list[first] + list[second]) * 0.5f;
                list.RemoveAt(second); // the second index is always greater that the first => removing the second item first
                list.RemoveAt(first);
                list.Add(mixed);
            }
        }

        private WaterSurfaceRequestArray _request = new WaterSurfaceRequestArray();
        private float[]                  _prevWaterLevel;
        private float[]                  _velK;

        private void FixedUpdate()
        {
#if UNITY_EDITOR
            debugForces.Clear();
#endif

            if (_prevWaterLevel == null || _prevWaterLevel.Length != _voxelsWorldPos.Length)
            {
                _prevWaterLevel = new float[_voxelsWorldPos.Length];
                _velK           = new float[_voxelsWorldPos.Length];
            }

            // Update voxel world-space positions
            for (int i = 0; i < _voxelsWorldPos.Length; i++)
                _voxelsWorldPos[i] = transform.TransformPoint(voxels[i]);

            _request.SetNewPositions(_voxelsWorldPos);
            WaterSystem.TryGetWaterSurfaceData(_request);

            if (!_request.IsDataReady)
            {
                _rigidBody.Sleep();
                return;
            }
            else _rigidBody.WakeUp();

            var pointsUnderwater = 0f;

            for (int i = 0; i < _voxelsWorldPos.Length; i++)
            {
                var wp            = _voxelsWorldPos[i];
                var sd            = _request.Result[i];
                var waterPos      = sd.Position;
                var waterVelocity = sd.Velocity;

                // Smooth water level to avoid sudden jumps from wave height changes
                float smoothedWaterY = Mathf.SmoothDamp(_prevWaterLevel[i], waterPos.y, ref _velK[i], 0.05f);
                _prevWaterLevel[i] = smoothedWaterY;

                float k = smoothedWaterY - wp.y; // submersion depth (>0 = under water)

                Vector3 pointVel = _rigidBody.GetPointVelocity(wp);

                float   volume                   = _rigidBody.mass                             / Density;
                float   archimedesForceMagnitude = WATER_DENSITY                               * Mathf.Abs(Physics.gravity.y) * volume;
                Vector3 archimedesPerVoxel       = new Vector3(0, archimedesForceMagnitude, 0) / voxels.Count;

                // --- Flow interaction (rivers, moving water) ---
                const float maxVelocityDepth = 5f;
                float       flowDepthAtten   = (k > 0) ? (1f - Mathf.Clamp01(k / maxVelocityDepth)) : 0f; // strong near surface, weak deeper
                Vector3     flowXZ           = new Vector3(waterVelocity.x, 0f, waterVelocity.z);
                Vector3     relative         = pointVel - flowXZ * (VelocityForce * flowDepthAtten);

                // Base damping — pushes velocity toward water flow velocity
                Vector3 damping = -relative * (DAMPFER * _rigidBody.mass / voxels.Count);

                Vector3 force = damping;

                if (k > 0)
                {
                    pointsUnderwater++;

                    // Buoyancy
                    float depthFactor = Mathf.Clamp01(k / 1.5f);
                    force += archimedesPerVoxel * Mathf.Sqrt(depthFactor);

                    // Soft vertical damping to reduce wave jitter but keep inertia
                    Vector3 verticalVel = Vector3.Project(pointVel, Vector3.up);
                    force -= verticalVel * 0.5f;
                }
                else
                {
                    // In air — apply lighter drag
                    force *= 0.4f;
                }

                // Limit force to prevent extreme impulses
                force = Vector3.ClampMagnitude(force, archimedesPerVoxel.y * 3f);

                _rigidBody.AddForceAtPosition(force, wp);

#if UNITY_EDITOR
                if (_isSelectedFrames > 0) debugForces.Add(new[] { wp, force });
#endif
            }

            float underwaterFactor = pointsUnderwater / voxels.Count;
            _rigidBody.drag = Mathf.Lerp(Drag, WaterDrag, underwaterFactor);
        }


        private void OnValidate()
        {
            if (_lastSlicesPerAxisX != SlicesPerAxisX ||
                _lastSlicesPerAxisY != SlicesPerAxisY ||
                _lastSlicesPerAxisZ != SlicesPerAxisZ)
            {
                OnEnable();
            }
        }

#if UNITY_EDITOR
        private int _isSelectedFrames;
        private void OnDrawGizmosSelected()
        {
            _isSelectedFrames = 5;

            if (voxels == null || debugForces == null)
            {
                return;
            }

            float gizmoSize = 0.1f * bounceMaxSize;

            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
            Handles.color = Color.yellow;

            foreach (var p in voxels)
            {
                Handles.SphereHandleCap(0, transform.TransformPoint(p), Quaternion.identity, gizmoSize, EventType.Repaint);
            }

            Handles.color = Color.cyan;

            foreach (var force in debugForces)
            {
                Handles.DrawLine(force[0], force[0] + (force[1] / _rigidBody.mass) * bounceMaxSize * 0.25f);
            }
        }
#endif
    }
}