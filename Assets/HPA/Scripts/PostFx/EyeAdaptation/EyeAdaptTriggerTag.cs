using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HP.Generics
{
    public class EyeAdaptTriggerTag : MonoBehaviour
    {
        public EyeAdaptationTrigger eyeAdaptation;
        public bool insideOnly = false;

        void OnTriggerEnter(Collider other)
        {
            //Debug.Log("blabla");
            eyeAdaptation.EyeAdaptationTransition(insideOnly);
        }
    }

}
