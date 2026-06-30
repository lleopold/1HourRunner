using UnityEngine;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace KWS
{
    [ExecuteInEditMode]
    public class KWS_LocalWaterZone : MonoBehaviour, KWS_TileZoneManager.IWaterZone
    {
        public bool OverrideColorSettings = true;
        
        public float Transparent = 4;
        public Color WaterColor       = Color.white;
        public Color TurbidityColor   = new Color(159 / 255.0f, 59 / 255.0f, 0 / 255.0f);
        public bool UseSphericalBlending = false;
        
        public bool OverrideWindSettings = false;
        public float WindStrengthMultiplier = 0.05f;
        public float WindEdgeBlending = 0.75f;
/*
        public bool  OverrideRainSettings   = false;
        public float RainStrengthMultiplier = 0.25f;
        public float RainEdgeBlending       = 0.75f;
 */       
        public bool  OverrideHeight         = false;
        public float HeightEdgeBlending     = 1.0f;
        public bool  ClipWaterBelowZone     = false;
        public Mesh  ClipMesh;

        public bool    OverrideMesh;
        public Mesh    CustomMesh;
        
        public bool    UseClipMask;

        [SerializeField] internal bool                                        ShowColorSettings  = true;
        [SerializeField] internal bool                                        ShowWindSettings   = true;
        [SerializeField] internal bool                                        ShowHeightSettings = true;
        [SerializeField] internal bool                                        ShowMeshSettings   = true;
        [SerializeField] internal bool                                        ShowClipSettings   = true;
        
        public                    Vector3                                     Position                => CachedAreaPos;
        public                    Vector3                                     Size                    => CachedAreaSize;
        public                    Quaternion                                  Rotation                => CachedRotation; 
        public                    Vector4                                     RotationMatrix          => CachedRotationMatrix;
        public                    Bounds                                      Bounds                  => CachedBounds;
        public                    bool                                        IsZoneVisible           => _IsZoneVisible;
        public                    bool                                        IsZoneInitialized       => _isZoneInitialized;
        public                    float                                       ClosetsDistanceToCamera { get; set; }
        int KWS_TileZoneManager.IWaterZone.                                   ID                      { get; set; }
        Bounds KWS_TileZoneManager.IWaterZone.                                OrientedBounds          => CachedOrientedBounds;
        KWS_TileZoneManager.PrecomputedOBBZone KWS_TileZoneManager.IWaterZone.PrecomputedObbZone      => _precomputedObbZone;

        // bool 

        private  Vector3    CachedAreaPos;
        private  Vector3    CachedAreaSize;
        private  Quaternion CachedRotation;
        private  Bounds     CachedBounds;
        private  Bounds     CachedOrientedBounds;
        private  Vector4    CachedRotationMatrix;
        internal Matrix4x4  CachedFittedMatrix;
        internal Matrix4x4  CachedClipMatrix;

        KWS_TileZoneManager.PrecomputedOBBZone _precomputedObbZone;

        private bool _IsZoneVisible;
        bool _isZoneInitialized;
        
        Vector3         _lastPosition;
        Quaternion      _lastRotation;
        Vector3         _lastScale;

        void KWS_TileZoneManager.IWaterZone.UpdateVisibility(Camera cam)
        {
            _IsZoneVisible = false;

            if (!KWS_UpdateManager.FrustumCaches.TryGetValue(cam, out var cache))
            {
                return;
            }

            var planes = cache.FrustumPlanes;
            var min    = Bounds.min;
            var max    = Bounds.max;
            
            if (OverrideHeight)
            {
                float waterLevel = WaterSystem.Instance.WaterLevel;
                min.y = Mathf.Min(min.y, waterLevel);
                max.y = Mathf.Max(max.y, waterLevel + WaterSystem.Instance.CurrentMaxHeightOffsetRelativeToWaterLevel);
            }
            
            _IsZoneVisible = KW_Extensions.IsBoxVisibleApproximated(ref planes, min, max);
        }

        internal bool AllOverridesDisabled ()
        {
            if (OverrideColorSettings || OverrideWindSettings || OverrideHeight) return false;
            return true;
        }

        internal void UpdateTransform()
        {
            var t = transform;

            if (OverrideColorSettings)
            {
                var angles                    = transform.rotation.eulerAngles;
                angles.x           = angles.z = 0;
                transform.rotation = Quaternion.Euler(angles);
            }

            CachedAreaPos  = t.position;
            CachedAreaSize = t.localScale;
            CachedRotation = t.rotation;
            
            if (OverrideMesh && CustomMesh)
            {
                CachedFittedMatrix = GetFittedWorldMatrix();
                var customMeshBounds = CustomMesh.bounds;
                CachedAreaPos  = transform.TransformPoint(customMeshBounds.center);
                CachedAreaSize = Vector3.Scale(customMeshBounds.size, transform.lossyScale);
            }
            if (UseClipMask && ClipMesh)
            {
                CachedClipMatrix = GetClipWorldMatrix();
                var clipMeshBounds = ClipMesh.bounds;
                CachedAreaPos  = transform.TransformPoint(clipMeshBounds.center);
                CachedAreaSize = Vector3.Scale(clipMeshBounds.size, transform.lossyScale);
            }

            CachedBounds         = new Bounds(CachedAreaPos, CachedAreaSize);
            CachedOrientedBounds = KW_Extensions.GetOrientedBounds(CachedAreaPos, CachedAreaSize, CachedRotation);

            var angleRad = CachedRotation.eulerAngles.y * Mathf.Deg2Rad;
            var cos      = Mathf.Cos(angleRad);
            var sin      = Mathf.Sin(angleRad);
            CachedRotationMatrix = new Vector4(cos, sin, -sin, cos);
            
            
            
            CachePrecomputedOBBZone();
            
            _isZoneInitialized = true;
        }

        internal void CachePrecomputedOBBZone()
        {
            var     bounds   = Bounds;
            Vector2 center   = new Vector2(bounds.center.x, bounds.center.z);
            Vector2 halfSize = new Vector2(bounds.size.x, bounds.size.z) * 0.5f;
        

            float angleRad = transform.rotation.eulerAngles.y * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angleRad);
            float sin = Mathf.Sin(angleRad);
            Vector4 rotMatrix = new Vector4(cos, -sin, sin, cos);

            _precomputedObbZone = new KWS_TileZoneManager.PrecomputedOBBZone();
            _precomputedObbZone.Center = center;
            _precomputedObbZone.Axis = new Vector2[2];
            _precomputedObbZone.Axis[0] = new Vector2(rotMatrix.x, rotMatrix.y); // right
            _precomputedObbZone.Axis[1] = new Vector2(rotMatrix.z, rotMatrix.w); // forward
            _precomputedObbZone.HalfSize = halfSize;
            _precomputedObbZone.RotMatrix = rotMatrix;

            Vector2 bX = new Vector2(1, 0);
            Vector2 bY = new Vector2(0, 1);

            _precomputedObbZone.Extents    =  new float[2];
            _precomputedObbZone.Extents[0] =  Mathf.Abs(Vector2.Dot(_precomputedObbZone.Axis[0] * halfSize.x, bX)) + Mathf.Abs(Vector2.Dot(_precomputedObbZone.Axis[1] * halfSize.y, bX));
            _precomputedObbZone.Extents[1] =  Mathf.Abs(Vector2.Dot(_precomputedObbZone.Axis[0] * halfSize.x, bY)) + Mathf.Abs(Vector2.Dot(_precomputedObbZone.Axis[1] * halfSize.y, bY));
        }

        Matrix4x4 GetFittedWorldMatrix()
        {/*
            var qLocal = Quaternion.Euler(CustomMeshRotationOffset);
            var b   = CustomMesh.bounds;
            var ext = b.extents;          
            var R       = Matrix4x4.Rotate(qLocal);

            float ax00 = Mathf.Abs(R.m00), ax01 = Mathf.Abs(R.m01), ax02 = Mathf.Abs(R.m02);
            float ax10 = Mathf.Abs(R.m10), ax11 = Mathf.Abs(R.m11), ax12 = Mathf.Abs(R.m12);
            float ax20 = Mathf.Abs(R.m20), ax21 = Mathf.Abs(R.m21), ax22 = Mathf.Abs(R.m22);

            var rotExt = new Vector3(
                ax00 * ext.x + ax01 * ext.y + ax02 * ext.z,
                ax10 * ext.x + ax11 * ext.y + ax12 * ext.z,
                ax20 * ext.x + ax21 * ext.y + ax22 * ext.z);

            var rotSize = rotExt * 2f;       

            var scale = new Vector3(
                Size.x / rotSize.x,
                Size.y / rotSize.y,
                Size.z / rotSize.z);

            var M = Matrix4x4.Scale(scale) * R * Matrix4x4.Translate(-b.center);

            return Matrix4x4.TRS(Position, Rotation, Vector3.one) * M;
            */
            return transform.localToWorldMatrix;
        }
        
        Matrix4x4 GetClipWorldMatrix()
        {
            return transform.localToWorldMatrix;
        }

      
        void OnEnable()
        {
            transform.hasChanged = false;
            _IsZoneVisible       = false;
            _isZoneInitialized   = false;

            UpdateTransform();
            KWS_TileZoneManager.LocalWaterZones.Add(this);
        }

        void Update()
        {
            var t = transform;

            if (KW_Extensions.AproximatedEqual(_lastPosition, t.position)   && 
                KW_Extensions.AproximatedEqual(_lastRotation, t.rotation)   &&
                KW_Extensions.AproximatedEqual(_lastScale ,   t.localScale))
            {
               
            }
            else
            {
                UpdateTransform();

                _lastPosition                 = t.position;
                _lastRotation                 = t.rotation;
                _lastScale                    = t.localScale;
               
            }
        }

        void OnDisable()
        {
            KWS_TileZoneManager.LocalWaterZones.Remove(this);

            ReleaseTextures();
        }

        void OnDrawGizmosSelected()
        {
            if (OverrideColorSettings && UseSphericalBlending)
            {
                transform.localScale = Vector3.one * transform.localScale.x;
            }

            Gizmos.matrix = Matrix4x4.TRS(CachedAreaPos, CachedRotation, CachedAreaSize);

            Gizmos.color = new Color(0.85f, 0.85f, 0.2f, 0.99f);
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);

            //Gizmos.color = new Color(0.85f, 0.85f, 0.2f, 0.03f);
            //Gizmos.DrawCube(Vector3.zero, Vector3.one);

            if (transform.hasChanged)
            {
                transform.hasChanged = false;
                UpdateTransform();
            }

            if (UseClipMask && ClipMesh != null)
            {
                var worldMatrix = KW_Extensions.GetFittedWorldMatrix(Position, Rotation, Size, ClipMesh);
                Gizmos.matrix = worldMatrix;
                Gizmos.color  = new Color(0.85f, 0.85f, 0.2f, 0.03f);
                Gizmos.DrawWireMesh(ClipMesh, Vector3.zero, Quaternion.identity, Vector3.one); 
            }


        }


        void OnDrawGizmos()
        {   
            if (OverrideColorSettings && UseSphericalBlending)
            {
                transform.localScale = Vector3.one * transform.localScale.x;
            }
            
            Gizmos.matrix = Matrix4x4.TRS(CachedAreaPos, CachedRotation, CachedAreaSize);

            Gizmos.color = new Color(0.15f, 0.85f, 0.2f, 0.99f);
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }
        void ReleaseTextures()
        {
           
        }

    }
}