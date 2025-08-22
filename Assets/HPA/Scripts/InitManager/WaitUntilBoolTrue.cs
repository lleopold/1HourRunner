using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HP.Generics
{
    public class WaitUntilBoolTrue : MonoBehaviour, HP.Generics.IInitable
    {
        public bool isInitDone = false;

        public bool IsInitDone()
        {
            return isInitDone;
        }
    }

}
