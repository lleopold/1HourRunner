using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KWS
{
    [ExecuteAlways]
    public class KWS_EffectorAnimationCurve : MonoBehaviour
    {
        public AnimationCurve ForceCurve             = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
        public float          ForceScale             = 1;
        
        [Space]
        public AnimationCurve VelocityCurve          = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
        public float VelocityScale          = 0.1f;
        
        [Space]
        public float AnimationTime          = 1;
        public bool  RandomVelocityRotation = true;
        
        private KWS_DynamicWavesSimulationEffector _effector;

        private float _leftTime = 0;
        
        void OnEnable()
        {
            _effector                = GetComponent<KWS_DynamicWavesSimulationEffector>();
            _effector.UseCustomForce = true;
            _leftTime                = 0;
        }

        void Update()
        {
            if (_leftTime > AnimationTime)
            {
                _effector.UseCustomForce = false;
                return;
            }
                
            _leftTime += KW_Extensions.DeltaTime();
            var currentTime     = _leftTime / AnimationTime;
            
            var currentForce    = ForceCurve.Evaluate(currentTime);
            var currentVelocity = VelocityCurve.Evaluate(currentTime);

            var rotation = _effector.transform.forward;
            if (RandomVelocityRotation)
            {
                rotation = Random.onUnitSphere;
            }
           
            _effector.OverrideForce(currentForce * ForceScale, currentVelocity * rotation);
        }
    }
}