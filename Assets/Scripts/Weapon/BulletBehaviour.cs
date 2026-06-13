using UnityEngine;
using LUZEMRIK.BloodDecals;

public class BulletBehaviour : MonoBehaviour
{
    public float speed = 20f;
    public float maxDistance = 50f;
    public GameObject hitEffectPrefab;
    public GameObject hitEffectPrefabBloodCloud;

    // Blood FX on zombie hit
    private GameObject _bloodSplashPrefab;     // Resources/FX/BloodSplash (CFXR2 directional)
    private BloodDecalAsset _bloodPuddleAsset; // Resources/FX/BloodPuddle (LUZEMRIK)
    private static readonly Color32 BloodColor = new Color32(110, 0, 0, 255);

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
        _bloodSplashPrefab = Resources.Load<GameObject>("FX/BloodSplash");
        _bloodPuddleAsset = Resources.Load<BloodDecalAsset>("FX/BloodPuddle");
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
                // Damage was already applied by the roll system — spawn blood, then despawn
                SpawnBlood(transform.position, other);
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

    private void SpawnBlood(Vector3 hitPoint, Collider zombie)
    {
        // Directional splash particle, sprayed along bullet travel direction
        if (_bloodSplashPrefab != null)
        {
            var splash = Instantiate(_bloodSplashPrefab, hitPoint,
                Quaternion.LookRotation(_direction == Vector3.zero ? Vector3.forward : _direction));
            Destroy(splash, 3f);
        }

        // Ground puddle decal under the zombie (URP DecalProjector via LUZEMRIK manager)
        if (_bloodPuddleAsset != null && BloodDecalManager.Instance != null)
        {
            Vector3 origin = zombie.bounds.center;
            int groundMask = ~(1 << _zombieLayer); // ignore zombie colliders when finding floor
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 10f, groundMask, QueryTriggerInteraction.Ignore))
            {
                float s = Random.Range(1.2f, 2.0f);
                BloodDecalManager.Instance.AddDecal(
                    _bloodPuddleAsset, BloodColor, hit.point + hit.normal * 0.01f, hit.normal,
                    new Vector3(s, s, 1f));
            }
        }
    }

    private void DestroyBullet()
    {
        Destroy(gameObject);
    }
}
