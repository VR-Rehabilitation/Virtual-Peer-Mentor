using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnergyTideManager : MonoBehaviour
{
    [Header("Tracking Targets")]
    public GameObject leftAnkle;
    public GameObject rightAnkle;
    public GameObject upTargetArea;
    public GameObject downTargetArea;
    
    [Header("Game Objects")]
    public GameObject[] tideObjects; // Array of objects to be thrown
    public GameObject healthCrystal; // Destination target
    
    [Header("Effects")]
    public GameObject particleEffectPrefab; // Destination effect
    public Transform particleEffectPosition;
    public GameObject PointParticle;
    public GameObject UpAreaPointParticlePosition;
    public GameObject DownAreaPointParticlePosition;
    
    // State Tracking
    private bool leftAnkleInUpArea = false;
    private bool rightAnkleInUpArea = false;
    private bool leftAnkleInDownArea = false;
    private bool rightAnkleInDownArea = false;

    // Logic State
    // 0: Initial, 1: Last triggered Up, 2: Last triggered Down
    private int lastTriggerAreaState = 0; 
    private bool hasFirstUpTrigger = false;
    
    private void Start()
    {
        if (leftAnkle == null || rightAnkle == null || upTargetArea == null || 
            downTargetArea == null || tideObjects == null || healthCrystal == null)
        {
            Debug.LogError("EnergyTideManager: Please assign all references in Inspector!");
        }
    }
    
    private void Update()
    {
        CheckUpAreaCollision();
        
        // Only check down area if we have started the cycle
        if (hasFirstUpTrigger)
        {
            CheckDownAreaCollision();
        }
    }
    
    private void CheckUpAreaCollision()
    {
        leftAnkleInUpArea = IsObjectInArea(leftAnkle, upTargetArea);
        rightAnkleInUpArea = IsObjectInArea(rightAnkle, upTargetArea);
        
        if (leftAnkleInUpArea && rightAnkleInUpArea)
        {
            // Case 1: Very first trigger
            if (!hasFirstUpTrigger)
            {
                TriggerUpEffect();
                hasFirstUpTrigger = true;
            }
            // Case 2: Subsequent trigger (must have come from Down)
            else if (lastTriggerAreaState == 2)
            {
                TriggerUpEffect();
            }
        }
    }

    private void TriggerUpEffect()
    {
        LaunchEnergyParticle();
        lastTriggerAreaState = 1; // Mark as "Up Triggered"

        if (PointParticle != null && UpAreaPointParticlePosition != null)
        {
            Instantiate(PointParticle, UpAreaPointParticlePosition.transform.position, Quaternion.identity);
            PlayRandomSuccessSound();
            StartCoroutine(PlaySoundAfterDelay("EnergyIn", 1f));
        }
    }
    
    private void CheckDownAreaCollision()
    {
        leftAnkleInDownArea = IsObjectInArea(leftAnkle, downTargetArea);
        rightAnkleInDownArea = IsObjectInArea(rightAnkle, downTargetArea);
        
        // Only trigger if last action was "Up"
        if (leftAnkleInDownArea && rightAnkleInDownArea && lastTriggerAreaState == 1)
        {
            TriggerDownEffect();
        }
    }

    private void TriggerDownEffect()
    {
        LaunchEnergyParticle();
        lastTriggerAreaState = 2; // Mark as "Down Triggered"

        if (PointParticle != null && DownAreaPointParticlePosition != null)
        {
            Instantiate(PointParticle, DownAreaPointParticlePosition.transform.position, Quaternion.identity);
            PlayRandomSuccessSound();
            StartCoroutine(PlaySoundAfterDelay("EnergyIn", 1f));
        }
    }
    
    private void PlayRandomSuccessSound()
    {
        string[] audioClips = { "MotionSuccess1", "MotionSuccess2", "MotionSuccess3", "MotionSuccess4", "MotionSuccess5" };
        string randomClip = audioClips[Random.Range(0, audioClips.Length)];
        AudioManager.Instance.Play(randomClip);
    }

    private IEnumerator PlaySoundAfterDelay(string soundName, float delay)
    {
        yield return new WaitForSeconds(delay);
        AudioManager.Instance.Play(soundName);
    }
    
    private bool IsObjectInArea(GameObject obj, GameObject area)
    {
        if (obj == null || area == null) return false;

        Collider objCollider = obj.GetComponent<Collider>();
        Collider areaCollider = area.GetComponent<Collider>();
        
        if (objCollider == null || areaCollider == null) return false;
        
        return objCollider.bounds.Intersects(areaCollider.bounds);
    }
    
    private void LaunchEnergyParticle()
    {
        GameObject selectedTideObject = GetRandomAvailableTideObject();
        if (selectedTideObject == null) return;
        
        // Start flight coroutine
        StartCoroutine(FlyToTarget(selectedTideObject, healthCrystal.transform.position));
        
        ScoreManager.Instance?.AddScore(0.5f);
    }
    
    private GameObject GetRandomAvailableTideObject()
    {
        List<GameObject> existingTideObjects = new List<GameObject>();
        
        foreach (GameObject obj in tideObjects)
        {
            if (obj != null)
            {
                existingTideObjects.Add(obj);
            }
        }
        
        if (existingTideObjects.Count == 0)
        {
            Debug.Log("No tide objects remaining!");
            return null;
        }
        
        int randomIndex = Random.Range(0, existingTideObjects.Count);
        return existingTideObjects[randomIndex];
    }
    
    private IEnumerator FlyToTarget(GameObject energyParticle, Vector3 targetPosition)
    {
        Vector3 startPosition = energyParticle.transform.position;
        
        // Calculate mid point for arc effect
        Vector3 midPosition = (startPosition + targetPosition) / 2;
        midPosition += Vector3.up * Vector3.Distance(startPosition, targetPosition) * 0.5f;
        
        float journeyTime = 1.0f;
        float elapsedTime = 0;
        
        while (elapsedTime < journeyTime)
        {
            float t = elapsedTime / journeyTime;
            
            // Bezier curve movement
            energyParticle.transform.position = CalculateBezierPoint(t, startPosition, midPosition, targetPosition);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Final effect upon reaching destination
        if (particleEffectPrefab != null && particleEffectPosition != null)
        {
            Instantiate(particleEffectPrefab, particleEffectPosition.position, Quaternion.identity);
        }
        
        Destroy(energyParticle);
    }
    
    // Quadratic Bezier Curve formula: B(t) = (1-t)²*P₀ + 2(1-t)t*P₁ + t²*P₂
    private Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        
        Vector3 point = uu * p0; 
        point += 2 * u * t * p1; 
        point += tt * p2; 
        
        return point;
    }
}