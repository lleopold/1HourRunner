// Description: TSLightOpti: Optimize Lights
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace HP.Generics
{
    public class TSLightOpti : MonoBehaviour
    {
        private float           currentDistanceToTarget = -1;

        private Light           lightC;
        private bool            isLightEnable = false;

        [Header ("Distance in meters to:")]
        public float            enableLight = 20;                       // Enable the light if the distance to the target < enableLight + Light is in front of the target
       // public float            enableLightBack = 60;                   // Enable the light if the distance to the target < enableLightBack + Light is behind the target
       // public bool             LinkFrontAndBack = false;               // if true -> use enableLight value for light in front of and behind the target
        public float            reachMaxIntensity = 10;                 // Max Light intensity is player distance < reachMaxIntensity
        public float            SpeedLightIntensity = 2;                // Speed to reach the max light intensity

        private bool            isLightShadowEnable = false;            // return if the shadow of the light is enabled
        public float            enableShadow = 10;                      // Enable the shadow if the distance to the target < enableShadow
        public float            reachMaxStrength = 5;                   // Speed to reach the max shadow strength

        public LightShadows     lightShadows = LightShadows.Hard;

        private float           intensityRef = 0;
        private float           strengthRef = 0;

        public bool             bypassShadow = false;                   // Disable shadow for this light




        [System.Serializable]
        public class TargetParam
        {
            public Transform    target;
            public float        targetDistance = 0;

            public TargetParam(Transform trans)
            {
                target = trans;
            }
        }

        [HideInInspector]
        public List<TargetParam> targetsList = new List<TargetParam>();

        void Start()
        {
        }

        public IEnumerator InitRoutine()
        {
            #region
            lightC = GetComponent<Light>();

            intensityRef = lightC.intensity;
            strengthRef = lightC.shadowStrength;

            isLightEnable = lightC.enabled;

            if(bypassShadow) lightC.shadows = LightShadows.None;
            if (isLightEnable) ResetLightIntensity();

            TSCharacterTag[] targets = FindObjectsOfType<TSCharacterTag>();

            foreach (TSCharacterTag target in targets)
                targetsList.Add(new TargetParam(target.transform));


            yield return null;
            #endregion
        }

        void Update()
        {
            #region
            if(lightC && targetsList.Count > 0)
            {
                CalculateDistanceFromLightToTarget();

                EnableOrDisableLightDependingDistance();
                EnableOrDisableLightShadowDependingDistance();

                UpdateLightIntensity();
                UpdateLightShadow();
            }
            #endregion
        }

        void EnableOrDisableLightDependingDistance()
        {
            #region
            // Enable Light
            if(currentDistanceToTarget <= enableLight && !isLightEnable)
            {
                isLightEnable = true;
                if (lightC)
                {
                    lightC.enabled = true;    
                }
            }

            // Disable Light
            if (currentDistanceToTarget > enableLight && isLightEnable)
            {
                isLightEnable = false;
                if (lightC)
                {
                   lightC.enabled = false;
                   ResetLightIntensity();
                } 
            }
            #endregion
        }

        void ResetLightIntensity()
        {
            #region
            lightC.intensity = 0;
            #endregion
        }

        void UpdateLightIntensity()
        {
            #region
            if (isLightEnable)
            {
                float scaledIntensity = (currentDistanceToTarget - reachMaxIntensity) / (enableLight - reachMaxIntensity);
                scaledIntensity = Mathf.Clamp01(scaledIntensity);

                lightC.intensity = Mathf.MoveTowards(lightC.intensity, intensityRef * (1 - scaledIntensity),Time.deltaTime* SpeedLightIntensity);
            }
            #endregion
        }

        void EnableOrDisableLightShadowDependingDistance()
        {
            #region
            if (isLightEnable && !bypassShadow)
            {
                // Enable Shadow
                if (currentDistanceToTarget <= enableShadow && !isLightShadowEnable)
                {
                    isLightShadowEnable = true;
                    if (lightC)
                        lightC.shadows = lightShadows;
                }

                // Disable Shadow
                if (currentDistanceToTarget > enableShadow && isLightShadowEnable)
                {
                    isLightShadowEnable = false;
                    if (lightC)
                    {
                        lightC.shadows = LightShadows.None;
                        ResetLightShadow();
                    }
                }
            }
            #endregion
        }

        void ResetLightShadow()
        {
            #region
            lightC.shadowStrength = 0;
            #endregion
        }

        void UpdateLightShadow()
        {
            #region
            if (isLightEnable && isLightShadowEnable)
            {
                float scaledShadowStrength = (currentDistanceToTarget - reachMaxStrength) / (enableShadow - reachMaxStrength);
                scaledShadowStrength = Mathf.Clamp01(scaledShadowStrength);

                lightC.shadowStrength = Mathf.MoveTowards(lightC.shadowStrength, strengthRef * (1 - scaledShadowStrength), Time.deltaTime * 2);
            }
            #endregion
        }

        void CalculateDistanceFromLightToTarget()
        {
            #region
            for (var i = 0; i < targetsList.Count; i++)
                if (targetsList[i].target)
                    targetsList[i].targetDistance = Vector3.Distance(targetsList[i].target.position, transform.position);

           
            for (var i = 0; i < targetsList.Count; i++)
            {
                if (i > 0 && targetsList[i].target && targetsList[i - 1].target)
                {
                    if (IsCurrentTargetDistanceSmallerThanPreviousTargetDistance(i,i-1))
                        currentDistanceToTarget = targetsList[i].targetDistance;
                    else if (IsCurrentTargetDistanceSmallerThanPreviousTargetDistance(i-1))
                        currentDistanceToTarget = targetsList[i-1].targetDistance;
                    else
                        currentDistanceToTarget = 100000;
                }

                if (targetsList.Count == 1){
                    if (IsCurrentTargetDistanceSmallerThanPreviousTargetDistance(i))
                        currentDistanceToTarget = targetsList[i].targetDistance;
                    else 
                        currentDistanceToTarget = 1000000;
                }
            }
            #endregion
        }

        bool IsCurrentTargetDistanceSmallerThanPreviousTargetDistance(int currentID,int lastID = -1)
        {
            #region
            bool isDistanceSmallerThanOtherTarget = isDistanceSmallerThanOtherTarget = true;
            if (lastID != -1) isDistanceSmallerThanOtherTarget = targetsList[currentID].targetDistance < targetsList[lastID].targetDistance ? true : false; 

            //bool isInFrontOfTarget = IsLampInFrontOfTheTarget(targetsList[currentID].target) ? true : false;
            bool isDistanceSmallerThanEnableFrontLightDistance = targetsList[currentID].targetDistance < enableLight ? true : false;
            //bool isDistanceSmallerThanEnableBackLightDistance = targetsList[currentID].targetDistance < enableLightBack ? true : false;

            if (isDistanceSmallerThanOtherTarget  && isDistanceSmallerThanEnableFrontLightDistance)
                return true;
            else
               return false;
            #endregion
        }

        bool IsLampInFrontOfTheTarget(Transform target)
        {
            #region
            Vector3 dir = this.transform.position - target.position;
            float length  = Vector3.Dot(dir, target.forward);

            // if length > 0 the lamp is in front of the target
            //  if length < 0 the lamp is behind the target
            if (length >= 0)
                return true;
            else
                return false;
            #endregion
        }
    }

}
