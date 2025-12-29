using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnergyBalanceManager : MonoBehaviour
{
    [Header("Targets")]
    public GameObject LeftFoot;
    public GameObject RightFoot;
    public GameObject UpTargetArea;

    [Header("Particle Effects")]
    public GameObject particleEffectPrefab; // Effect at destroyed child position
    public GameObject PointEffect;          // Global effect prefab
    public GameObject PointEffectPos;       // Position for global effect

    [Header("Chaos Object")]
    public GameObject Chaos; // Parent object containing destroyable children

    // Logic for alternating feet
    private enum NextFootTrigger { Right, Left, Any }
    private NextFootTrigger nextFootToTrigger = NextFootTrigger.Any; // Start with either

    // Cooldown management
    private bool effectTriggered = false;
    private float cooldownTimer = 0f;
    private const float CooldownDuration = 1f;

    private void Start()
    {
        if (LeftFoot == null || RightFoot == null || UpTargetArea == null || Chaos == null)
        {
            Debug.LogError("EnergyBalanceManager: Missing required objects in Inspector.");
            return;
        }
        
        SetupColliders(LeftFoot, "LeftFoot");
        SetupColliders(RightFoot, "RightFoot");
        
        Debug.Log("EnergyBalanceManager initialized. Ready for input.");
    }

    private void SetupColliders(GameObject foot, string footName)
    {
        if (foot == null) return;

        // Ensure Collider exists
        if (foot.GetComponent<Collider>() == null)
        {
            Debug.LogWarning($"{footName} missing Collider, adding BoxCollider.");
            foot.AddComponent<BoxCollider>();
        }
        
        // Ensure Trigger script exists
        FootTrigger trigger = foot.GetComponent<FootTrigger>();
        if (trigger == null)
        {
            trigger = foot.AddComponent<FootTrigger>();
            trigger.controller = this;
            trigger.isLeftFoot = (footName == "LeftFoot");
            Debug.Log($"Added FootTrigger to {footName}");
        }
    }

    private void Update()
    {
        // Handle Cooldown
        if (effectTriggered)
        {
            cooldownTimer += Time.deltaTime;
            if (cooldownTimer >= CooldownDuration)
            {
                effectTriggered = false;
                cooldownTimer = 0f;
                Debug.Log("Cooldown finished. Ready for next trigger.");
            }
        }
    }

    // Called by FootTrigger component
    public void OnFootEnterTarget(bool isLeftFoot)
    {
        if (effectTriggered) return;

        // Check if the correct foot is being used
        bool correctFoot = false;
        if (nextFootToTrigger == NextFootTrigger.Any) correctFoot = true;
        else if (isLeftFoot && nextFootToTrigger == NextFootTrigger.Left) correctFoot = true;
        else if (!isLeftFoot && nextFootToTrigger == NextFootTrigger.Right) correctFoot = true;

        string footName = isLeftFoot ? "Left Foot" : "Right Foot";

        if (correctFoot)
        {
            Debug.Log($"{footName} triggered successfully!");
            
            TriggerSuccessAction();
            
            // Alternate foot requirement
            nextFootToTrigger = isLeftFoot ? NextFootTrigger.Right : NextFootTrigger.Left;
            Debug.Log($"Next trigger must be: {nextFootToTrigger}");
        }
        else
        {
            Debug.Log($"Wrong foot! {footName} triggered, but expected {nextFootToTrigger}");
        }
    }

    private void TriggerSuccessAction()
    {
        effectTriggered = true;
        cooldownTimer = 0f;

        DestroyRandomChaosChild();
        
        ScoreManager.Instance?.AddScore(0.5f);
    }

    private void DestroyRandomChaosChild()
    {
        if (Chaos != null && Chaos.transform.childCount > 0)
        {
            int childCount = Chaos.transform.childCount;
            int randomIndex = Random.Range(0, childCount);
            Transform childToDestroy = Chaos.transform.GetChild(randomIndex);
            Vector3 childPosition = childToDestroy.position;
            
            Destroy(childToDestroy.gameObject);
            
            // Spawn particle at child location
            if (particleEffectPrefab != null)
            {
                Instantiate(particleEffectPrefab, childPosition, Quaternion.identity);
                StartCoroutine(PlaySoundAfterDelay("DestroyElectronic", 0.25f));
            }
            
            // Spawn particle at global position
            if (PointEffect != null && PointEffectPos != null)
            {
                Instantiate(PointEffect, PointEffectPos.transform.position, Quaternion.identity);
                
                string[] audioClips = { "MotionSuccess1", "MotionSuccess2", "MotionSuccess3", "MotionSuccess4", "MotionSuccess5" };
                string randomClip = audioClips[Random.Range(0, audioClips.Length)];
                AudioManager.Instance.Play(randomClip);
            }
            
            Debug.Log($"Destroyed Chaos child index: {randomIndex}. Remaining: {childCount - 1}");
        }
        else
        {
            Debug.LogWarning("Chaos object missing or has no children!");
        }
    }

    private IEnumerator PlaySoundAfterDelay(string soundName, float delay)
    {
        yield return new WaitForSeconds(delay);
        AudioManager.Instance.Play(soundName);
    }
}

// Helper component added to feet
public class FootTrigger : MonoBehaviour
{
    public EnergyBalanceManager controller;
    public bool isLeftFoot;

    private void OnTriggerEnter(Collider other)
    {
        // Only trigger if hitting the specific UpTargetArea defined in the controller
        if (controller != null && controller.UpTargetArea == other.gameObject)
        {
            controller.OnFootEnterTarget(isLeftFoot);
        }
    }
}