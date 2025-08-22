using UnityEngine;

[CreateAssetMenu(fileName = "WeaponConfigManager", menuName = "Custom/WeaponConfigManager")]
public class WeaponConfigManager : ScriptableObject
{
    public WeaponConfig defaultConfig; // Default configuration for prefabs without a specific configuration

    [System.Serializable]
    public struct WeaponPrefabConfig
    {
        public string prefabName;
        public WeaponConfig config;
    }

    public WeaponPrefabConfig[] prefabConfigs;

    public WeaponConfig GetConfig(string prefabName)
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
