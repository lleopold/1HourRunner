using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HP.Generics
{
    public class OptiGridAndPlayer : MonoBehaviour, IInitable
    {
       // public bool instantiateTheCharacter = false;

        public GameObject character;
        public AP_Cam_Follow cam;

        void Start()
        {
            InstantiateCharacter();
        }


        public void InstantiateCharacter()
        {
            StartCoroutine(InstantiateCharacterRoutine());
        }

        IEnumerator InstantiateCharacterRoutine()
        {
            GameObject newChara = Instantiate(character, new Vector3(996, 31, 830), Quaternion.identity);

            yield return new WaitUntil(() => newChara.transform.position == new Vector3(996, 31, 830));

            // Access Head object inside the character
            cam.target = newChara.transform.GetChild(4).GetChild(1);

            yield return new WaitForSeconds(2);

            HP.Generics.TSOptiGrid.instance.Init();

            yield return null;
        }


        public bool IsInitDone()
        {
            if (HP.Generics.TSOptiGrid.instance.isInitDone)
                return true;
            else
                return false;
        }
    }

}
