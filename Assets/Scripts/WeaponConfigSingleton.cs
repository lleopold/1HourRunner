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

    public WeaponConfig WeaponConfig
    {
        get
        {
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
            return instance;
        }
    }
    private WeaponConfigSingleton()
    {
        // Set default values
        chosenWeapon = DataHolder.chosenWeapon;
        weaponConfigManager = Resources.Load<WeaponConfigManager>("Config/Weapon/WeaponConfigManager");
        weaponConfig = weaponConfigManager.GetConfig(DataHolder.chosenWeapon.ToString());

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
