using CodeMonkey.HealthSystemCM;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{

    //How huch is initial damage 
    [SerializeField] public float hitPotential;
    //How much +- damage can be
    [SerializeField] public float hitVariationPercent;
    private float realDamage;

    public float bulletSpeed = 0f;
    public Vector3 velocity;
    public float destroyDelay = 2f;


    void Start()
    {
        Debug.Log("Bullet created");
        realDamage = hitPotential * (1 + UnityEngine.Random.Range(0, hitVariationPercent / 100));
        //Vector3 forceDirection = Vector3.forward; // Change this to your desired direction
        velocity = velocity.normalized * bulletSpeed;
        Invoke("DestroyBullet", destroyDelay);
    }

    void Update()
    {
        transform.position += velocity * Time.deltaTime;
    }
    void DestroyBullet()
    {
        // Cancel the Invoke in case the bullet is destroyed through collision before the delay
        CancelInvoke("DestroyBullet");

        // Destroy the bullet GameObject
        Destroy(gameObject);
    }
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "EnemyTag")
        {
            collision.gameObject.GetComponent<Enemy>().DamageReceived(realDamage, transform.forward);
            //collision.gameObject.GetComponent<HealthSystem>().Damage(realDamage); //hm
            // Optionally play animation/particle effect/sound
            Destroy(gameObject);
        }
        // Check if the bullet collided with an object
        //Destroy(gameObject);
    }
}
