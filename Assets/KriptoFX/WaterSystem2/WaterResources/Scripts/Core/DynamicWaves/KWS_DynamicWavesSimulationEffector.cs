using System;
using UnityEngine;


namespace KWS
{
    [ExecuteInEditMode]
    public class KWS_DynamicWavesSimulationEffector : MonoBehaviour
    {
        public InteractionTypeEnum InteractionType = InteractionTypeEnum.WaterSource;
        public ForceTypeEnum       ForceType       = ForceTypeEnum.Sphere;

        public bool UseWaterSurfaceIntersection = false;
        public bool ActivateWhenIntersectsWater;

        public float MotionForce   = 2.0f;
        public float ConstantForce = 0.0f;

        public float ConstantFlowRate = 0.1f;

        public float ConstantDrainRate = 0.1f;

        public bool    UseMeshFilterAsSource = true;
        public Mesh    OverrideObstacleMesh;
        public Vector3 MeshOffset = Vector3.zero;
        public Vector3 MeshScale  = Vector3.one;


        public float DirectionalVelocity         = 0.0f;
        public bool UseRotationForce = false;
        
        public bool  UseSourceColor = false;
        public Color SourceColor    = new Color(0.2f, 0.0f, 0.01f, 1);

        public bool UseCustomForce = false;


        internal Mesh     CurrentMesh => UseMeshFilterAsSource ? _renderMesh : OverrideObstacleMesh;

        internal bool IsObstacleBoundsIntersectWater
        {
            get
            {
                if (ActivateWhenIntersectsWater) return _isObstacleBoundsIntersectWater;
                else return true;
            }
        }

        internal DynamicWaveDataStruct DynamicWaveData;

        
        private float _customForce;
        Vector3       _customForceDirection;

        private float   _force;
        private Vector3 _relativePos;
        private Vector3 _relativeScale;
        private Vector3 _lastPos;
        private Vector3 _lastRotation;
        Quaternion _lastRotationQ;
        private Mesh    _renderMesh;
        private float   _lastTime;
        private float   _timeDelta;
        
        Renderer      _renderer;
        bool          _isObstacleBoundsIntersectWater;
        internal float _obstacleIntersectionAlphaFade = 1;

        static  Mesh _triangleMesh;

        public enum InteractionTypeEnum
        {
            WaterSource,
            WaterDrain,
            ForceObject,
            ObstacleObject
        }

        public enum ForceTypeEnum
        {
            Sphere,
            Box,
            Triangle
        }


        //don't forget about 32-bit pad!
        public struct DynamicWaveDataStruct
        {
            public uint  ZoneInteractionType;
            public float Force;
            public float WaterHeight;
            public uint  UseColor;

            public Vector4 Size;
            public Vector4 Position;

            public Vector3 ForceDirection;
            public uint    UseWaterIntersection;

            public Vector4   Color;
            public Matrix4x4 MatrixTRS;
        }

        Transform _t;


        void OnEnable()
        {
            _t           = transform;
            _t.hasChanged = false;
            KWS_TileZoneManager.DynamicWavesEffectors.Add(this);


            var meshFilter              = GetComponent<MeshFilter>();
            if (meshFilter) _renderMesh = meshFilter.sharedMesh;
            if (InteractionType == InteractionTypeEnum.ObstacleObject && UseMeshFilterAsSource && !meshFilter) Debug.LogError(name + " (KWS_DynamicWavesObstacle) Can't find the mesh filter");

            if (ActivateWhenIntersectsWater && InteractionType == InteractionTypeEnum.ObstacleObject)
            {
                _obstacleIntersectionAlphaFade = 0;
                _renderer                      = GetComponent<Renderer>();
            }
            else
            {
                _obstacleIntersectionAlphaFade = 1;
            }

            _lastPos     = _relativePos = _t.TransformPoint(MeshOffset);
            _force       = 0;
            _customForce = 0;
            _lastTime    = KW_Extensions.TotalTime();
            
            UpdateData(1, forceUpdate: false);
            
        }

        void OnDisable()
        {
            KWS_TileZoneManager.DynamicWavesEffectors.Remove(this);

            KWS_DynamicWavesHelpers.Release();

            if (KWS_TileZoneManager.DynamicWavesEffectors.Count == 0)
            {
                KW_Extensions.SafeDestroy(_triangleMesh);
                _triangleMesh = null;
            }
            
            _lastPos     = _relativePos = _t.TransformPoint(MeshOffset);
            _force       = 0;
            _customForce = 0;
        }


        bool IsTransformChanged(Transform t)
        {
            if (t.hasChanged)
            {
                t.hasChanged = false;
                return true;
            }

            return false;
        }


