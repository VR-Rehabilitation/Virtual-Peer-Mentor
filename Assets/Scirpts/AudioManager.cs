using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    // Singleton Instance
    public static AudioManager Instance { get; private set; }

    [System.Serializable]
    public class Sound
    {
        public string name;
        public AudioClip clip;
        
        [Range(0f, 1f)]
        public float volume = 1f;
        
        [Range(0.1f, 3f)]
        public float pitch = 1f;
        
        public bool loop = false;
        
        [HideInInspector]
        public AudioSource source;
        
        public enum SoundType
        {
            SFX,    // Sound Effects
            Music,  // Background Music
            UI,     // UI Sounds
            Ambient // Ambient Sounds
        }
        
        public SoundType soundType = SoundType.SFX;
    }

    // Registered sounds list (Editable in Inspector)
    public List<Sound> sounds = new List<Sound>();
    
    // Fast lookup dictionary
    private Dictionary<string, Sound> soundDictionary = new Dictionary<string, Sound>();
    
    // Audio Mixer Reference
    public AudioMixer audioMixer;
    
    [Header("Volume Settings")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 1f;
    [Range(0f, 1f)] public float uiVolume = 1f;
    [Range(0f, 1f)] public float ambientVolume = 1f;
    
    private Sound currentMusic;
    
    // Audio source object pool
    private List<AudioSource> audioSourcePool = new List<AudioSource>();
    private const int PoolSize = 10;

    private void Awake()
    {
        // Singleton implementation
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        InitializeAudioSources();
        ApplyVolumeSettings();
    }
    
    private void InitializeAudioSources()
    {
        soundDictionary.Clear();

        // Initialize each sound defined in the inspector
        foreach (Sound s in sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;
            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
            
            // Assign mixer groups if mixer exists
            if (audioMixer != null)
            {
                string groupName = s.soundType.ToString();
                var groups = audioMixer.FindMatchingGroups(groupName);
                
                if (groups.Length > 0)
                {
                    s.source.outputAudioMixerGroup = groups[0];
                }
            }

            // Add to dictionary for fast access
            if (!soundDictionary.ContainsKey(s.name))
            {
                soundDictionary.Add(s.name, s);
            }
        }
        
        // Initialize Object Pool
        for (int i = 0; i < PoolSize; i++)
        {
            CreatePoolableAudioSource();
        }
    }

    private AudioSource CreatePoolableAudioSource()
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        audioSourcePool.Add(source);
        return source;
    }
    
    public void ApplyVolumeSettings()
    {
        if (audioMixer == null) return;

        SetMixerVolume("MasterVolume", masterVolume);
        SetMixerVolume("SFXVolume", sfxVolume);
        SetMixerVolume("MusicVolume", musicVolume);
        SetMixerVolume("UIVolume", uiVolume);
        SetMixerVolume("AmbientVolume", ambientVolume);
    }

    private void SetMixerVolume(string paramName, float volume)
    {
        // Log10 conversion for decibels
        float dbVolume = Mathf.Log10(Mathf.Max(0.0001f, volume)) * 20; 
        audioMixer.SetFloat(paramName, dbVolume);
    }
    
    public void Play(string name)
    {
        if (soundDictionary.TryGetValue(name, out Sound s))
        {
            s.source.Play();
        }
        else
        {
            Debug.LogWarning($"AudioManager: Sound '{name}' not found!");
        }
    }

    public void Stop(string name)
    {
        if (soundDictionary.TryGetValue(name, out Sound s))
        {
            s.source.Stop();
        }
    }

    public void PlayOneShot(AudioClip clip)
    {
        if (clip == null) return;
        AudioSource source = GetAvailableSource();
        source.PlayOneShot(clip);
        StartCoroutine(ReturnToPoolWhenFinished(source, clip.length));
    }

    // Play music with crossfade
    public void PlayMusic(string name, float fadeTime = 1f)
    {
        if (!soundDictionary.TryGetValue(name, out Sound s)) return;

        // Skip if this specific music is already playing
        if (currentMusic != null && currentMusic.name == name && currentMusic.source.isPlaying)
            return;

        StartCoroutine(FadeMusicRoutine(s, fadeTime));
    }

    private IEnumerator FadeMusicRoutine(Sound newMusic, float fadeTime)
    {
        // Fade out current music
        if (currentMusic != null && currentMusic.source.isPlaying)
        {
            float startVol = currentMusic.source.volume;
            for (float t = 0; t < fadeTime; t += Time.deltaTime)
            {
                currentMusic.source.volume = Mathf.Lerp(startVol, 0, t / fadeTime);
                yield return null;
            }
            currentMusic.source.Stop();
        }

        // Fade in new music
        currentMusic = newMusic;
        currentMusic.source.Play();
        float targetVol = newMusic.volume;
        
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            currentMusic.source.volume = Mathf.Lerp(0, targetVol, t / fadeTime);
            yield return null;
        }
        currentMusic.source.volume = targetVol; // Ensure final volume is exact
    }

    // Play 3D sound at position using the pool
    public void PlayAtPosition(string name, Vector3 position)
    {
        if (!soundDictionary.TryGetValue(name, out Sound s)) return;

        AudioSource source = GetAvailableSource();
        source.transform.position = position;
        source.spatialBlend = 1f; // Set to 3D
        source.clip = s.clip;
        source.volume = s.volume;
        source.pitch = s.pitch;
        source.Play();
        
        StartCoroutine(ReturnToPoolWhenFinished(source, s.clip.length));
    }

    private AudioSource GetAvailableSource()
    {
        foreach (AudioSource source in audioSourcePool)
        {
            if (!source.isPlaying)
            {
                source.spatialBlend = 0f; // Reset to 2D default
                source.transform.position = Vector3.zero;
                return source;
            }
        }
        
        // Pool is full, expand it
        Debug.LogWarning("AudioManager: Pool full. Creating new source.");
        return CreatePoolableAudioSource();
    }
    
    private IEnumerator ReturnToPoolWhenFinished(AudioSource source, float delay)
    {
        yield return new WaitForSeconds(delay);
        source.Stop();
        source.clip = null;
    }
    
    // Setters for UI Sliders
    public void SetMasterVolume(float volume) { masterVolume = volume; ApplyVolumeSettings(); }
    public void SetSFXVolume(float volume) { sfxVolume = volume; ApplyVolumeSettings(); }
    public void SetMusicVolume(float volume) { musicVolume = volume; ApplyVolumeSettings(); }
    public void SetUIVolume(float volume) { uiVolume = volume; ApplyVolumeSettings(); }
}