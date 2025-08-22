using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HP.Generics
{
    public class NewLocationTrigger : MonoBehaviour
    {
        public string locationName = "New Location";
        public List<GameObject> locationMiniMap = new List<GameObject>();
        public Color locationColor = Color.blue;



        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<TSCharacterTag>() && 
                NewLocationCanvasManager.instance.lastLocation != locationName)
            {
                NewLocationCanvasManager.instance.TextFromNewLocationTrigger(locationName,this.gameObject);

                for (var i = 0; i < locationMiniMap.Count; i++)
                {
                    locationMiniMap[i].GetComponent<Image>().color = locationColor;
                   locationMiniMap[i].SetActive(true);
                }
                   

            }
        }

    }

}
