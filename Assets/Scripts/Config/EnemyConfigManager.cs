using UnityEngine;

[CreateAssetMenu(fileName = "EnemyConfigManager", menuName = "Custom/EnemyConfigManager")]
public class EnemyConfigManager : ScriptableObject
{
    public EnemyConfig defaultConfig; // Default configuration for prefabs without a specific configuration

    [System.Serializable]
    public struct EnemyPrefabConfig
    {
        public string prefabName;
        public EnemyConfig config;
    }

    public EnemyPrefabConfig[] prefabConfigs;

    public EnemyConfig GetConfig(string prefabName)
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
