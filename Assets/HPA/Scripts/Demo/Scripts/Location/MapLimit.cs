using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HP.Generics
{
    public class MapLimit : MonoBehaviour
    {
        public string locationName = "You're close to the map limit. Turn back.";

        public bool defaultText = true;

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<TSCharacterTag>())
            {
                if(defaultText) locationName = "<size=20>YOU'RE CLOSE TO THE MAP LIMIT</size>" 
                    + "\n" 
                    + "TURN BACK";
                NewLocationCanvasManager.instance.TextFromZoneLimitTriggerEnter(locationName);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponent<TSCharacterTag>())
            {
                if (defaultText) locationName = "<size=20>YOU'RE CLOSE TO THE MAP LIMIT</size>"
                      + "\n"
                      + "TURN BACK";
                NewLocationCanvasManager.instance.TextFromZoneLimitTriggerExit(locationName);
            }
        }
    }

}

