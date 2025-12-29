using UnityEngine;
using Unity.XR.PXR;

public class BulletCollision : MonoBehaviour
{
    [Header("Visual Effects")]
    public GameObject hitEffect; 
    public GameObject hitEffect2;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Monster"))
        {
            HandleHitEffects();
            AddScores();
            
            // Destroy monster and bullet
            Destroy(collision.gameObject);
            Destroy(this.gameObject);
        }
    }

    private void HandleHitEffects()
    {
        if (hitEffect != null)
            Instantiate(hitEffect, transform.position, Quaternion.identity);
            
        if (hitEffect2 != null)
            Instantiate(hitEffect2, transform.position, Quaternion.identity);
            
        AudioManager.Instance.Play("MonsterDead");
        
        // Haptic feedback
        PXR_Input.SendHapticImpulse(PXR_Input.VibrateType.RightController, 0.5f, 500, 100);
        PXR_Input.SendHapticImpulse(PXR_Input.VibrateType.LeftController, 0.5f, 500, 100);
    }

    private void AddScores()
    {
        ScoreManager.Instance?.AddScore(0.5f);
    }
}