        void UpdateData(float frames, bool forceUpdate)
        {
            
            if (ActivateWhenIntersectsWater && InteractionType == InteractionTypeEnum.ObstacleObject)
            {
                CheckColliderToWaterIntersection();
                if (_isObstacleBoundsIntersectWater == false) return;
                
            }
            
            float     currentForce          = 0;
            Vector3   currentForceDirection = Vector3.up;

            if (InteractionType == InteractionTypeEnum.ObstacleObject) _relativePos = _t.TransformPoint(MeshOffset);
            else _relativePos                                                       = _t.position;
            
            if (UseCustomForce)
            {
                currentForce          = _customForce;
                currentForceDirection = _customForceDirection;
            }
            else
            {
                switch (InteractionType)
                {
                    case InteractionTypeEnum.WaterDrain:
                        UpdateInteractionTypeSource(out currentForce, out currentForceDirection);
                        break;
                    case InteractionTypeEnum.WaterSource:
                        UpdateInteractionTypeSource(out currentForce, out currentForceDirection);
                        break;
                    case InteractionTypeEnum.ForceObject:
                        UpdateInteractionTypeForce(frames, forceUpdate, out currentForce, out currentForceDirection);
                        currentForce = 0; //force object should use forceVelocity instead
                        break;
                    case InteractionTypeEnum.ObstacleObject:
                        UpdateInteractionTypeDynamicObject(out currentForce);
                        break;

                }
            }
          
            DynamicWaveData.Position             = _relativePos;
            DynamicWaveData.Size                 = Vector3.Scale(_t.lossyScale, MeshScale);
            
            DynamicWaveData.Force                = currentForce;
            DynamicWaveData.ForceDirection       = currentForceDirection;
            DynamicWaveData.UseWaterIntersection = (uint)(UseWaterSurfaceIntersection ? 1 : 0);
            DynamicWaveData.Color                = SourceColor;
            DynamicWaveData.UseColor             = UseSourceColor ? 1u : 0u;
          
            if (InteractionType == InteractionTypeEnum.ObstacleObject)
            {
                DynamicWaveData.MatrixTRS = Matrix4x4.TRS(_relativePos, transform.rotation, Vector3.Scale(_t.lossyScale, MeshScale));
            }
            else
            {
                DynamicWaveData.MatrixTRS = _t.localToWorldMatrix;
            }

           
        }

        
        void UpdateInteractionTypeSource(out float currentForce, out Vector3 currentForceDirection)
        {
            currentForceDirection = Vector3.up;
            _relativePos          = _t.position;
            _lastPos              = _relativePos;
            
            currentForce = InteractionType switch
            {
                InteractionTypeEnum.WaterSource => ConstantFlowRate,
                InteractionTypeEnum.WaterDrain  => -ConstantDrainRate,
                _                               => 0f
            };
            
            currentForceDirection = Vector3.Lerp(currentForceDirection, _t.forward * (DirectionalVelocity), Mathf.Abs(DirectionalVelocity)) * 0.1f;
            
        }
        
        
        void UpdateInteractionTypeForce(float frames, bool forceUpdate, out float currentForce, out Vector3 currentForceDirection)
        {
            currentForce = 0;
            currentForceDirection = Vector3.up;
            float forceMagnitude        = 0f;
          
            var currentTime = KW_Extensions.TotalTime();
            _timeDelta = (currentTime - _lastTime);
            _lastTime  = currentTime;
            
            if (_timeDelta <= 0f || _timeDelta > 1f) 
            {
                currentForce          = 0;
                currentForceDirection = Vector3.up;
                return;
            }

            if (IsTransformChanged(_t) || forceUpdate)
            {
                var delta = _lastPos - _relativePos;
                if (delta.sqrMagnitude > 0.000001f)
                {
                    currentForceDirection = delta.normalized;
                    forceMagnitude        = delta.magnitude / (_timeDelta * 60f);
                }
                _lastPos              = _relativePos;

                if (UseRotationForce)
                {
                    var currentRotation = _t.rotation.eulerAngles;
                    var prevForward     = _lastRotationQ * Vector3.forward;
                    var currForward     = _t.rotation    * Vector3.forward;
                    currentForceDirection = Vector3.Lerp((currForward - prevForward).normalized, currentForceDirection, Mathf.Clamp01(forceMagnitude));
                    _lastRotationQ        = _t.rotation;

                    var rotationForce = Mathf.Max(Mathf.Abs(Mathf.DeltaAngle(currentRotation.x, _lastRotation.x)),
                                                  Mathf.Max(Mathf.Abs(Mathf.DeltaAngle(currentRotation.y, _lastRotation.y)),
                                                            Mathf.Abs(Mathf.DeltaAngle(currentRotation.z, _lastRotation.z))));
                    _lastRotation  = currentRotation;
                    forceMagnitude = Mathf.Max(forceMagnitude, rotationForce * 0.05f);
                }
            }


            float targetForce = Mathf.Min(1, _force + forceMagnitude);
            _force =  Mathf.Lerp(_force, targetForce, _timeDelta * 5f);
            _force *= 0.8f;
           
            if (_force < 0.0001f || float.IsNaN(_force)) 
                _force = 0f;
            
            currentForce =  _force * MotionForce + ConstantForce;
            currentForceDirection = Vector3.Lerp(currentForceDirection * currentForce, _t.forward * (Mathf.Clamp01(currentForce * 10) * DirectionalVelocity), Mathf.Abs(DirectionalVelocity));
         
            if (frames > 1)
            {
                float frameScale = 1.0f / (frames * Mathf.Max(1, Time.timeScale));

                currentForce *= frameScale;
             //   currentForceDirection *= frameScale;
            }
        }
        
