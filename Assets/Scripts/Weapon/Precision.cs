using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Splines;
using UnityEngine;

namespace Assets.Scripts.Weapon
{
    internal class Precision
    {
        public float minPrecisionAngle { get; set; }
        public float maxPrecisionAngle { get; set; }
        public float aimingStartingPrecision { get; set; }
        public float currentPrecisionAngle { get; set; }
        public float aimingSpeed { get; set; }

        public Precision(float minPrecision, float maxPrecision, float currentPrecision, float aimingSpeed)
        {
            this.minPrecisionAngle = minPrecision;
            this.maxPrecisionAngle = maxPrecision;
            this.currentPrecisionAngle = currentPrecision;
            this.aimingSpeed = aimingSpeed;
        }

        public float GetPrecision(float currentPrecision, float deltaTime)
        {
            if (currentPrecision > minPrecisionAngle)
            {
                currentPrecision -= aimingSpeed * deltaTime;
            }
            else
                return minPrecisionAngle;
            return currentPrecision;
        }

        public int CheckPrecision()
        {
            if (currentPrecisionAngle < minPrecisionAngle)
            {
                UnityEngine.Debug.LogError("Precision is lower than minimum precision angle");
                return 1;
            }
            else if (currentPrecisionAngle > maxPrecisionAngle)
            {
                UnityEngine.Debug.LogError("Precision is lower than minimum precision angle");
                return 1;
            }
            if (aimingSpeed <= 0)
            {
                UnityEngine.Debug.LogError("Aiming speed is lower than 0");
                return 1;
            }

            else
                return 0;
        }
    }
}
