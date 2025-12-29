using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.XR.PXR;

public class ElementEliminationManager : MonoBehaviour
{
    [Header("Targets")]
    public GameObject ShoulderLeftHand;
    public GameObject ShoulderRightHand;
    public GameObject TargetAreaLeft;
    public GameObject TargetAreaRight;

    [Header("Particle Effects")]
    public GameObject controllerParticleEffect; // Particle prefab for controller collision
    public GameObject robotParticleEffect;      // Particle prefab for robot collision
    public GameObject controllerParticlePos;    // PointParticlePosition
    public GameObject robotParticlePos;         // RobotParticlePosition

    [Header("Blood Cell")]
    public GameObject BloodCell; 

    // State tracking
    private bool leftHandTouchingTarget = false;
    private bool rightHandTouchingTarget = false;
    private bool effectTriggered = false;
    
    // Cooldown settings
    private float cooldownTimer = 0f;
    private const float CooldownDuration = 1.5f; 

    private void Update()
    {
        // Handle cooldown
        if (effectTriggered)
        {
            cooldownTimer += Time.deltaTime;
            if (cooldownTimer >= CooldownDuration)
            {
                effectTriggered = false;
                cooldownTimer = 0f;
            }
        }

        // Check Trigger Condition
        if (leftHandTouchingTarget && rightHandTouchingTarget && !effectTriggered)
        {
            ExecuteCorrectMotion();
        }
    }

    private void ExecuteCorrectMotion()
    {
        ScoreManager.Instance?.AddScore(1);

        TriggerMotionEffects();
    }

    private void TriggerMotionEffects()
    {
        // Set flag to prevent double trigger
        effectTriggered = true;

        DestroyRandomBloodCellChild();
        PlaySuccessEffects();
    }

    private void DestroyRandomBloodCellChild()
    {
        if (BloodCell != null && BloodCell.transform.childCount > 0)
        {
            int childCount = BloodCell.transform.childCount;
            int randomIndex = Random.Range(0, childCount);
            Transform childToDestroy = BloodCell.transform.GetChild(randomIndex);
            
            Destroy(childToDestroy.gameObject);
            Debug.Log($"Destroyed BloodCell child at index: {randomIndex}");
        }
        else
        {
            Debug.LogWarning("BloodCell is missing or has no children!");
        }
    }

    // Input Handlers
    public void OnLeftHandEnterTarget()
    {
        leftHandTouchingTarget = true;
        // Logic handled in Update for synchronization
    }

    public void OnLeftHandExitTarget()
    {
        leftHandTouchingTarget = false;
    }

    public void OnRightHandEnterTarget()
    {
        rightHandTouchingTarget = true;
    }

    public void OnRightHandExitTarget()
    {
        rightHandTouchingTarget = false;
    }

    private void PlaySuccessEffects()
    {
        if (controllerParticleEffect != null && controllerParticlePos != null)
        {
            Instantiate(controllerParticleEffect, controllerParticlePos.transform.position, Quaternion.identity);
        }

        if (robotParticleEffect != null && robotParticlePos != null)
        {
            Instantiate(robotParticleEffect, robotParticlePos.transform.position, Quaternion.identity);
        }

        // Play random success sound
        string[] audioClips = { "MotionSuccess1", "MotionSuccess2", "MotionSuccess3", "MotionSuccess4", "MotionSuccess5" };
        string randomClip = audioClips[Random.Range(0, audioClips.Length)];
        AudioManager.Instance.Play(randomClip);
        
        StartCoroutine(PlaySoundAfterDelay("Disappear", 0.25f));
        
        // Haptic Feedback
        PXR_Input.SendHapticImpulse(PXR_Input.VibrateType.RightController, 0.5f, 500, 100);
        PXR_Input.SendHapticImpulse(PXR_Input.VibrateType.LeftController, 0.5f, 500, 100);
    }
    
    private IEnumerator PlaySoundAfterDelay(string soundName, float delay)
    {
        yield return new WaitForSeconds(delay);
        AudioManager.Instance.Play(soundName);
    }
}