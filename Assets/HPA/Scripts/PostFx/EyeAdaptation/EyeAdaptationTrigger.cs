using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HP.Generics
{
    public class EyeAdaptationTrigger : MonoBehaviour
    {
        public float            transitionDuration = 1;
        public Volume           localPostFx;

        public float            startWeight = 0;

        public enum EyeAdaptState { Inside, Outside };
        public EyeAdaptState    player = EyeAdaptState.Outside;


        void Start()
        {
            InitLocalPostFxWeight();
        }

        public void InitLocalPostFxWeight()
        {
            localPostFx.weight = startWeight;
        }

        public void EyeAdaptationTransition(bool insideOnly = false)
        {
            #region
            StopAllCoroutines();
            StartCoroutine(EyeAdaptationTransitionRoutine(insideOnly));
            #endregion
        }

        public void SpawnForceInsideEyeAdaptationTransition(bool insideOnly = false)
        {
            #region
            player = EyeAdaptState.Inside;
            StopAllCoroutines();
            StartCoroutine(EyeAdaptationTransitionRoutine(insideOnly));
            #endregion
        }

        IEnumerator EyeAdaptationTransitionRoutine(bool insideOnly = false)
        {
            #region
            float t = 0;
            float duration = transitionDuration;
            float target = 0;

            float currentWeight = localPostFx.weight;
            //Debug.Log("blabla1");

            if (insideOnly)
            {
                target = 1;
                player = EyeAdaptState.Inside;
                //Debug.Log("blabla2");
            }
            else
            {
                if (player == EyeAdaptState.Outside)
                {
                    target = 1;
                    player = EyeAdaptState.Inside;
                }
                else
                {
                    player = EyeAdaptState.Outside;
                }
                //Debug.Log("blabla3");

            }

            while (t < 1)
            {
                t += Time.deltaTime / duration;

                localPostFx.weight = Mathf.Lerp(currentWeight, target, t);

                yield return null;
            }
            //Debug.Log("blabla4");
            yield return null;
            #endregion
        }
    }
}

