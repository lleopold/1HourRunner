using System.Text.RegularExpressions;
using UnityEngine;

public class LevelLogic : MonoBehaviour
{
    Player player1;
    void Start()
    {
        SpawnMainPlayer();
    }
    void Update()
    {
        if (player1 == null || player1.weaponGameObjectPrefab == null)
        {
            player1 = new Player();
            player1 = player1.CreatePlayer();
        }
    }
    public void SpawnMainPlayer()
    {
        player1 = new Player();
        player1 = player1.CreatePlayer();
        if (PlayerConfigSingleton.Instance.PlayerConfig.Prefab == null)
        {
            Debug.LogError("Prefab is not assigned in PlayerConfigSingleton");
            return;
        }
        player1.playerGameObjectInstance = Instantiate(PlayerConfigSingleton.Instance.PlayerConfig.Prefab, transform.position, transform.rotation);
        player1.playerGameObjectInstance.name = "Player";
        var weaponPrefab = WeaponConfigSingleton.Instance.WeaponConfig?.prefab;
        if (weaponPrefab == null)
        {
            Debug.LogWarning($"[LevelLogic] No prefab assigned for weapon {DataHolder.chosenWeapon} — skipping weapon spawn.");
        }
        else
        {
            player1.weaponGameObjectInstance = Instantiate(weaponPrefab, transform.position, transform.rotation);
            player1.AttachWeapon();
        }
        //SetPlayerPostitionOnCenterofTerrain();
    }
    void SetPlayerPostitionOnCenterofTerrain()
    {
        Terrain terrain = Terrain.activeTerrain;
        Vector3 terrainSize = terrain.terrainData.size;
        Vector3 terrainPosition = terrain.transform.position;
        Vector3 terrainCenter = terrainPosition + terrainSize / 2;
        player1.playerGameObjectInstance.transform.position = terrainCenter;
        float playerHeight = player1.playerGameObjectInstance.GetComponent<Renderer>().bounds.size.y;
        player1.playerGameObjectInstance.transform.position += player1.playerGameObjectInstance.transform.up * (playerHeight / 2);
    }
    Transform FindRecursive(Transform parent, string pattern)
    {
        Regex regex = new Regex(pattern);

        if (regex.IsMatch(parent.name))
        {
            return parent;
        }

        foreach (Transform child in parent)
        {
            Transform result = FindRecursive(child, pattern);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }
    public void UnPause()
    {
        Time.timeScale = 1;
    }
}
