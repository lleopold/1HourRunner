using UnityEngine;

public class BulletBehaviour : MonoBehaviour
{
    public float speed = 20f; // Speed of the bullet
    public float maxDistance = 50f; // Maximum travel distance for bullet
    public GameObject hitEffectPrefab; // Prefab for the hit effect

    private Vector3 _startPosition; // To track distance traveled
    private Vector3 _direction; // Bullet's direction

    public void Initialize(Vector3 direction)
    {
        _direction = direction.normalized;
        _direction.y = 0; // Prevent bullet from going up or down
        _startPosition = transform.position;
    }

    void Update()
    {
        // Move the bullet forward
        transform.position += _direction * speed * Time.deltaTime;
        if (_direction.y != 0)
        {
            Debug.Log("Bullet going soewhere: X" + _direction.x + ";Y: " + _direction.y + "Z:" + _direction.z);
        }

        // Check if the bullet has traveled far enough
        if (Vector3.Distance(_startPosition, transform.position) >= maxDistance)
        {
            DestroyBullet();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Bullet hit: " + collision.gameObject.name);
        // Check for hit effect
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }

        // Destroy the bullet
        DestroyBullet();
    }
    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Bullet triggered by: {other.gameObject.name}");
        if (other.name == "Player" || other.name.Contains("Coin"))
        {
            return;
            //touches player on start, ignore that
        }
        //if zombie hit
        var enemy = other.transform.GetComponent<Enemy>();
        if ((other != null) && (other.gameObject.tag == "Zombie"))
        {
            float damage = GetWeaponDamage(WeaponConfigSingleton.Instance.WeaponConfig.DamageFluctuation);
            Debug.Log("Hit enemy: " + damage);
            enemy.DamageReceived(damage, transform.forward);
            hitEffectPrefab = Resources.Load<GameObject>("Weapons/Hit_02");//zombie hit effect

        }
        else
        {
            //how knows what was hit
            // Spawn hit effect at the bullet's position
            hitEffectPrefab = Resources.Load<GameObject>("Weapons/Hit_01");
        }

        if (hitEffectPrefab != null)
        {
            var impact = Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            ParticleSystem hitPart = impact.GetComponent<ParticleSystem>();
            hitPart.Play();
            Destroy(impact, 2f);
        }

        // Destroy the bullet after the hit
        Destroy(gameObject);
    }
    private float GetWeaponDamage(float randomPercentage)
    {
        return WeaponConfigSingleton.Instance.WeaponConfig.Damage * (1 + UnityEngine.Random.Range(-randomPercentage, randomPercentage) / 100f);
    }

    private void DestroyBullet()
    {
        Destroy(gameObject);
    }
}
