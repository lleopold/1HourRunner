using UnityEngine;

[CreateAssetMenu(fileName = "EnemyConfig", menuName = "Custom/EnemyConfig")]
public class EnemyConfig : ScriptableObject
{
    public string name; // The name of the prefab
    public GameObject prefab; // The prefab itself
    public float health;
    public float timeBetweenAttacks;
    public float speed;
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


    // Add other configuration fields as needed
}
