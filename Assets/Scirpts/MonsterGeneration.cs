using UnityEngine;

public class MonsterGeneration : MonoBehaviour
{
    [SerializeField] private GameObject monsterPrefab;
    public float spawnInterval = 2f;
    
    private float spawnTimer;
    private bool isHighSpawn = true; // Toggles between high and low spawn positions

    private void Update()
    {
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            SpawnMonster();
            spawnTimer = 0;
        }
    }

    private void SpawnMonster()
    {
        if (monsterPrefab == null) return;

        float yPos = isHighSpawn ? 0.45f : -0.15f;
        Vector3 spawnPos = new Vector3(2f, yPos, 2.5f);
        
        // Instantiate facing correct direction
        var monster = Instantiate(monsterPrefab, spawnPos, Quaternion.Euler(0, -90, 0));
        
        // Add movement logic
        // Note: Ideally this script should be on the prefab itself, but adding here to preserve legacy logic.
        if (monster.GetComponent<MonsterMovement>() == null)
        {
            monster.AddComponent<MonsterMovement>();
        }

        isHighSpawn = !isHighSpawn; // Toggle for next spawn
    }
}

public class MonsterMovement : MonoBehaviour
{
    private const float Speed = 1f;
    private readonly Vector3 destination = new Vector3(-3f, 0, 2.5f);

    private void Update()
    {
        // Keep Y position constant, move X and Z towards destination
        Vector3 target = new Vector3(destination.x, transform.position.y, destination.z);
        
        transform.position = Vector3.MoveTowards(transform.position, target, Speed * Time.deltaTime);

        // Destroy if reached destination
        if (Vector3.Distance(transform.position, target) < 0.1f)
        {
            Destroy(gameObject);
        }
    }
}