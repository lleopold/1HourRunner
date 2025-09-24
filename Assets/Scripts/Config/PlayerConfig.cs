using UnityEngine;

[CreateAssetMenu(fileName = "PlayerConfig", menuName = "Custom/PlayerConfig")]
public class PlayerConfig : ScriptableObject
{
    public string Name;
    public GameObject Prefab;
    public float health;
    public float weight;
    public float strength;
    public float stamina;
    public float staminaRegenDelay;
    public float staminaRegenSpeed;
    public float speed; //walking speed
    public float acceleration;

    //All +- perct
    public float RunningSpeed_pct; // if 20 then 20% faster than Speed
    public float BackMovementPenalty_pct;

    public float recoilReduction;
    public float reloadSpeed;
    public float vision;

    public float InjuredPenalty;

    public AudioClip hitReceived;

    public float PrecisionAngleMin;
    public float PrecisionAngleMax;
    public float AimingSpeed;
    public float PrecisionAimingAngleStarting;

    /// <summary>
    /// Base value that drives movement-induced aim spread:
    /// - Walk spread growth : +movementAimingPenalty degrees per second
    /// - Run spread growth  : +movementAimingPenalty * 2 degrees per second
    /// - Initial burst when starting to move while aiming : (movementAimingPenalty)% of (max-min) range
    /// </summary>
    public float movementAimingPenalty = 25f;
}
