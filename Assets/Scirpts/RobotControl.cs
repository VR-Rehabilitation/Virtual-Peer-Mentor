using UnityEngine;

public class RobotControl : MonoBehaviour
{
    [Header("Settings")]
    public float moveSpeed = 3f;
    public float bulletSpeed = 10f;
    public float verticalBoundary = 0.3f;
    
    [Header("References")]
    public GameObject robot;
    public GameObject bulletPrefab;
    public Transform bulletSpawnPoint;
    public GameObject upEffect, downEffect;
    public Transform upEffectPos, downEffectPos;

    private Vector3 targetPosition;
    private bool isMoving;
    private bool canTrigger = true;

    private void Update()
    {
        if (!isMoving || robot == null) return;

        robot.transform.position = Vector3.MoveTowards(robot.transform.position, targetPosition, moveSpeed * Time.deltaTime);
        
        // Check if reached destination
        if (Vector3.Distance(robot.transform.position, targetPosition) < 0.001f)
        {
            isMoving = false;
            SpawnBullet();
        }
    }

    // Called by external Trigger on the TargetAreas
    public void HandleInput(bool moveUp)
    {
        if (!canTrigger) return;

        // Determine new target position
        targetPosition = robot.transform.position;
        targetPosition.y = moveUp ? verticalBoundary : -verticalBoundary;
        
        isMoving = true;
        canTrigger = false;

        // Play Visual Effects
        GameObject effectToSpawn = moveUp ? upEffect : downEffect;
        Transform effectPos = moveUp ? upEffectPos : downEffectPos;
        
        if (effectToSpawn != null && effectPos != null)
        {
            Instantiate(effectToSpawn, effectPos.position, Quaternion.identity);
        }

        PlayRandomSuccessSound();
        
        // Reset trigger after delay
        Invoke(nameof(ResetTrigger), 0.5f);
    }

    private void ResetTrigger() => canTrigger = true;

    private void SpawnBullet()
    {
        if (bulletPrefab == null || bulletSpawnPoint == null) return;

        var bullet = Instantiate(bulletPrefab, bulletSpawnPoint.position, Quaternion.Euler(0, 0, -90));
        
        if (bullet.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.linearVelocity = Vector3.right * bulletSpeed;
        }
    }

    private void PlayRandomSuccessSound()
    {
        int index = Random.Range(1, 6); // 1 to 5
        AudioManager.Instance.Play($"MotionSuccess{index}");
    }
}