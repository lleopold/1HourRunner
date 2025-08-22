using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HP.Generics
{
    public class Init_LightProb : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            LightProbes.TetrahedralizeAsync();
        }
    }

}
