using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HP.Generics
{
    public class AmbianceVolumeInsideOutside : MonoBehaviour
    {
        public float outsideVolume = .137f;
        public float insideVolume = .1f;
        public AudioSource aSource;

        public bool isOutside = true;

        // Start is called before the first frame update


        private void Update()
        {
            Detection();
        }

        IEnumerator NewVolumeRoutine(float newVolume)
        {
            isOutside = !isOutside;

            float t = 0;
            float duration = .5f;

            float startValue = aSource.volume;

            while (t < 1)
            {
                t += Time.deltaTime / duration;

                aSource.volume = Mathf.Lerp(startValue, newVolume, t);

                yield return null;
            }


            yield return null;
        }

        Vector3 lastPos = new Vector3();
        bool lastCheckHitDetected = false;


        void Detection()
        {
            RaycastHit hit;
            if (Physics.Linecast(transform.position, lastPos, out hit))
            {
                if(!lastCheckHitDetected)
                    ChangeVolume(hit);

                lastCheckHitDetected = true;
               
            }
            else
            {
                lastCheckHitDetected = false;
            }


            lastPos = transform.position;
        }


        void ChangeVolume(RaycastHit hit)
        {
            if (hit.transform.GetComponent<EyeAdaptTriggerTag>())
            {
                //Debug.Log("Check)");
                if (aSource && isOutside)
                {
                    StopAllCoroutines();
                    StartCoroutine(NewVolumeRoutine(insideVolume));
                }
                else if (aSource && !isOutside)
                {
                    StopAllCoroutines();
                    StartCoroutine(NewVolumeRoutine(outsideVolume));
                }
            }
        }
    }

}
