using UnityEngine;

namespace JuiceBox
{
    public class TestScripts : MonoBehaviour
    {
        public float HoldTime = 5f;

        private float lastHoldRelease;

        public void DoNothing()
        {

        }

        public SignalEffect HoldForTime()
        {
            if (lastHoldRelease + HoldTime > Time.time)
                return SignalEffect.KeepAlive;

            lastHoldRelease = Time.time;

            return SignalEffect.RunNormally;
        }
    }
}