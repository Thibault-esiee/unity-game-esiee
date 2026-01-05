using UnityEngine;

public class BackgroundMusic : MonoBehaviour
{
    
    private AudioSource audioSource;
    
    
    [SerializeField] private AudioClip musicClip;
    
    
    [SerializeField] [Range(0.0f, 1.0f)] private float volume = 0.5f;
    
    
    [SerializeField] private bool dontDestroyOnLoad = true;
    
    
    [SerializeField] private bool playOnAwake = true;
    
    
    private static BackgroundMusic instance = null;
    
    private void Awake()
    {
        
        if (dontDestroyOnLoad)
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }
        
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        audioSource.clip = musicClip;
        audioSource.volume = volume;
        audioSource.loop = true; 
        
        
        if (playOnAwake)
        {
            audioSource.Play();
        }
    }
    
    
    
    public void Play()
    {
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }
    
    public void Pause()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Pause();
        }
    }
    
    public void Stop()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }
    
    
    public void SetVolume(float newVolume)
    {
        if (audioSource != null)
        {
            audioSource.volume = Mathf.Clamp01(newVolume);
        }
    }
    
    
    public void ChangeMusic(AudioClip newClip)
    {
        if (audioSource != null)
        {
            bool wasPlaying = audioSource.isPlaying;
            audioSource.clip = newClip;
            
            if (wasPlaying)
            {
                audioSource.Play();
            }
        }
    }
}