// Description: ResetPath: Attached to objects with a Bezier Component. 
// Reset the Bezier curve
using UnityEngine;

namespace HP.Generics
{
    public class ResetPath : MonoBehaviour
    {
        public bool             seeInspector = false;
        public bool             resetTransform = true;
        public bool             resetBezierScript = true;
        public bool             resetMesh = true;
        public bool             resetCollider = true;

        public bool             mirror = false;

        public MirrorType       mirrorType = MirrorType.None;
    }
    public enum MirrorType { None, BarrierAndWall,Concrete, InstantiatedOnly }
}
