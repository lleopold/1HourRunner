using UnityEngine;

public class DataHolder : MonoBehaviour
{
    // Define your data variables
    private static PlayerEnum chosenPlayer;
    public static WeaponEnum chosenWeapon;
    public static WeaponType weaponType;
    public static int EnemiesKilled;
    public static int EnemiesSpawned;

    public static PlayerEnum ChosenPlayer
    {
        get
        {
            return chosenPlayer;
        }
        set
        {
            chosenPlayer = value;
        }
    }
    // Other data variables can be added as needed

    void Start()
    {
        // Make sure the object isn't destroyed when loading a new scene
        DontDestroyOnLoad(this.gameObject);
    }
}
public enum WeaponType
{
    H1,
    H2
}
