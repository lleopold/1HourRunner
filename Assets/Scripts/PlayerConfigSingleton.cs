using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerConfigSingleton
{
    private static PlayerConfigSingleton instance;
    private static PlayerEnum chosenPlayer;
    private PlayerConfig playerConfig;
    private PlayerConfigManager PlayerConfigManager;

    public PlayerConfig PlayerConfig
    {
        get
        {
            return PlayerConfigManager.GetConfig(DataHolder.ChosenPlayer.ToString()); ;
        }
        set
        {
            playerConfig = value;
        }
    }
    public static PlayerConfigSingleton Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new PlayerConfigSingleton();
            }
            return instance;
        }
    }
    private PlayerConfigSingleton()
    {
        // Set default values
        chosenPlayer = DataHolder.ChosenPlayer;
        PlayerConfigManager = Resources.Load<PlayerConfigManager>("Config/Player/PlayerConfigManager");
        playerConfig = PlayerConfigManager.GetConfig(DataHolder.ChosenPlayer.ToString());

    }
    
    public void SaveConfigToFile()
    {
        string jsonData = JsonUtility.ToJson(Instance.PlayerConfig);
        File.WriteAllText("c:\\Temp\\"+DataHolder.ChosenPlayer.ToString()+".txt", jsonData);
    }

    public PlayerConfig LoadPlayerConfigFromFile(string filePath)
    {
        filePath = "c:\\Temp\\" + DataHolder.ChosenPlayer.ToString() + ".txt";
        // Check if the file exists
        if (File.Exists(filePath))
        {
            // Read the JSON data from the file
            string jsonData = File.ReadAllText(filePath);

            // Deserialize the JSON data into a PlayerConfig object
            PlayerConfig playerConfig = JsonUtility.FromJson<PlayerConfig>(jsonData);

            return playerConfig;
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
