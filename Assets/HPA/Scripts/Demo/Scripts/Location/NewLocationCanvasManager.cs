using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HP.Generics
{
    public class NewLocationCanvasManager : MonoBehaviour
    {
        public static NewLocationCanvasManager  instance;
        public SpawnSystem                      spawnSystem;

        [HideInInspector]
        public string                           lastLocation;

        void Awake()
        {
            #region Create only one instance of the gameObject in the Hierarchy
            if (instance == null)
                instance = this;
            #endregion
        }

        void Start()
        {
            StartCoroutine(InitRoutine());
        }

        IEnumerator InitRoutine()
        {
            transform.GetChild(0).gameObject.SetActive(false);
            transform.GetChild(0).GetChild(0).GetComponent<CanvasGroup>().alpha = 0;

            yield return null;
        }

        public void FadeTheText(GameObject obj)
        {
            StopAllCoroutines();
            StartCoroutine(FadeTheTextRoutine(obj));
        }

        IEnumerator FadeTheTextRoutine(GameObject obj)
        {
            #region 
            transform.GetChild(0).GetChild(0).GetComponent<CanvasGroup>().alpha = 0;
           // transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>().text = spawnSystem.spawnList[spawnSystem.currentSpawnID].locationName;
           // lastLocation = spawnSystem.spawnList[spawnSystem.currentSpawnID].locationName;

            transform.GetChild(0).gameObject.SetActive(true);

            float t = 0;
            float duration = 1;

            while (t < duration)
            {
                t += Time.deltaTime;

                transform.GetChild(0).GetChild(0).GetComponent<CanvasGroup>().alpha = t;

                yield return null;
            }


            t = 0;
            duration = 2;
            while (t < duration)
            {
                t += Time.deltaTime;
                yield return null;
            }

            t = 0;
            duration = 1;
            while (t < duration)
            {
                t += Time.deltaTime;

                transform.GetChild(0).GetChild(0).GetComponent<CanvasGroup>().alpha = 1 - t;

                yield return null;
            }

            if (obj)
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

        public void TextFromNewLocationTrigger(string newtext,GameObject objNewLocation)
        {
            StopAllCoroutines();
            StartCoroutine(TextFromNewLocationTriggerRoutine(newtext));
            if(objNewLocation)
                objNewLocation.SetActive(false);
        }

        IEnumerator TextFromNewLocationTriggerRoutine(string newtext)
        {
            #region
           
            transform.GetChild(0).GetChild(0).GetComponent<CanvasGroup>().alpha = 0;
            transform.GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = newtext;

            transform.GetChild(0).GetChild(0).GetChild(1).GetChild(2).gameObject.SetActive(true);
            transform.GetChild(0).GetChild(0).GetChild(1).GetChild(3).gameObject.SetActive(true);

            transform.GetChild(0).GetChild(0).GetChild(1).GetChild(4).gameObject.SetActive(false);

            lastLocation = newtext;

            transform.GetChild(0).gameObject.SetActive(true);

            float t = 0;
            float duration = 1;

            while (t < duration)
            {
                t += Time.deltaTime;

                transform.GetChild(0).GetChild(0).GetComponent<CanvasGroup>().alpha = t;

                yield return null;
            }


            t = 0;
            duration = 4;
            while (t < duration)
            {
                t += Time.deltaTime;
                yield return null;
            }

            t = 0;
            duration = 1;
            while (t < duration)
            {
                t += Time.deltaTime;

                transform.GetChild(0).GetChild(0).GetComponent<CanvasGroup>().alpha = 1 - t;

                yield return null;
            }

            yield return null; 
            #endregion
        }

        public void TextFromZoneLimitTriggerEnter(string newtext)
        {
            StopAllCoroutines();
            StartCoroutine(TextFromZoneLimitTriggerEnterRoutine(newtext));
        }

        IEnumerator TextFromZoneLimitTriggerEnterRoutine(string newtext)
        {
            #region

            transform.GetChild(0).GetChild(0).GetComponent<CanvasGroup>().alpha = 0;
            transform.GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = newtext;
            lastLocation = newtext;

            transform.GetChild(0).GetChild(0).GetChild(1).GetChild(2).gameObject.SetActive(false);
            transform.GetChild(0).GetChild(0).GetChild(1).GetChild(3).gameObject.SetActive(false);

            transform.GetChild(0).GetChild(0).GetChild(1).GetChild(4).gameObject.SetActive(true);

            transform.GetChild(0).gameObject.SetActive(true);

            float t = 0;
            float duration = 1;

            while (t < duration)
            {
                t += Time.deltaTime;

                transform.GetChild(0).GetChild(0).GetComponent<CanvasGroup>().alpha = t;

                yield return null;
            }

            yield return null;
            #endregion
        }

        public void TextFromZoneLimitTriggerExit(string newtext)
        {
            StopAllCoroutines();
            StartCoroutine(TextFromZoneLimitTriggerExitRoutine(newtext));
        }

        IEnumerator TextFromZoneLimitTriggerExitRoutine(string newtext)
        {
            #region
            transform.GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = newtext;
            lastLocation = newtext;

            transform.GetChild(0).gameObject.SetActive(true);

            float t = 1 - transform.GetChild(0).GetChild(0).GetComponent<CanvasGroup>().alpha;
            float duration = 1;

            while (t < duration)
            {
                t += Time.deltaTime;

                transform.GetChild(0).GetChild(0).GetComponent<CanvasGroup>().alpha = 1 - t;

                yield return null;
            }

            yield return null;
            #endregion
        }
    }
}
