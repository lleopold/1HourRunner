using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HP.Generics
{
    public class OptionsPageAssistant : MonoBehaviour
    {
        public GameObject objHuDInfo;
        public GameObject objFPSInfo;
        public void UpdateHudInfoState()
        {
            #region
            if (objHuDInfo)
                objHuDInfo.SetActive(!objHuDInfo.activeSelf); 
            #endregion
        }

        public void UpdateFPSInfoState()
        {
            #region
            if (objFPSInfo)
                objFPSInfo.SetActive(!objFPSInfo.activeSelf);
            #endregion
        }

        public void QuitTheApplication()
        {
            Application.Quit();
        }

    }

}