using UnityEngine;

[CreateAssetMenu(fileName = "WeaponConfig", menuName = "Custom/WeaponConfig")]
public class WeaponConfig : ScriptableObject
{
    public GameObject prefab;
    public string Name;
    /// <summary>
    /// Name of settings file
    /// </summary>
    public float Damage;
    public float FireRate;//How many shots per second
    public float DamageFluctuation;
    public int ClipSize;
    public float Precision;
    public float ReloadTime;
    /// <summary>
    /// Shotgun 8gauge, Bird
    /// </summary>
    public float SimultaniousBullets;
    public float CritChance;
    public float CritMultiplier;
    /// <summary>
    /// Stagger, how much staggers enemy on hit
    /// </summary>
    public float Stagger;
    /// <summary>
    /// How much does itinfluence next shot pistol little, AK a lot, in percent from 0 to 100 of players min and max precision, 100 would mean it will take 100% of players precision each shot
    /// </summary>
    public float Recoil;
    public float Weight;
    public AudioClip shootingClip;
    public ParticleSystem muzzleFlash;
    public float Accuracy;
    public float PointBlankRange;        // up to this distance, hit chance is 100% (if aiming at the target)
    public float OptimalRange;          // full hit chance up to this distance
    public float MaxEffectiveRange;     // hit chance drops to ~10% at this distance

}
