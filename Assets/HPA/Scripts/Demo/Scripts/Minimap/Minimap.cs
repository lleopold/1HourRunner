using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HP.Generics
{
    public class Minimap : MonoBehaviour
    {
        public RectTransform    minimapZoneRect;
        public RectTransform    characterRect;
        public Transform        minimapOrigin;
        TSCharacterTag          character;
        public Vector2          mapSize = new Vector2(2100, 2100);
        public Transform        camPosition; 

        void Start()
        {
            #region
            character = FindObjectOfType<TSCharacterTag>();
            if(!camPosition)
                camPosition = GameObject.FindWithTag("MainCamera").transform; 
            #endregion
        }

        private void OnDisable()
        {
            characterRect.gameObject.SetActive(false);
        }

        void Update()
        {
            #region
            if (character)
            {
                UpdateCharacterDirection();
                UpdateCharacterPosition();
            } 
            #endregion
        }
        void UpdateCharacterPosition()
        {
            #region 
            float posXChara = character.transform.position.x;
            float posMinX = minimapOrigin.position.x;
            float posMaxX = minimapOrigin.position.x + mapSize.x;

            float scaledPosX = (posXChara - posMinX) / (posMaxX - posMinX);

            float posYChara = character.transform.position.z;
            float posMinY = minimapOrigin.position.z;
            float posMaxY = minimapOrigin.position.z + mapSize.y;

            float scaledPosY = (posYChara - posMinY) / (posMaxY - posMinY);

            characterRect.pivot = new Vector2(scaledPosX, scaledPosY);

            if (!characterRect.gameObject.activeSelf)
                characterRect.gameObject.SetActive(true);
            #endregion
        }

        void UpdateCharacterDirection()
        {
            #region
            if (camPosition)
            {
                characterRect.GetChild(0).localEulerAngles
                = new Vector3(  characterRect.GetChild(0).localEulerAngles.x,
                                characterRect.GetChild(0).localEulerAngles.y,
                                -camPosition.eulerAngles.y);
            } 
            #endregion
        }
    }
}
