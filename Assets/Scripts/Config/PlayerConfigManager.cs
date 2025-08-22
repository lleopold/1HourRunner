using UnityEngine;

[CreateAssetMenu(fileName = "PlayerConfigManager", menuName = "Custom/PlayerConfigManager")]
public class PlayerConfigManager : ScriptableObject
{
    public PlayerConfig defaultConfig; // Default configuration for prefabs without a specific configuration

    [System.Serializable]
    public struct PlayerPrefabConfig
    {
        public string prefabName;
        public PlayerConfig config;
    }

    public PlayerPrefabConfig[] prefabConfigs;

    public PlayerConfig GetConfig(string prefabName)
    {
        foreach (var prefabConfig in prefabConfigs)
        {
            if (prefabConfig.prefabName == prefabName)
            {
                return prefabConfig.config;
            }
        }

        return defaultConfig;
    }
}
