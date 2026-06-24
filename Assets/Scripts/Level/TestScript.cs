using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TestScript : MonoBehaviour
{
    // Add near your spawner fields
    [SerializeField] float minFromPlayer = 12f;
    [SerializeField] float maxFromPlayer = 28f;
    [SerializeField] LayerMask containerMask;     // set to Containers layer in Inspector
    [SerializeField] float navSampleMax = 6f;     // how far to search for nearest NavMesh point
    [SerializeField] int maxTries = 20;

    [SerializeField] bool autoSpawn = false;   // off for debug: spawn only via Debug1 button

    public float spawnRadius = 5f;   // Radius around the player to spawn zombies

    public GameObject zombiePrefab;
    public float timeBetweenSpawns = 1f;
    public int minZombiesOnScreen = 3;
    public int maxZombiesOnScreen = 9;

    private float lastSpawnTime;
    private GameObject[] zombies;
    public List<string> zombiePrefabNames;

    public void Start()
    {
        lastSpawnTime = Time.time;
        //zombiePrefabNames = new List<string> { "fem_zombie_2", "fem_zombie_3", "male_zombie_1", "male_zombie_2", "male_zombie_3", "male_zombie_4" };
        zombiePrefabNames = new List<string> { "male_zombie_II_1_Ragdoll", "male_zombie_sports_1_ragdoll" }; //"fem_zombie_II_1",

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
        if (!autoSpawn) return;   // debug: spawning driven by Debug1 button only

        if (Time.time - lastSpawnTime >= timeBetweenSpawns && zombies.Length < maxZombiesOnScreen)
        {
            // Only reset the timer when a zombie actually spawned — a failed spawn shouldn't burn the cooldown.
            if (SpawnZombies())
                lastSpawnTime = Time.time;
        }
    }

    public bool SpawnZombies()
    {
        string zombiePrefabName = zombiePrefabNames[Random.Range(0, zombiePrefabNames.Count)];
        var configManager = Resources.Load<EnemyConfigManager>("Config/Enemy/EnemyConfigManager");
        if (configManager == null) { Debug.LogError("EnemyConfigManager not found at Resources/Config/Enemy/EnemyConfigManager"); return false; }

        // GetConfig may return null (unknown name + null defaultConfig) — don't dereference it blindly.
        var enemyConfig = configManager.GetConfig(zombiePrefabName);
        var prefab = (enemyConfig != null ? enemyConfig.prefab : null)
                     ?? Resources.Load<GameObject>("Enemies/" + zombiePrefabName);
        if (!prefab)
        {
            Debug.LogError($"Zombie prefab not found: '{zombiePrefabName}' — no config prefab and no Resources/Enemies/{zombiePrefabName}.prefab. Skipping.");
            return false;
        }

        var player = GameObject.Find("Player");
        var cam = Camera.main;
        if (!player || !cam)
        {
            Debug.LogError("Player or Main Camera not found in scene — cannot spawn zombies.");
            return false;
        }

        if (!TryGetSpawnPoint(player.transform.position, cam, out var pos))
        {
            // fallback: spawn behind camera on NavMesh
            var back = cam.transform.position - cam.transform.forward * 10f;
            if (!UnityEngine.AI.NavMesh.SamplePosition(back, out var hit, navSampleMax, UnityEngine.AI.NavMesh.AllAreas))
                return false;
            pos = hit.position;
        }

        // Instantiate inactive so we can inject the player reference BEFORE Enemy.Awake runs.
        // Awake builds AI states that dereference _player.transform; activating an already-set
        // instance avoids the null-ref race (the old `enemy._player = player` ran too late).
        bool prefabWasActive = prefab.activeSelf;
        prefab.SetActive(false);
        var go = Instantiate(prefab, pos, Quaternion.identity);
        prefab.SetActive(prefabWasActive);

        // Tag first so a later missing-component error can't leave the zombie untagged (uncounted → spawn spam).
        go.tag = "Zombie";

        var enemy = go.GetComponent<Enemy>();
        if (enemy != null) enemy._player = player;
        else Debug.LogWarning($"Spawned zombie '{go.name}' has no Enemy component on its root.");

        // Activate (runs Awake) with the player injected AND the original "(Clone)" name intact,
        // so Enemy.Awake's config lookup — which strips "(Clone)" — still resolves. Rename after.
        try
        {
            go.SetActive(true);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Exception while activating spawned zombie '{go.name}': {ex}");
            Destroy(go);
            return false;
        }

        DataHolder.EnemiesSpawned++;
        go.name = zombiePrefabName + DataHolder.EnemiesSpawned;

        var billboard = go.GetComponentInChildren<BillbBoard>();
        if (billboard != null) billboard.cam = cam.transform;

        // ensure agent is validly placed
        var agent = go.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent) agent.Warp(pos);

        return true;
    }
    bool TryGetSpawnPoint(Vector3 playerPos, Camera cam, out Vector3 spawnPos)
    {
        spawnPos = default;
        for (int i = 0; i < maxTries; i++)
        {
            // pick random point in ring around player
            float r = Random.Range(minFromPlayer, maxFromPlayer);
            float ang = Random.value * Mathf.PI * 2f;
            Vector3 p = playerPos + new Vector3(Mathf.Cos(ang) * r, 2f, Mathf.Sin(ang) * r); // y up a bit

            // project to NavMesh
            if (!UnityEngine.AI.NavMesh.SamplePosition(p, out var hit, navSampleMax, UnityEngine.AI.NavMesh.AllAreas))
                continue;

            Vector3 candidate = hit.position;

            // not inside a container (uses container colliders)
            if (Physics.CheckSphere(candidate + Vector3.up * 0.5f, 0.5f, containerMask, QueryTriggerInteraction.Ignore))
                continue;

            // path to player must exist
            var path = new UnityEngine.AI.NavMeshPath();
            if (!UnityEngine.AI.NavMesh.CalculatePath(candidate, playerPos, UnityEngine.AI.NavMesh.AllAreas, path))
                continue;
            if (path.status != UnityEngine.AI.NavMeshPathStatus.PathComplete)
                continue;

            spawnPos = candidate;
            return true;
        }
        return false;
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
