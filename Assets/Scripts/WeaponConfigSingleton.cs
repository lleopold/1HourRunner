using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;


public class WeaponConfigSingleton
{
    private static WeaponConfigSingleton instance;
    private static WeaponEnum chosenWeapon;
    public static WeaponConfig weaponConfig;
    private WeaponConfigManager weaponConfigManager;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic()
    {
        instance = null;
        weaponConfig = null;
    }

    public WeaponConfig WeaponConfig
    {
        get
        {
            if (weaponConfigManager == null)
            {
                weaponConfigManager = Resources.Load<WeaponConfigManager>("Config/Weapon/WeaponConfigManager");
                if (weaponConfigManager == null) { Debug.LogError("WeaponConfigManager asset not found in Resources/Config/Weapon/"); return null; }
            }
            return weaponConfigManager.GetConfig(DataHolder.chosenWeapon.ToString());
        }
        set
        {
            weaponConfig = value; // Fix the variable name here
        }
    }
    public static WeaponConfigSingleton Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new WeaponConfigSingleton();
            }
            else if (DataHolder.chosenWeapon != chosenWeapon)
            {
                // Weapon changed since last instantiation — reload config
                chosenWeapon = DataHolder.chosenWeapon;
                if (instance.weaponConfigManager != null)
                    weaponConfig = instance.weaponConfigManager.GetConfig(chosenWeapon.ToString());
            }
            return instance;
        }
    }
    private WeaponConfigSingleton()
    {
        // Set default values
        chosenWeapon = DataHolder.chosenWeapon;
        weaponConfigManager = Resources.Load<WeaponConfigManager>("Config/Weapon/WeaponConfigManager");
        if (weaponConfigManager == null) { Debug.LogError("WeaponConfigManager asset not found in Resources/Config/Weapon/"); return; }
        weaponConfig = weaponConfigManager.GetConfig(chosenWeapon.ToString());

    }

    public void SaveConfigToFile()
    {
        string jsonData = JsonUtility.ToJson(Instance.WeaponConfig);
        File.WriteAllText("c:\\Temp\\" + DataHolder.chosenWeapon.ToString() + ".txt", jsonData);
    }

    public WeaponConfig LoadWeaponConfigFromFile(string filePath)
    {
        filePath = "c:\\Temp\\" + DataHolder.chosenWeapon.ToString() + ".txt";
        // Check if the file exists
        if (File.Exists(filePath))
        {
            // Read the JSON data from the file
            string jsonData = File.ReadAllText(filePath);

            // Deserialize the JSON data into a WeaponConfig object
            WeaponConfig WeaponConfig = JsonUtility.FromJson<WeaponConfig>(jsonData);

            return WeaponConfig;
        }
        else
        {
            Debug.LogError("File not found: " + filePath);
            return null;
        }
    }


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
