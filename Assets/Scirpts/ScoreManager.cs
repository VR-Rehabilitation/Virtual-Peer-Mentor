using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using Unity.XR.PICO; 
using Unity.XR.PXR;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI encouragingText;

    [Header("VPM Configuration")]
    [SerializeField] private Animator vpmAnimator;
    [SerializeField] private float feedbackCooldown = 3.0f; 
    [SerializeField] private float hapticDuration = 0.5f;
    [SerializeField] [Range(0, 1)] private float hapticStrength = 0.6f;

    [Header("Audio Configuration")]
    [SerializeField] private List<AudioClip> encouragementAudioClips;

    [Header("Game Settings")]
    public string nextStage;
    [SerializeField] private float maxScore = 25f;

    // State Variables
    private float currentScore = 0;
    private bool isGamePaused = false;
    private int currentLevel = 1;
    private float lastFeedbackTime;

    // Phrase Library
    private readonly Dictionary<int, List<string>> phraseLibrary = new Dictionary<int, List<string>>();
    private Dictionary<int, List<int>> availableIndices = new Dictionary<int, List<int>>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializePhraseLibrary();
        InitializeRandomization();
    }

    private void InitializePhraseLibrary()
    {
        // Level 1: Early (0-20%)
        phraseLibrary.Add(1, new List<string> {
            "Good start!", "Keep it up!", "Nice move!", "Focusing well!", "Great beginning!",
            "Steady pace!", "Doing well!", "Nice rhythm!", "Stay calm!", "Good job!"
        });

        // Level 2: Progressing (20-40%)
        phraseLibrary.Add(2, new List<string> {
            "Getting stronger!", "Looking good!", "Nice form!", "Keep going!", "You got this!",
            "Well done!", "Moving well!", "Great effort!", "Stay strong!", "Solid work!"
        });

        // Level 3: Midway (40-60%)
        phraseLibrary.Add(3, new List<string> {
            "Halfway there!", "Impressive!", "Great energy!", "Excellent!", "Keep pushing!",
            "Wonderful!", "Doing great!", "Fantastic!", "Stay focused!", "You are doing it!"
        });

        // Level 4: Advanced (60-80%)
        phraseLibrary.Add(4, new List<string> {
            "Almost there!", "Don't stop!", "Full power!", "Keep relaxed!", "Amazing job!",
            "So close!", "Keep fighting!", "Brilliant!", "Outstanding!", "Finish strong!"
        });

        // Level 5: Completion (80-100%)
        phraseLibrary.Add(5, new List<string> {
            "Victory is yours!", "You are a legend!!", "Perfect!", "You almost did it!", "Excellent work!",
            "Almost success!", "Close to finish!", "Incredible work!", "You are Champion!", "Mission almost accomplished!"
        });
    }

    private void InitializeRandomization()
    {
        for (int i = 1; i <= 5; i++)
        {
            ResetAvailableIndices(i);
        }
    }

    private void ResetAvailableIndices(int level)
    {
        if (!availableIndices.ContainsKey(level))
            availableIndices[level] = new List<int>();
        
        availableIndices[level].Clear();
        for (int i = 0; i < 10; i++)
        {
            availableIndices[level].Add(i);
        }
    }

    public void AddScore(float points)
    {
        currentScore += points;
        
        UpdateUI();
        CheckLevelAndTriggerFeedback();
        CheckGameCompletion();
    }

    private void UpdateUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"{currentScore}/{maxScore}";
        }
    }

    private void CheckLevelAndTriggerFeedback()
    {
        float progress = Mathf.Clamp01(currentScore / maxScore);
        int newLevel = 1;
        
        if (progress >= 0.8f) newLevel = 5;
        else if (progress >= 0.6f) newLevel = 4;
        else if (progress >= 0.4f) newLevel = 3;
        else if (progress >= 0.2f) newLevel = 2;

        currentLevel = newLevel;

        if (Time.time - lastFeedbackTime >= feedbackCooldown)
        {
            TriggerVPMFeedback(currentLevel);
            lastFeedbackTime = Time.time;
        }
    }

    private void TriggerVPMFeedback(int level)
    {
        string phrase = GetRandomPhrase(level, out int phraseIndex);

        if (encouragingText != null)
        {
            encouragingText.text = phrase;
        }

        PlayAudioForPhrase(level, phraseIndex);
        PlayRandomGesture();
        TriggerHaptics();
    }

    private string GetRandomPhrase(int level, out int originalIndex)
    {
        List<int> available = availableIndices[level];
        
        if (available.Count == 0)
        {
            ResetAvailableIndices(level);
        }

        int randIndex = Random.Range(0, available.Count);
        originalIndex = available[randIndex];
        
        available.RemoveAt(randIndex); 

        return phraseLibrary[level][originalIndex];
    }

    private void PlayAudioForPhrase(int level, int indexInLevel)
    {
        if (encouragementAudioClips == null || encouragementAudioClips.Count == 0) return;

        // Calculate index assuming 10 clips per level
        int globalIndex = (level - 1) * 10 + indexInLevel;

        if (globalIndex >= 0 && globalIndex < encouragementAudioClips.Count)
        {
            AudioClip clip = encouragementAudioClips[globalIndex];
            if (clip != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayOneShot(clip);
            }
        }
        else
        {
            Debug.LogWarning($"Missing Audio Clip for Level {level}, Index {indexInLevel} (Global: {globalIndex})");
        }
    }

    private void PlayRandomGesture()
    {
        if (vpmAnimator == null) return;
        
        int[] validGestures = { 1, 3, 4 };
        int selection = validGestures[Random.Range(0, validGestures.Length)];

        vpmAnimator.SetTrigger($"Score{selection}");
    }

    private void TriggerHaptics()
    {
        try
        {
            const int hapticFrequency = 150;
            PXR_Input.SendHapticImpulse(PXR_Input.VibrateType.LeftController, hapticStrength, (int)(hapticDuration * 500), hapticFrequency);
            PXR_Input.SendHapticImpulse(PXR_Input.VibrateType.RightController, hapticStrength, (int)(hapticDuration * 500), hapticFrequency);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Haptics failed: {e.Message}");
        }
    }

    private void CheckGameCompletion()
    {
        if (currentScore >= maxScore && !isGamePaused)
        {
            isGamePaused = true;
            Time.timeScale = 0f;
            StartCoroutine(LoadNextSceneAfterDelay());
        }
    }

    private IEnumerator LoadNextSceneAfterDelay()
    {
        yield return new WaitForSecondsRealtime(3f);
        Time.timeScale = 1f;
        if (!string.IsNullOrEmpty(nextStage))
        {
            SceneManager.LoadScene(nextStage);
        }
        else
        {
            Debug.LogWarning("Next stage name is empty in ScoreManager!");
        }
    }
}