using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HP.Generics
{
    public interface IInitable
    {
        bool IsInitDone() { return true; }
    }
}