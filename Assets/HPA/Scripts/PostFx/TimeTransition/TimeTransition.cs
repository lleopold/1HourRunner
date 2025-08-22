using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace HP.Generics
{
    public class TimeTransition : MonoBehaviour
    {
        public GameObject       sun;
        Light sunLight;

      //  [HideInInspector]
        public bool             isTransitionAllowed = true;
       // [HideInInspector]
        public int              selectedPreset = 0;

        [System.Serializable]
        public class Params
        {
            [Header("Sun Transition")] 
            public float newValue = 10;

            [Header("Sun Emission Color")]
            public Color sunEmissionColor;
            public float sunEmissionIntensity;

            [Header("Skybox")]
            public float sunSize = 0;
            public float sunSizeConvergence = 0;
            public float atmosphereThickness = 0;
            public float exposure = 0;

            [Header("Fog")]
            public float density = .001f;
            public Color fogColor = Color.black;

            [Header("Bloom")]
            public float bloomThreshold = 0;
            public float bloomIntensity = 0;
            public float bloomScatter = 0;
            //public Color bloomTint = Color.blue;

            [Header("Color Adjustement")]
            public int colorAdjustContrast = 0;
            //public Color colorAdjustColorFilter;
            public int colorAdjustSaturatin = 0;

            [Header("Color Lookup")]
            public float colorLookupContribution;

           

        }

        public List<Params>     presets = new List<Params>();

        public float            transitionDuration = 2;

        Material                runtimeSkybox;

        public Volume           volume;
        private VolumeProfile   volumeProfile;

        private Bloom           bloom;
        private ColorAdjustments colorAdjustments;
        private ColorLookup     colorLookup;

        public KeyCode          key = KeyCode.U;

       
        public string sceneName;

        public float firstPartDuration = 10;

        public float timer = 10;
        private float currentTimer =0;
        public float howManyDivision = 5;
        private float currentDivision = 0;
        private float ratio = 0;

        private bool autoMode = false;


        public float bypassAutoModeDuration = 15;

        // Start is called before the first frame update
        void Start()
        {
            #region 
            Init(); 
            #endregion
        }

        void Init()
        {
            #region 
            CloneSkybox();
            InitPostEffect();
            InitSun();
            StartCoroutine(DayTimeRoutinePart1());
            #endregion
        }


        IEnumerator DayTimeRoutinePart1()
        {
            autoMode = true;
            while (!isTransitionAllowed)
            {
                yield return null;
            }
            while (!runtimeSkybox)
            {
                yield return null;
            }

            if (currentDivision == 0)
            {
                bloom.threshold.Override(presets[selectedPreset].bloomThreshold);
                bloom.intensity.Override(presets[selectedPreset].bloomIntensity);
                bloom.scatter.Override(presets[selectedPreset].bloomScatter);
                colorAdjustments.contrast.Override(presets[selectedPreset].colorAdjustContrast);
                colorAdjustments.saturation.Override(presets[selectedPreset].colorAdjustSaturatin);
                colorLookup.contribution.Override(presets[selectedPreset].colorLookupContribution);

                RenderSettings.fogDensity = presets[selectedPreset].density;
                RenderSettings.fogColor = presets[selectedPreset].fogColor;
                runtimeSkybox.SetFloat("_SunSize", presets[selectedPreset].sunSize);
                runtimeSkybox.SetFloat("_Exposure", presets[selectedPreset].exposure);
                runtimeSkybox.SetFloat("_SunSizeConvergence", presets[selectedPreset].sunSizeConvergence);
                runtimeSkybox.SetFloat("_AtmosphereThickness", presets[selectedPreset].atmosphereThickness);

                sun.transform.localEulerAngles =
                new Vector3(presets[selectedPreset].newValue,
                           sun.transform.localEulerAngles.y,
                           sun.transform.localEulerAngles.z);

                sunLight.color = presets[selectedPreset].sunEmissionColor;

                sunLight.intensity = presets[selectedPreset].sunEmissionIntensity;
            }


            float t = 0;

            while (t < firstPartDuration)
            {
                if (!PauseManager.instance.Bool_IsGamePaused)
                {
                    t += Time.deltaTime;
                }

                yield return null;
            }

         
            StartCoroutine(DayTimeRoutinePart2());

            yield return null;
        }


        IEnumerator DayTimeRoutinePart2()
        {
           /* autoMode = true;
            while (!isTransitionAllowed)
            {
                yield return null;
            }
            while (!runtimeSkybox)
            {
                yield return null;
            }
          */

           
           

            while (currentTimer < timer)
            {
                if (!PauseManager.instance.Bool_IsGamePaused)
                {
                    currentTimer += Time.deltaTime;
                }


                yield return null;
            }

            currentDivision++;
            currentDivision %= howManyDivision+1;

            ratio = currentDivision / howManyDivision;

          
            if(currentDivision == 0)
            {
               
                currentDivision =0;
                selectedPreset++;
                selectedPreset %= presets.Count;
                StartCoroutine(DayTimeRoutinePart1());
            }
            else
            {
                currentTimer = 0;
                UpdateTransition();
                StartCoroutine(DayTimeRoutinePart2());
            }
            

            yield return null;
        }

        // Update is called once per frame
        void Update()
        {
            #region 
            if (autoMode 
                || 
                (isTransitionAllowed && !autoMode))
            {
                if (Input.GetKeyDown(key))
                {
                    autoMode = false;
                    StopAllCoroutines();
                   
                    currentTimer = 0;
                    currentDivision = 0;
                    ratio = 0;

                    selectedPreset++;
                    selectedPreset %= presets.Count;
                    StartCoroutine(EnableAutoModeRoutine());
                    //StartCoroutine(DayTimeRoutine());
                    UpdateTransition();
                }
            }
            #endregion
        }
        IEnumerator EnableAutoModeRoutine()
        {
            float t = 0;
            
            while (t < bypassAutoModeDuration)
            {
                if (!PauseManager.instance.Bool_IsGamePaused)
                {
                    t += Time.deltaTime;
                }


                yield return null;
            }

            autoMode = true;
            StartCoroutine(DayTimeRoutinePart1());

            yield return null;
        }

        void UpdateTransition()
        {
            SunTransition();
            SkyboxMaterialTransition();
            FogModeTransition();
            PostFxTransition();
        }

        void SunTransition()
        {
            #region 
            StopCoroutine(SunTransitionRoutine());
            StartCoroutine(SunTransitionRoutine()); 
            #endregion
        }

        IEnumerator SunTransitionRoutine()
        {
            #region
            isTransitionAllowed = false;
            float t = 0;
            float duration = transitionDuration;
            float currentSunRotationX = sun.transform.localEulerAngles.x;

            Color currentSunColor = sunLight.color;
            float currentSunIntensity = sunLight.intensity;

            while (t < 1)
            {
                t += Time.deltaTime / duration;


                float targetSunValue = Mathf.Lerp(presets[selectedPreset].newValue, presets[(selectedPreset + 1) % presets.Count].newValue, ratio);

                sun.transform.localEulerAngles =
                    new Vector3(Mathf.Lerp(currentSunRotationX, targetSunValue, t),
                                sun.transform.localEulerAngles.y,
                                sun.transform.localEulerAngles.z);

                Color targetSunLightValue = Color.Lerp(presets[selectedPreset].sunEmissionColor, presets[(selectedPreset + 1) % presets.Count].sunEmissionColor, ratio);
                sunLight.color = Color.Lerp(currentSunColor, targetSunLightValue, t);


                float targetSunLightIntensityValue = Mathf.Lerp(presets[selectedPreset].sunEmissionIntensity, presets[(selectedPreset + 1) % presets.Count].sunEmissionIntensity, ratio);
                sunLight.intensity = Mathf.Lerp(currentSunIntensity, targetSunLightIntensityValue, t);

                yield return null;
            }

            isTransitionAllowed = true;
            yield return null; 
            #endregion
        }

        void SkyboxMaterialTransition()
        {
            #region 
            StopCoroutine(SkyboxMaterialTransitionRoutine());
            StartCoroutine(SkyboxMaterialTransitionRoutine()); 
            #endregion
        }

        void CloneSkybox()
        {
            #region
            StopCoroutine(CloneSkyboxRoutine());
            StartCoroutine(CloneSkyboxRoutine());
            #endregion
        }
        IEnumerator CloneSkyboxRoutine()
        {
            #region
            yield return new WaitUntil(() => SceneManager.GetActiveScene() == SceneManager.GetSceneByName(sceneName));
            Material currentSkybox = RenderSettings.skybox;
            runtimeSkybox = Instantiate(currentSkybox);
            RenderSettings.skybox = runtimeSkybox;
            yield return null;
            #endregion

        }

        void InitSun()
        {
            if(sun)
            {
                sunLight = sun.GetComponent<Light>();
            }

        }

        IEnumerator SkyboxMaterialTransitionRoutine()
        {
            #region
            float t = 0;
            float duration = transitionDuration;


            float sunSize = runtimeSkybox.GetFloat("_SunSize");
            float exposure = runtimeSkybox.GetFloat("_Exposure");

            float sunSizeConvergence = runtimeSkybox.GetFloat("_SunSizeConvergence");
            float atmosphereThickness = runtimeSkybox.GetFloat("_AtmosphereThickness");

            
            

            while (t < 1)
            {
                t += Time.deltaTime / duration;

                float targetsunSizeValue = Mathf.Lerp(presets[selectedPreset].sunSize, presets[(selectedPreset + 1) % presets.Count].sunSize, ratio);
                float targetexposureSizeValue = Mathf.Lerp(presets[selectedPreset].exposure, presets[(selectedPreset + 1) % presets.Count].exposure, ratio);

                runtimeSkybox.SetFloat("_SunSize", Mathf.Lerp(sunSize, targetsunSizeValue, t));
                runtimeSkybox.SetFloat("_Exposure", Mathf.Lerp(exposure, targetexposureSizeValue, t));


                float targetsunSizeConvergenceSizeValue = Mathf.Lerp(presets[selectedPreset].sunSizeConvergence, presets[(selectedPreset + 1) % presets.Count].sunSizeConvergence, ratio);
                float targetatmosphereThicknessValue = Mathf.Lerp(presets[selectedPreset].atmosphereThickness, presets[(selectedPreset + 1) % presets.Count].atmosphereThickness, ratio);

                runtimeSkybox.SetFloat("_SunSizeConvergence", Mathf.Lerp(sunSizeConvergence, targetsunSizeConvergenceSizeValue, t));
                runtimeSkybox.SetFloat("_AtmosphereThickness", Mathf.Lerp(atmosphereThickness, targetatmosphereThicknessValue, t));



             


                yield return null;
            }

            yield return null;
            #endregion
        }
        
        void FogModeTransition()
        {
            #region
            StopCoroutine(FogModeTransitionRoutine());
            StartCoroutine(FogModeTransitionRoutine());
            #endregion
        }

        IEnumerator FogModeTransitionRoutine()
        {
            #region
            float t = 0;
            float duration = transitionDuration;

            float currentFogDensity = RenderSettings.fogDensity;
            Color currentFogColor   = RenderSettings.fogColor;
            //RenderSettings.fogColor

            while (t < 1)
            {
                t += Time.deltaTime / duration;

                float targefogDensityValue = Mathf.Lerp(presets[selectedPreset].density, presets[(selectedPreset + 1) % presets.Count].density, ratio);
                Color targetfogColorValue = Color.Lerp(presets[selectedPreset].fogColor, presets[(selectedPreset + 1) % presets.Count].fogColor, ratio);


                RenderSettings.fogDensity = Mathf.Lerp(currentFogDensity, targefogDensityValue, t);
                RenderSettings.fogColor = Color.Lerp(currentFogColor, targetfogColorValue, t);





               

                yield return null;
            }

            yield return null;
            #endregion
        }

        void InitPostEffect()
        {
            #region
            // Access volume profile
            volumeProfile = volume.profile;

            // Access Bloom Fx
            volumeProfile.TryGet(out bloom);

            // Access colorAdjustments Fx
            volumeProfile.TryGet(out colorAdjustments);

            // Access colorLookup Fx
            volumeProfile.TryGet(out colorLookup); 
            #endregion
        }

        void PostFxTransition()
        {
            #region
            StopCoroutine(PostFxTransitionRoutine());
            StartCoroutine(PostFxTransitionRoutine());
            #endregion
        }

        IEnumerator PostFxTransitionRoutine()
        {
            #region
            float t = 0;
            float duration = transitionDuration;

            float bloomThreshold = (float)bloom.threshold;
            float bloomIntensity = (float)bloom.intensity;
            float bloomScatter = (float)bloom.scatter;
           // Color bloomTint = (Color)bloom.tint;

            float colorAdjustmentsContrast = (float)colorAdjustments.contrast;
            //Color colorAdjustColorFilter = (Color)colorAdjustments.colorFilter;
            float colorAdjustSaturation = (float)colorAdjustments.saturation;

            float colorLookupContribution = (float)colorLookup.contribution;

            while (t < 1)
            {
                t += Time.deltaTime / duration;


                float targebloomThresholdValue = Mathf.Lerp(presets[selectedPreset].bloomThreshold, presets[(selectedPreset + 1) % presets.Count].bloomThreshold, ratio);
                float targebloomIntensityValue = Mathf.Lerp(presets[selectedPreset].bloomIntensity, presets[(selectedPreset + 1) % presets.Count].bloomIntensity, ratio);
                float targebloomScatterValue = Mathf.Lerp(presets[selectedPreset].bloomScatter, presets[(selectedPreset + 1) % presets.Count].bloomScatter, ratio);


                bloom.threshold.Override(Mathf.Lerp(bloomThreshold, targebloomThresholdValue, t));
                bloom.intensity.Override(Mathf.Lerp(bloomIntensity, targebloomIntensityValue, t));
                bloom.scatter.Override(Mathf.Lerp(bloomScatter, targebloomScatterValue, t));
                //bloom.tint.Override(Color.Lerp(bloomTint, presets[selectedPreset].bloomTint, t));

                float targecolorAdjustmentsValue = Mathf.Lerp(presets[selectedPreset].colorAdjustContrast, presets[(selectedPreset + 1) % presets.Count].colorAdjustContrast, ratio);
                colorAdjustments.contrast.Override(Mathf.Lerp(colorAdjustmentsContrast, targecolorAdjustmentsValue,t));
                // colorAdjustments.colorFilter.Override(Color.Lerp(colorAdjustColorFilter, presets[selectedPreset].colorAdjustColorFilter, t));

                float targecolorAdjustSaturatinValue = Mathf.Lerp(presets[selectedPreset].colorAdjustSaturatin, presets[(selectedPreset + 1) % presets.Count].colorAdjustSaturatin, ratio);
                colorAdjustments.saturation.Override(Mathf.Lerp(colorAdjustSaturation, targecolorAdjustSaturatinValue, t));

                float targecolorLookupContributionValue = Mathf.Lerp(presets[selectedPreset].colorLookupContribution, presets[(selectedPreset + 1) % presets.Count].colorLookupContribution, ratio);
                colorLookup.contribution.Override(Mathf.Lerp(colorLookupContribution, targecolorLookupContributionValue, t));

                yield return null;
            }

            yield return null;
            #endregion
        }
    }
}
