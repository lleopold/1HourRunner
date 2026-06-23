using UnityEngine;

[CreateAssetMenu(fileName = "EnemyConfig", menuName = "Custom/EnemyConfig")]
public class EnemyConfig : ScriptableObject
{
    public string name; // The name of the prefab
    public GameObject prefab; // The prefab itself
    public float health;
    public float timeBetweenAttacks;
    public float speed;
    [Range(0f, 100f)] public float staggerResistance; // 0 = full stagger, 100 = immune
    [Range(0f, 100f)] public float acceleration;       // 0-100, mapped to NavMeshAgent.acceleration (recovery ramp)
    [Range(0f, 1f)] public float hitSoundVolume = 0.4f; // volume of the get-hit SFX
    public float rage;
    public float meleeRadius;
    public float meleeDamage;
    public float meleeCooldown;
    public float meleeKnockback;
    public int[] meleeAnimation; // Array of animation IDs
    public int maxAmountOfRuns;
    public int maxAmountOfCharges;

    public float rangedDamage; // If enemy has a ranged attack
    public float rangedRadius;
    public float rangedCooldown;
    public float rangedKnockback;
    public int[] rangedAnimation; // Array of animation IDs


    // ── Scream / Shockwave rally ───────────────────────────────────────────
    [Header("Scream / Shockwave")]
    public bool canScream = true;              // master toggle per enemy type
    public float screamRadius = 6f;            // OverlapSphere radius for who gets agonized
    public int screamMinNeighbors = 3;         // need this many UNboosted zombies nearby to bother screaming
    public float screamProximityRadius = 0f;   // "very close to player"; <=0 falls back to meleeRadius*2
    public float screamProximityTime = 7f;     // continuous seconds near player before it "snaps"
    public float screamYellDuration = 1.2f;    // ZScream -> Zagonizing handoff delay (seconds)
    public float screamMaxDuration = 5f;       // safety: force the blast if the anim event never fires
    public float agonyMaxDuration = 4f;        // safety: force boost if the agony anim event never fires
    [Range(1f, 3f)] public float boostMultiplier = 1.2f; // permanent, non-stacking speed multiplier
    public GameObject screamShockwaveVfx;      // big ground distortion (e.g. VFX_Shockwave_01_Distortion_Big_1s)
    public GameObject agonyBoostVfx;           // small red ring above head (e.g. VFX_Shockwave_02_Red_1s)
    [Range(0.05f, 3f)] public float agonyBoostVfxScale = 1f; // uniform scale for the red boost ring


    // Add other configuration fields as needed
}
