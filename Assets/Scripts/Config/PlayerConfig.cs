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

    ///Aiming precision
    //public float aimingPrecision;//Aiming precision

    /// <summary>
    /// Recoil reduction in percentage
    /// </summary>
    public float recoilReduction; //How much reciol from weapon player is able to handle in percentage. 100% is no recoil, 0% is full recoil
    public float reloadSpeed;
    public float vision;

    public float InjuredPenalty; //How much is movement impared by injury pct of injury * this so: 50% wounded -20% Injured movement = -10% to all speed 

    public AudioClip hitReceived;

    /// <summary>
    /// //ovo bi verovatno trebalo samo na oruzju da bude, ali za sada je i ovde recimo da si uber precizan onda mozes i na oruzju da spustis preciznos, recimo za pistolj min precisnost je 3 stepena, ti spustis na 2 kad ti ovaj ode na -1
    /// </summary>
    public float PrecisionAngleMin;
    public float PrecisionAngleMax;//ovo je kao aiming speed
    /// <summary>
    /// How fast player aims from max to min
    /// </summary>
    public float AimingSpeed;
    /// <summary>
    /// Aiming starts from here, recoil can make precision lower than this
    /// </summary>
    public float PrecisionAimingAngleStarting;


}
