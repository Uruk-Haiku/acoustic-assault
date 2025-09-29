using UnityEngine;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class SimpleMidiPlayer : MonoBehaviour
{
    [Header("MIDI Settings")]
    [SerializeField] private string midiFileName = "your_song.mid";
    [SerializeField] private float volume = 0.5f;

    public DamageCalculator damageCalculator;

    private List<NoteEvent> noteEvents = new List<NoteEvent>();
    private Dictionary<int, AudioSource> activeNotes = new Dictionary<int, AudioSource>();

    [SerializeField] private bool playMusic = true;

    private class NoteEvent
    {
        public float time;
        public int noteNumber;
        public float velocity;
        public bool isNoteOn;
        
        public NoteEvent(float time, int noteNumber, float velocity, bool isNoteOn)
        {
            this.time = time;
            this.noteNumber = noteNumber;
            this.velocity = velocity;
            this.isNoteOn = isNoteOn;
        }
    }
    
    void Start()
    {
        // Just load and play the damn MIDI!
        LoadMidiFile(midiFileName);
        StartCoroutine(PlayMidi());
    }

    private void LoadMidiFile(string fileName)
    {
        try
        {
            // Read the MIDI file
            var midiFile = MidiFile.Read($"Assets/Music/Songs/{fileName}");
            var tempoMap = midiFile.GetTempoMap();
            var notes = midiFile.GetNotes();
            
            // Convert notes to events
            foreach (var note in notes)
            {
                float startTime = (float)note.TimeAs<MetricTimeSpan>(tempoMap).TotalSeconds;
                float endTime = startTime + (float)note.LengthAs<MetricTimeSpan>(tempoMap).TotalSeconds;
                float velocity = note.Velocity / 127f;
                
                noteEvents.Add(new NoteEvent(startTime, note.NoteNumber, velocity, true));
                noteEvents.Add(new NoteEvent(endTime, note.NoteNumber, velocity, false));
            }
            
            // Sort by time
            noteEvents = noteEvents.OrderBy(e => e.time).ToList();
            
            Debug.Log($"Loaded {fileName}: {notes.Count()} notes");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading MIDI file: {e.Message}");
        }
    }
    
    private IEnumerator PlayMidi()
    {
        float currentTime = 0f;
        int eventIndex = 0;
        
        while (eventIndex < noteEvents.Count)
        {
            // Process all events at current time
            while (eventIndex < noteEvents.Count && noteEvents[eventIndex].time <= currentTime)
            {
                var noteEvent = noteEvents[eventIndex];
                
                if (noteEvent.isNoteOn)
                {
                    PlayNote(noteEvent.noteNumber, noteEvent.velocity);
                }
                else
                {
                    StopNote(noteEvent.noteNumber);
                }
                
                eventIndex++;
            }
            
            yield return null;
            currentTime += Time.deltaTime;
        }
        
        Debug.Log("MIDI playback finished");
    }
    
    private void PlayNote(int midiNoteNumber, float velocity)
    {
        // Stop if already playing
        if (activeNotes.ContainsKey(midiNoteNumber))
        {
            StopNote(midiNoteNumber);
        }
        
        // Create audio source
        GameObject noteObject = new GameObject($"Note_{midiNoteNumber}");
        noteObject.transform.parent = transform;
        AudioSource audioSource = noteObject.AddComponent<AudioSource>();
        
        // Calculate frequency for this MIDI note
        float frequency = 440f * Mathf.Pow(2f, (midiNoteNumber - 69f) / 12f);
        
        // Setup and play (pitch should be 1, the frequency is in the generated tone)
        audioSource.pitch = 1f;
        audioSource.volume = volume * velocity;
        audioSource.loop = true;
        audioSource.clip = GenerateTone(frequency);
        if (playMusic)
            audioSource.Play();
        
        activeNotes[midiNoteNumber] = audioSource;
        SendFrequencyOfCurrentNoteNumber();
    }
    
    private void StopNote(int midiNoteNumber)
    {
        if (activeNotes.TryGetValue(midiNoteNumber, out AudioSource audioSource))
        {
            if (audioSource != null)
            {
                Destroy(audioSource.gameObject);
            }
            activeNotes.Remove(midiNoteNumber);
        }
        SendFrequencyOfCurrentNoteNumber();
    }
    
    private AudioClip GenerateTone(float frequency)
    {
        int sampleRate = 44100;
        int samples = sampleRate; // 1 second clip
        AudioClip clip = AudioClip.Create("Tone", samples, 1, sampleRate, false);
        
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            data[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate) * 0.5f;
        }
        
        clip.SetData(data, 0);
        return clip;
    }

    private void SendFrequencyOfCurrentNoteNumber()
    {
        // if no notes are active, set target frequency to 0 and does not give score
        if (activeNotes.Count == 0)
        {
            damageCalculator.SetTargetFrequency(0f);
            return;
        }
          
        int currentNoteNumber = activeNotes.Keys.FirstOrDefault();
        //Debug.Log($"Current Note Number: {currentNoteNumber}");
        damageCalculator.SetTargetFrequency(440f * Mathf.Pow(2f, (currentNoteNumber - 69f) / 12f));
    }
}