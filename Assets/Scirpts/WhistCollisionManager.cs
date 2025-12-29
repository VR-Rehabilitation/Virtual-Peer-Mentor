using UnityEngine;

public class WhistCollisionController : MonoBehaviour
{
    [Header("Collision Objects")]
    public GameObject whist;      // Object to check
    public GameObject targetArea;
    
    [Header("Spawning Settings")]
    public Transform energyPoint; // Spawn position
    public GameObject energyPrefab; // Prefab to spawn
    public float speed = 15f; 

    [Header("Effects")]
    public ParticleSystem particleEffect; 
    public GameObject TargetCollisionEffect;
    public GameObject TargetCollisionEffectPosition;

    private BoxCollider colliderA;
    private BoxCollider colliderB;
    
    private bool isCurrentlyColliding = false; 
    private bool hasTriggeredThisCollision = false; 

    void Start()
    {
        // Default to self 
        if (whist == null) whist = gameObject;

        // Ensure colliders exist
        colliderA = SetupCollider(whist);
        colliderB = SetupCollider(targetArea);
    }

    private BoxCollider SetupCollider(GameObject obj)
    {
        if (obj == null) return null;
        
        BoxCollider box = obj.GetComponent<BoxCollider>();
        if (box == null) box = obj.AddComponent<BoxCollider>();
        
        box.isTrigger = true; // Ensure they are triggers
        return box;
    }

    void Update()
    {
        if (colliderA == null || colliderB == null) return;

        bool currentCollisionState = CheckCollision();

        if (currentCollisionState)
        {
            // If collision just started
            if (!isCurrentlyColliding && !hasTriggeredThisCollision)
            {
                SpawnAndMovePrefab();
                hasTriggeredThisCollision = true; 
                Debug.Log("Collision started, effect triggered.");
            }
            isCurrentlyColliding = true;
        }
        else
        {
            // Collision ended
            if (isCurrentlyColliding)
            {
                Debug.Log("Collision ended, state reset.");
                hasTriggeredThisCollision = false; // Reset to allow next collision
            }
            isCurrentlyColliding = false;
        }
    }

    // Check intersection between bounds
    private bool CheckCollision()
    {
        return colliderA.bounds.Intersects(colliderB.bounds);
    }

    private void SpawnAndMovePrefab()
    {
        if (energyPrefab != null && energyPoint != null)
        {
            // Spawn energy object
            GameObject spawnedObject = Instantiate(energyPrefab, energyPoint.position, Quaternion.identity);

            // Play particles
            if (particleEffect != null)
            {
                particleEffect.Play();
                Debug.Log("Particle System restarted.");
            }
            else
            {
                Debug.LogWarning("Particle System not assigned in Inspector!");
            }

            // Play collision effect and sound
            if (TargetCollisionEffect != null && TargetCollisionEffectPosition != null)
            {
                Instantiate(TargetCollisionEffect, TargetCollisionEffectPosition.transform.position, Quaternion.identity);
                
                string[] audioClips = { "MotionSuccess1", "MotionSuccess2", "MotionSuccess3", "MotionSuccess4", "MotionSuccess5" };
                string randomClip = audioClips[Random.Range(0, audioClips.Length)];
                AudioManager.Instance.Play(randomClip);
            }

            // Add movement controller to the spawned object
            ProjectileController controller = spawnedObject.AddComponent<ProjectileController>();
            controller.speed = speed;

            Debug.Log("Prefab spawned and moving +Z.");
        }
        else
        {
            Debug.LogError("Energy Prefab or Energy Point missing in Inspector!");
        }
    }
}

// Simple projectile controller class
public class ProjectileController : MonoBehaviour
{
    public float speed = 15f;

    void Update()
    {
        // Move along Z axis
        transform.Translate(0, 0, speed * Time.deltaTime);
        
        // Safety destroy after 10 seconds to prevent leak
        Destroy(gameObject, 10f); 
    }
}