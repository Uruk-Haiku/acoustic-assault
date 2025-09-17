using UnityEngine;
using System.Collections;

public class SimpleBeatPlayer : MonoBehaviour
{
    [Header("Audio Clips")]
    [Tooltip("Drag your kick drum sound here")]
    public AudioClip kickSound;
    
    [Tooltip("Drag your snare drum sound here")]
    public AudioClip snareSound;
    
    [Tooltip("Drag your hi-hat sound here")]
    public AudioClip hihatSound;
    
    [Header("Beat Settings")]
    [Range(60, 200)]
    [Tooltip("Beats per minute")]
    public float bpm = 120f;
    
    [Header("Pattern (Check to play on that beat)")]
    [Tooltip("Kick pattern - which of the 16 beats to play")]
    public bool[] kickPattern = new bool[16] { true, false, false, false, true, false, false, false, true, false, false, false, true, false, false, false };
    
    [Tooltip("Snare pattern - which of the 16 beats to play")]
    public bool[] snarePattern = new bool[16] { false, false, false, false, true, false, false, false, false, false, false, false, true, false, false, false };
    
    [Tooltip("Hi-hat pattern - which of the 16 beats to play")]
    public bool[] hihatPattern = new bool[16] { true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true };
    
    [Header("Controls")]
    public bool playOnStart = true;
    public bool isPlaying = false;
    
    // Private variables
    private AudioSource audioSource;
    private float beatInterval;
    private int currentBeat = 0;
    private Coroutine playCoroutine;
    
    void Start()
    {
        // Get or add AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Start playing if set to play on start
        if (playOnStart)
        {
            Play();
        }
    }
    
    // Public method to start playing
    public void Play()
    {
        if (!isPlaying)
        {
            isPlaying = true;
            currentBeat = 0;
            CalculateBeatInterval();
            playCoroutine = StartCoroutine(PlayBeat());
        }
    }
    
    // Public method to stop playing
    public void Stop()
    {
        if (isPlaying)
        {
            isPlaying = false;
            if (playCoroutine != null)
            {
                StopCoroutine(playCoroutine);
                playCoroutine = null;
            }
            currentBeat = 0;
        }
    }
    
    // Public method to set BPM
    public void SetBPM(float newBPM)
    {
        bpm = Mathf.Clamp(newBPM, 60f, 200f);
        CalculateBeatInterval();
    }
    
    // Calculate the interval between beats
    private void CalculateBeatInterval()
    {
        // Convert BPM to seconds per beat
        // Dividing by 4 because we're using 16th notes (4 per beat)
        beatInterval = 60f / (bpm * 4f);
    }
    
    // Coroutine that plays the beat
    private IEnumerator PlayBeat()
    {
        while (isPlaying)
        {
            // Play sounds for current beat
            if (currentBeat < 16)
            {
                // Check and play kick
                if (kickPattern[currentBeat] && kickSound != null)
                {
                    audioSource.PlayOneShot(kickSound);
                }
                
                // Check and play snare
                if (snarePattern[currentBeat] && snareSound != null)
                {
                    audioSource.PlayOneShot(snareSound);
                }
                
                // Check and play hi-hat
                if (hihatPattern[currentBeat] && hihatSound != null)
                {
                    audioSource.PlayOneShot(hihatSound, 0.7f); // Hi-hat slightly quieter
                }
            }
            
            // Move to next beat
            currentBeat++;
            if (currentBeat >= 16)
            {
                currentBeat = 0; // Loop back to start
            }
            
            // Wait for next beat
            yield return new WaitForSeconds(beatInterval);
        }
    }
    
    // Optional: Add keyboard controls
    void Update()
    {
        // Press Space to toggle play/stop
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isPlaying)
                Stop();
            else
                Play();
        }
        
        // Press R to reset to first beat
        if (Input.GetKeyDown(KeyCode.R))
        {
            currentBeat = 0;
        }
        
        // Press Up/Down arrows to adjust BPM
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            SetBPM(bpm + 5);
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            SetBPM(bpm - 5);
        }
    }
    
    // Draw some debug info in the editor
    void OnGUI()
    {
        if (isPlaying)
        {
            GUI.Label(new Rect(10, 10, 200, 20), $"BPM: {bpm}");
            GUI.Label(new Rect(10, 30, 200, 20), $"Beat: {currentBeat + 1}/16");
            GUI.Label(new Rect(10, 50, 200, 20), "Press SPACE to stop");
        }
        else
        {
            GUI.Label(new Rect(10, 10, 200, 20), "Press SPACE to play");
        }
    }
}