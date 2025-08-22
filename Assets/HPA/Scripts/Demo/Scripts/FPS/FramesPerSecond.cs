using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HP.Generics
{
    public class FramesPerSecond : MonoBehaviour
    {
        Text txt;
        float timeBetweenTwoFPSCalculation = .5f;
        float timer = 0;
        
        List<int> fpsList;

        void Start()
        {
            #region 
            txt = GetComponent<Text>();
            fpsList = new List<int>(); 
            #endregion
        }

        void Update()
        {
            #region 
            CalculateFPS(); 
            #endregion
        }

        void CalculateFPS()
        {
            #region
            if (timer > timeBetweenTwoFPSCalculation)
            {
                var total = 0;
                for (var i = 0; i < fpsList.Count; i++)
                    total += fpsList[i];

                total /= fpsList.Count;

                txt.text = string.Format("{0} FPS", (int)total);
                timer = 0;
                fpsList.Clear();
            }
            else
            {
                fpsList.Add((int)(1 / Time.deltaTime));
            }

            timer += Time.deltaTime; 
            #endregion
        }
    }

}
