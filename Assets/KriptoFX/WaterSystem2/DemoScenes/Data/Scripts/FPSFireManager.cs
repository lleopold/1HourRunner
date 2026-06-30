using System;
using UnityEngine;
using System.Collections;
using KWS;
using Random = UnityEngine.Random;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif


public class FPSFireManager : MonoBehaviour
{
    public GameObject DefaultImpactEffect;
    [Space]
    public float BulletDistance = 100;
    public GameObject MuzzleFlashEffect;

    public GameObject[] KWS_ImpactEffects;
    public GameObject   KWS_BulletTracerEffect;
    public GameObject   AudioSource;
    public AudioClip[]  AudioClips;
    
    public  float        FireRate        = 0.1f; 

    private Coroutine    autoFireRoutine = null;
    
    bool RayPlaneIntersection(Ray ray, float planeY, out Vector3 hitPoint)
    {
        hitPoint = Vector3.zero;

        float dy = ray.direction.y;

        if (Mathf.Abs(dy) < 0.0001f)
            return false;

        float t = (planeY - ray.origin.y) / dy;

        if (t < 0) 
            return false;

        hitPoint = ray.origin + ray.direction * t;
        return true;
    }



    void Update () 
    {
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
#else
        if (Input.GetMouseButtonDown(0))
#endif
        {
            if (WaterSystem.IsCameraPartialUnderwater) CreateUnderwaterEffect();
            else CreateEffect();
        }
	    
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Mouse.current.middleButton.wasPressedThisFrame)
#else
        if (Input.GetKeyDown(KeyCode.Mouse2))
#endif
        {
            StartAutoFire();
        }

#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Mouse.current.middleButton.wasReleasedThisFrame)
#else
         if (Input.GetKeyUp(KeyCode.Mouse2))
#endif
       
        {
            StopAutoFire();
        }
    }
    
    void StartAutoFire()
    {
        if (autoFireRoutine != null) return;

        autoFireRoutine = StartCoroutine(AutoFire());
    }

    void StopAutoFire()
    {
        if (autoFireRoutine != null)
        {
            StopCoroutine(autoFireRoutine);
            autoFireRoutine = null;
        }
    }

    IEnumerator AutoFire()
    {
        while (true)
        {
            if (WaterSystem.IsCameraPartialUnderwater) CreateUnderwaterEffect();
            else CreateEffect();
            yield return new WaitForSeconds(FireRate);
        }

    }

    void CreateUnderwaterEffect()
    {
        if (WaterSystem.IsCameraPartialUnderwater)
        {
            
            if (WaterSystem.IsCameraPartialUnderwater && KWS_BulletTracerEffect != null)
            {
                var tracerEffectIstance = Instantiate(KWS_BulletTracerEffect, transform.position, transform.rotation);
                Destroy(tracerEffectIstance, 4);
            }

        }
    }
    
    private void CreateEffect()
    {
        var ray              = new Ray(transform.position, transform.forward);
        var intersectKWS     = false;
        var intersectSurface = false;
        var kwsHitPoint      = Vector3.zero;
            
        var waterLevel = KWS.WaterSystem.Instance.WaterLevel;
        if (RayPlaneIntersection(ray, waterLevel, out Vector3 waterPoint))
        {
            intersectKWS = true;
            kwsHitPoint  = waterPoint;
        }
            
            
        RaycastHit hit;
        if(Physics.Raycast(ray, out hit, BulletDistance))
        {
            if(hit.point.y > waterLevel) intersectSurface = true;
        }
            
        if (intersectSurface || intersectKWS)
        {
            
            GameObject effect = null;
            if (intersectSurface)
            {
                effect = DefaultImpactEffect;
            }
            else if(intersectKWS)
            {
                var randIdx = Random.Range(0, KWS_ImpactEffects.Length);
                   
                effect     = KWS_ImpactEffects[randIdx];
                hit.point  = kwsHitPoint;
                hit.normal = Vector3.up;
            }
                
              
            if (effect == null) return;
            
            

            var effectIstance = Instantiate(effect, hit.point, new Quaternion());
            effectIstance.transform.LookAt(hit.point + hit.normal);
            var scale = effectIstance.transform.localScale;
            effectIstance.transform.localScale *= Random.Range(0.8f, 1.5f);
                
            Destroy(effectIstance, 20);

            var muzzleFlashEffectIstance = Instantiate(MuzzleFlashEffect, transform.position, transform.rotation);
            Destroy(muzzleFlashEffectIstance, 4);

            var soundInstance = Instantiate(AudioSource, transform.position, transform.rotation);
            var audioSource   = soundInstance.GetComponent<AudioSource>();
            audioSource.clip = AudioClips[Random.Range(0, AudioClips.Length)];
            audioSource.Play();
            Destroy(soundInstance, 2);
        }
    }

}