        void UpdateInteractionTypeDynamicObject(out float currentForce)
        {
            currentForce          = 0;

            if (IsTransformChanged(_t))
            {
                currentForce = (_lastPos - _relativePos).magnitude;
                _lastPos     = _relativePos;

                var currentRotation = _t.rotation.eulerAngles;
                var rotationForce = Mathf.Max(Mathf.Abs(Mathf.DeltaAngle(currentRotation.x, _lastRotation.x)),
                                              Mathf.Max(Mathf.Abs(Mathf.DeltaAngle(currentRotation.y, _lastRotation.y)),
                                                        Mathf.Abs(Mathf.DeltaAngle(currentRotation.z, _lastRotation.z))));
                _lastRotation = currentRotation;
                currentForce  = Mathf.Clamp01(Mathf.Max(currentForce, rotationForce * 0.5f));
            }
            _force = currentForce;
        }

        
        internal void CustomUpdate(int frames)
        {
            UpdateData(frames, forceUpdate: false);
        }
        

        public void OverrideForce(float normalizedForce, Vector3 direction)
        {
            _customForce          = normalizedForce;
            _customForceDirection = direction;
        }

        private WaterSurfaceRequestPoint _request = new WaterSurfaceRequestPoint();
        
        
        void CheckColliderToWaterIntersection()
        {
            if (_renderer == null) { _isObstacleBoundsIntersectWater = true; }

            if (_isObstacleBoundsIntersectWater)
            {
                _obstacleIntersectionAlphaFade = Mathf.Clamp01(_obstacleIntersectionAlphaFade + Time.deltaTime * 5);
                return;
            }
            
            var bounds    = _renderer.bounds;
            var minPos = bounds.min;
           
            _request.SetNewPosition(minPos); 
            WaterSystem.TryGetWaterSurfaceData(_request);

            _isObstacleBoundsIntersectWater = minPos.y < _request.Result.Position.y;

        }
        
        public void DrawGizmo(Color color, bool canDrawMesh, bool isSelected, Color selectedColor)
        {
            Gizmos.color = color;

            _t = transform;
            
            if (InteractionType == InteractionTypeEnum.ObstacleObject)
            {
                if (canDrawMesh && CurrentMesh)
                {
                    Gizmos.DrawWireMesh(CurrentMesh, 0, _t.TransformPoint(MeshOffset), _t.rotation, Vector3.Scale(_t.lossyScale, MeshScale));
                }
            }
            else
            {
                if (ForceType == ForceTypeEnum.Sphere)
                {
                    Gizmos.matrix = _t.localToWorldMatrix;
                    Gizmos.DrawWireSphere(Vector3.zero, 0.5f);

                    if (isSelected)
                    {
                        Gizmos.color = selectedColor;
                        Gizmos.DrawSphere(Vector3.zero, 0.5f);
                    }
                }

                if (ForceType == ForceTypeEnum.Box)
                {
                    Gizmos.matrix = _t.localToWorldMatrix;
                    Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
                    
                    if (isSelected)
                    {
                        Gizmos.color = selectedColor;
                        Gizmos.DrawCube(Vector3.zero, Vector3.one);
                    }
                }

                if (ForceType == ForceTypeEnum.Triangle)
                {
                    if (_triangleMesh == null) _triangleMesh = MeshUtils.CreateTriangle(1);
                    Gizmos.DrawWireMesh(_triangleMesh, 0, _t.position, _t.rotation, _t.lossyScale);
                    
                    if (isSelected)
                    {
                        Gizmos.color = selectedColor;
                        Gizmos.DrawMesh(_triangleMesh, 0, _t.position, _t.rotation, _t.lossyScale);
                    }
                }
            }

           
        }

        void OnDrawGizmos()
        {
            var color= Color.blue;
            if (InteractionType == InteractionTypeEnum.WaterDrain) color = new Color(1, 0.3f, 0.2f); 
            
            DrawGizmo(color, canDrawMesh: false, isSelected: false, color);
        }

        void OnDrawGizmosSelected()
        {
            var selectedColor                                                    = new Color(0.05f, 0.1f, 0.95f, 0.7f);
            if (InteractionType == InteractionTypeEnum.WaterDrain) selectedColor = new Color(1, 0.3f, 0.2f, 0.5f); 
            DrawGizmo(new Color(1, 0.9f, 0.1f, 0.35f), canDrawMesh: true, isSelected: true, selectedColor);
            
            
            // if (InteractionType == InteractionTypeEnum.ObstacleObject && _renderer != null)
            // {
            //     var bounds    = _renderer.bounds;
            //     Gizmos.DrawWireCube(bounds.center, bounds.size);
            // }
        }
    }
}