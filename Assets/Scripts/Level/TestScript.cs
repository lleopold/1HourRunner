using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;

public class TestScript : MonoBehaviour
{
    public float spawnRadius = 5f;   // Radius around the player to spawn zombies

    public GameObject zombiePrefab;
    public float timeBetweenSpawns = 7f;
    public int minZombiesOnScreen = 1;
    public int maxZombiesOnScreen = 1;

    private float lastSpawnTime;
    private GameObject[] zombies;
    public List<string> zombiePrefabNames;

    public void Start()
    {
        lastSpawnTime = Time.time;
        //zombiePrefabNames = new List<string> { "fem_zombie_2", "fem_zombie_3", "male_zombie_1", "male_zombie_2", "male_zombie_3", "male_zombie_4" };
        zombiePrefabNames = new List<string> { "male_zombie_II_1_Ragdoll" }; //"fem_zombie_II_1",
    }
    int getNumberOfZombiesOnScreen()
    {
        return zombies.Length;
    }
    void spawnOneZombie()
    {
        if (getNumberOfZombiesOnScreen() < maxZombiesOnScreen)
        {
            SpawnZombies();
        }
    }

    // Update is called once per frame
    void Update()
    {
        zombies = GameObject.FindGameObjectsWithTag("Zombie");
        spawnOneZombie();
        // Check if it's time to spawn zombies, just one zombie on screen
        //if (Time.time - lastSpawnTime >= timeBetweenSpawns && zombies.Length < minZombiesOnScreen)
        //{
        //    SpawnZombies();
        //    lastSpawnTime = Time.time;
        //}
    }

    public void SpawnZombies()
    {
        string zombiePrefabName = zombiePrefabNames[Random.Range(0, zombiePrefabNames.Count)];
        EnemyConfigManager configManager = Resources.Load<EnemyConfigManager>("Config/Enemy/EnemyConfigManager"); // Load the config manager
        EnemyConfig enemyConfig = configManager.GetConfig(zombiePrefabName);
        GameObject zombiePrefab = Resources.Load<GameObject>("Enemies/" + zombiePrefabName);
        if (zombiePrefab == null)
        {
            Debug.LogError("Zombie prefab not found: " + zombiePrefabName);
            return;
        }
        // Get the position of the player
        GameObject player = GameObject.Find("Player");
        GameObject cam = Camera.main.gameObject;
        Vector3 playerPosition = player.transform.position;

        // Calculate a random position outside the camera's view
        float cameraHeight = cam.GetComponent<Camera>().orthographicSize;
        float cameraWidth = cameraHeight * cam.GetComponent<Camera>().aspect;
        float spawnOffset = spawnRadius + Mathf.Max(cameraWidth, cameraHeight);

        // Generate random offsets for x and z coordinates
        float randomX = Random.Range(-spawnOffset, spawnOffset);
        float randomZ = Random.Range(-spawnOffset, spawnOffset);

        // Instantiate the zombie at the player's position with the random offset
        Vector3 zombiePosition = new Vector3(playerPosition.x + randomX, 0f, playerPosition.z + randomZ);

        // Instantiate the zombie prefab
        if (enemyConfig.prefab == null)
        {
            enemyConfig.prefab = Resources.Load<GameObject>("Enemies/" + zombiePrefabName);
            return;
        }
        GameObject newZombie = Instantiate(enemyConfig.prefab, zombiePosition, Quaternion.identity);
        newZombie.GetComponent<Enemy>()._player = player;
        newZombie.GetComponentInChildren<BillbBoard>().cam = cam.transform;
        DataHolder.EnemiesSpawned++;
        newZombie.name = zombiePrefabName + DataHolder.EnemiesSpawned.ToString();
        newZombie.tag = "Zombie";
        newZombie.active = true;
    }


    public void LoadMainScreen()
    {
        SceneManager.LoadScene("Start");
    }

    public void QuitGAme()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // If not running in the Unity Editor, quit the application
        Application.Quit();
#endif
    }


}
