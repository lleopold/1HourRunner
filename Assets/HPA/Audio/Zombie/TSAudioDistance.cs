// Description: TSAudioDistance: Play Audio using distance from the player
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace HP.Generics
{
    public class TSAudioDistance : MonoBehaviour
    {
        private float currentDistanceToTarget = -1;


     
        private bool isLightEnable = false;

        [Header("Distance in meters to:")]
        public float enableAudio = 40;                       
                                                           
                                                            
        public float reachMaxVolume = 20;                 
        public float speedToReachMaxVolume = 2;              

        private float volumeRef = 0;
     

        [System.Serializable]
        public class TargetParam
        {
            public Transform target;
            public float targetDistance = 0;

            public TargetParam(Transform trans)
            {
                target = trans;
            }
        }

        [HideInInspector]
        public List<TargetParam> targetsList = new List<TargetParam>();


        [Header("Audio")]
        public AudioSource aSource;

        void Start()
        {
        }

        public IEnumerator InitRoutine()
        {
            #region
            volumeRef = aSource.volume;

            TSCharacterTag[] targets = FindObjectsOfType<TSCharacterTag>();

            foreach (TSCharacterTag target in targets)
                targetsList.Add(new TargetParam(target.transform));


            yield return null;
            #endregion
        }

        void Update()
        {
            #region
            if (aSource && targetsList.Count > 0)
            {
                CalculateDistanceFromAudioSourceToTarget();

                EnableOrDisableLightDependingDistance();

                UpdateAudioVolume();
            }
            #endregion
        }

        void EnableOrDisableLightDependingDistance()
        {
            #region
            // Enable Light
            if (currentDistanceToTarget <= enableAudio && !isLightEnable)
            {
                isLightEnable = true;
                if (aSource)
                {
                    aSource.gameObject.SetActive(true);
                }
            }

            // Disable Light
            if (currentDistanceToTarget > enableAudio && isLightEnable)
            {
                isLightEnable = false;
                if (aSource)
                {
                    aSource.gameObject.SetActive(false);
                    ResetAudioVolume();
                }
            }
            #endregion
        }

        void ResetAudioVolume()
        {
            #region
            aSource.volume = 0;
            #endregion
        }

        void UpdateAudioVolume()
        {
            #region
            if (isLightEnable)
            {
                float scaledIntensity = (currentDistanceToTarget - reachMaxVolume) / (enableAudio - reachMaxVolume);
                scaledIntensity = Mathf.Clamp01(scaledIntensity);

                aSource.volume = Mathf.MoveTowards(aSource.volume, volumeRef * (1 - scaledIntensity), Time.deltaTime * speedToReachMaxVolume);
            }
            #endregion
        }


        void CalculateDistanceFromAudioSourceToTarget()
        {
            #region
            for (var i = 0; i < targetsList.Count; i++)
                if (targetsList[i].target)
                    targetsList[i].targetDistance = Vector3.Distance(targetsList[i].target.position, transform.position);


            for (var i = 0; i < targetsList.Count; i++)
            {
                if (i > 0 && targetsList[i].target && targetsList[i - 1].target)
                {
                    if (IsCurrentTargetDistanceSmallerThanPreviousTargetDistance(i, i - 1))
                        currentDistanceToTarget = targetsList[i].targetDistance;
                    else if (IsCurrentTargetDistanceSmallerThanPreviousTargetDistance(i - 1))
                        currentDistanceToTarget = targetsList[i - 1].targetDistance;
                    else
                        currentDistanceToTarget = 100000;
                }

                if (targetsList.Count == 1)
                {
                    if (IsCurrentTargetDistanceSmallerThanPreviousTargetDistance(i))
                        currentDistanceToTarget = targetsList[i].targetDistance;
                    else
                        currentDistanceToTarget = 1000000;
                }
            }
            #endregion
        }

        bool IsCurrentTargetDistanceSmallerThanPreviousTargetDistance(int currentID, int lastID = -1)
        {
            #region
            bool isDistanceSmallerThanOtherTarget = isDistanceSmallerThanOtherTarget = true;
            if (lastID != -1) isDistanceSmallerThanOtherTarget = targetsList[currentID].targetDistance < targetsList[lastID].targetDistance ? true : false;

            //bool isInFrontOfTarget = IsLampInFrontOfTheTarget(targetsList[currentID].target) ? true : false;
            bool isDistanceSmallerThanEnableFrontLightDistance = targetsList[currentID].targetDistance < enableAudio ? true : false;
            //bool isDistanceSmallerThanEnableBackLightDistance = targetsList[currentID].targetDistance < enableAudioBack ? true : false;

            if (isDistanceSmallerThanOtherTarget && isDistanceSmallerThanEnableFrontLightDistance)
                return true;
            else
                return false;
            #endregion
        }

        bool IsLampInFrontOfTheTarget(Transform target)
        {
            #region
            Vector3 dir = this.transform.position - target.position;
            float length = Vector3.Dot(dir, target.forward);

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
