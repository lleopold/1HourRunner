// Description: Metods used during SpawnSystem and  GridOptimization initialization
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HP.Generics
{
    public class TSOptiCustomMethods : MonoBehaviour
    {
        public GameObject   loadingScreen;
        //public SpawnSystem  spawnSystem;
        // public bool         example = true;

        public void DisableCharacterMovement(GameObject obj)
        {
            #region
            /*if (example)
            {
                if (spawnSystem.Chara.GetComponent<Rigidbody>())
                    spawnSystem.Chara.GetComponent<Rigidbody>().isKinematic = true;
                    
                if (spawnSystem.Chara.GetComponent<characterMovement>())
                    spawnSystem.Chara.GetComponent<characterMovement>().isMovementAllowed = false;
            }*/

            ActionProcessDone(obj);
            #endregion
        }

        public void MoveCharacter(GameObject obj)
        {
            #region
            StartCoroutine(MoveCharacterRoutine(obj));
            #endregion
        }

        IEnumerator MoveCharacterRoutine(GameObject obj)
        {
            #region
            /*if (example)
            {
                // Move the player to the spawn position + the player looks in the same direction as the target
                yield return new WaitUntil(() => spawnSystem.Chara);

                spawnSystem.Chara.transform.SetParent(spawnSystem.spawnList[spawnSystem.currentSpawnID].spawnPos);

                yield return new WaitUntil(() => spawnSystem.Chara.transform.parent == spawnSystem.spawnList[spawnSystem.currentSpawnID].spawnPos);

                spawnSystem.Chara.transform.localPosition = new Vector3(0,0,0);
                spawnSystem.Chara.transform.localRotation = Quaternion.identity;

                yield return new WaitUntil(() => spawnSystem.Chara.transform.localPosition == Vector3.zero);
                yield return new WaitUntil(() => spawnSystem.Chara.transform.localRotation == Quaternion.identity);

                spawnSystem.Chara.transform.SetParent(null);

                yield return new WaitUntil(() => spawnSystem.Chara.transform.parent == null);



                // Replace the camera for the example character. 
                // If needed add your own code to initialize your chararcter.
                if (spawnSystem.Chara.GetComponent<characterMovement>())
                {
                    spawnSystem.Chara.GetComponent<characterMovement>().mouseY = 0;
                    spawnSystem.Chara.GetComponent<characterMovement>().objCamera.rotation = spawnSystem.spawnList[spawnSystem.currentSpawnID].spawnPos.rotation;

                    yield return new WaitForSeconds(.5f);
                }
            }*/

            ActionProcessDone(obj);

            yield return null;
            #endregion

        }


        public void UpdateCameraPosition(GameObject obj)
        {
            #region
            ActionProcessDone(obj);
            #endregion
        }

        public void EnableCharacterMovement(GameObject obj)
        {
            #region
          /*  if (example)
            {
                if (spawnSystem.Chara.GetComponent<Rigidbody>())
                    spawnSystem.Chara.gameObject.GetComponent<Rigidbody>().isKinematic = false;
                   
                if (spawnSystem.Chara.GetComponent<characterMovement>())
                {
                    spawnSystem.Chara.GetComponent<characterMovement>().ResetMovement();
                    spawnSystem.Chara.GetComponent<characterMovement>().isMovementAllowed = true;
                }  
            }*/
               
            ActionProcessDone(obj);
            #endregion
        }

        public void DisableLoadingScreen(GameObject obj)
        {
            #region
            StartCoroutine(DisableLoadingScreenRoutine(obj));
            #endregion
        }

        public IEnumerator DisableLoadingScreenRoutine(GameObject obj)
        {
            #region
            if (loadingScreen)
            {
                
            }

            if (loadingScreen)
            {
                Image imLoadingScreen = loadingScreen.transform.GetChild(0).GetComponent<Image>();


                loadingScreen.SetActive(true);

                // Fade
                Color loadingScreenColor = imLoadingScreen.color;
                while (imLoadingScreen.color.a > 0)
                {
                    float alpha = imLoadingScreen.color.a;
                    alpha = Mathf.MoveTowards(alpha, 0, Time.deltaTime * 2f);

                    imLoadingScreen.color = new Color(loadingScreenColor.r, loadingScreenColor.g, loadingScreenColor.b, alpha);
                    yield return null;
                }

                loadingScreen.SetActive(false);
            }

            if (obj)
                ActionProcessDone(obj);

            yield return null;
            #endregion
        }

        public void EnableLoadingScreen(GameObject obj)
        {
            #region
            if (loadingScreen)
                loadingScreen.SetActive(true);
                
            StartCoroutine(EnableLoadingScreenRoutine(obj));
            #endregion
        }

        public IEnumerator EnableLoadingScreenRoutine(GameObject obj)
        {
            #region

            if (loadingScreen)
            {
                Image imLoadingScreen = loadingScreen.transform.GetChild(0).GetComponent<Image>();


                if(imLoadingScreen.color.a != 1)
                {
                    loadingScreen.SetActive(true);
                    // Fade 
                    Color loadingScreenColor = imLoadingScreen.color;
                    while (imLoadingScreen.color.a < 1)
                    {
                        float alpha = imLoadingScreen.color.a;
                        alpha = Mathf.MoveTowards(alpha, 1, Time.deltaTime * 4f);

                        imLoadingScreen.color = new Color(loadingScreenColor.r, loadingScreenColor.g, loadingScreenColor.b, alpha);
                        yield return null;
                    }
                }

              
            }
            ActionProcessDone(obj);
            yield return null;
            #endregion
        }

        public void ActionProcessDone(GameObject obj)
        {
            #region
            IValidateAction<bool> vAction = obj.GetComponent<IValidateAction<bool>>();
            vAction.ValidateAction(true);
            #endregion
        }

        public void ResetLocalPostFx(GameObject obj)
        {
            #region 
            HP.Generics.EyeAdaptationTrigger[] eyeAdaptationTriggers = FindObjectsOfType<HP.Generics.EyeAdaptationTrigger>();
            foreach (HP.Generics.EyeAdaptationTrigger eyeAdaptationTrigger in eyeAdaptationTriggers)
                eyeAdaptationTrigger.SpawnForceInsideEyeAdaptationTransition();

            ActionProcessDone(obj); 
            #endregion
        }

        public void DisableNewLocation(GameObject obj)
        {
            #region 

            StartCoroutine(DisableNewLocationRoutine(obj));
            #endregion
        }

        IEnumerator DisableNewLocationRoutine(GameObject obj)
        {
            GroupLocationTag groupLocationTag = FindObjectOfType<GroupLocationTag>();

            if (groupLocationTag)
            {
                groupLocationTag.transform.GetChild(0).gameObject.SetActive(false);
                yield return new WaitUntil(() => !groupLocationTag.transform.GetChild(0).gameObject.activeSelf);
                NewLocationCanvasManager.instance.TextFromNewLocationTrigger("", null);
            }
            ActionProcessDone(obj);
            yield return null;
        }

        public void EnableNewLocation(GameObject obj)
        {
            #region 
            StartCoroutine(EnableNewLocationRoutine(obj));

            #endregion
        }
        IEnumerator EnableNewLocationRoutine(GameObject obj)
        {
            GroupLocationTag groupLocationTag = FindObjectOfType<GroupLocationTag>();

            if (groupLocationTag)
            {
                groupLocationTag.transform.GetChild(0).gameObject.SetActive(true);
                yield return new WaitUntil(() => groupLocationTag.transform.GetChild(0).gameObject.activeSelf);
            }
            ActionProcessDone(obj);
            yield return null;
        }
    }

}


