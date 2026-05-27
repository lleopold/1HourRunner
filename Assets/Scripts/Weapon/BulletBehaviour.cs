using UnityEngine;

public class BulletBehaviour : MonoBehaviour
{
    public float speed = 20f;
    public float maxDistance = 50f;
    public GameObject hitEffectPrefab;
    public GameObject hitEffectPrefabBloodCloud;

    private Vector3 _startPosition;
    private Vector3 _direction;
    private bool _isHitBullet; // true = damage already applied; false = miss bullet (passes through zombies)
    private int _zombieLayer;

    /// <summary>
    /// <paramref name="isHitBullet"/>: true = bullet visually tracks a zombie, damage already rolled.
    /// false = miss bullet, ignores zombie colliders.
    /// </summary>
    public void Initialize(Vector3 direction, bool isHitBullet = false)
    {
        _direction = direction.normalized;
        _direction.y = 0;
        _startPosition = transform.position;
        _isHitBullet = isHitBullet;
        _zombieLayer = LayerMask.NameToLayer("Zombie");

        hitEffectPrefab = Resources.Load<GameObject>("FX/Hit_02");
        hitEffectPrefabBloodCloud = Resources.Load<GameObject>("FX/Impact_blood");
    }

    void Update()
    {
        transform.position += _direction * speed * Time.deltaTime;

        if (Vector3.Distance(_startPosition, transform.position) >= maxDistance)
            DestroyBullet();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            Instantiate(hitEffectPrefabBloodCloud, transform.position, Quaternion.identity);
        }
        DestroyBullet();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.name == "Player" || other.name.Contains("Coin"))
            return;

        bool isZombie = other.gameObject.layer == _zombieLayer || other.gameObject.CompareTag("Zombie");

        if (isZombie)
        {
            if (_isHitBullet)
            {
                // Damage was already applied by the roll system — just despawn visually on contact
                DestroyBullet();
                return;
            }
            else
            {
                // Miss bullet — pass through zombies, no damage, no destroy
                return;
            }
        }

        // Non-zombie hit — spawn impact effect
        hitEffectPrefab = Resources.Load<GameObject>("FX/Hit_01");

        if (hitEffectPrefab != null && hitEffectPrefabBloodCloud != null)
        {
            var impact = Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            var impact2 = Instantiate(hitEffectPrefabBloodCloud, transform.position, Quaternion.identity);
            ParticleSystem hitPart = impact.GetComponent<ParticleSystem>();
            ParticleSystem hitPart2 = impact2.GetComponent<ParticleSystem>();
            hitPart?.Play();
            hitPart2?.Play();
            Destroy(impact, 2f);
            Destroy(impact2, 2f);
        }

        Destroy(gameObject);
    }

    private float GetWeaponDamage(float randomPercentage)
    {
        return WeaponConfigSingleton.Instance.WeaponConfig.Damage
               * (1 + UnityEngine.Random.Range(-randomPercentage, randomPercentage) / 100f);
    }

    private void DestroyBullet()
    {
        Destroy(gameObject);
    }
}
