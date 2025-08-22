// Description: ObjectRotation. Rotate an object
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HP.Generics
{
    public class ObjectRotation : MonoBehaviour
    {
        public Transform    trans;
        public float        speed = 1000;

        public void Update()
        {
            #region 
            RotateObject(); 
            #endregion
        }

        void RotateObject()
        {
            #region
            trans.localEulerAngles += new Vector3(0, 0, Time.deltaTime * speed);
            #endregion
        }
    }

}
