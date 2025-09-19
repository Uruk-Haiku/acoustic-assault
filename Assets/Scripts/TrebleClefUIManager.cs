using System.Collections.Generic;
using UnityEngine;

public struct NoteData 
{
    public int note;        // MIDI note number (0-127)
    public string letter;   // Note letter (e.g., "C4", "F#5")
    public float start;     // Start time in seconds
    public float end;       // End time in seconds
        
    public NoteData(int note, string letter, float start, float end)
    {
        this.note = note;
        this.letter = letter;
        this.start = start;
        this.end = end;
    }
}

public class TrebleClefUIManager : MonoBehaviour
{
    private const float Bar_Duration = 5f;

    public GameObject QuarterNote;
    public GameObject Canvas;

    private List<MidiNoteReader.NoteData> songNotes;

    private float heightTop = 0f;
    private float heightBot = 0f;
    private float widthLeft = 0f;
    private float widthRight = 0f;

    private float currSongTime = -2f;
    private float currSongLength = 0f;
    private int bpm;

    private bool isPlaying = true;

    // Added storage for instantiated objects
    private List<GameObject> instantiatedNotes = new List<GameObject>();

    void Start()
    {
        MidiNoteReader.MidiSong midiSong = MidiNoteReader.LoadMidiSongFromPath("MaryHadALittleLamb.mid");
        Debug.Log($"Loaded MIDI Song: {midiSong.name}, Length: {midiSong.length}s, BPM: {midiSong.bpm}, Notes: {midiSong.notes.Count}");
        songNotes = midiSong.notes;
        RectTransform rt = GetComponent<RectTransform>();
        float width = rt.rect.width * rt.lossyScale.x;
        float height = rt.rect.height * rt.lossyScale.y;
        
        widthLeft = width / 4;
        widthRight = width / 3;
        heightTop = 2 * height / 11f;
        heightBot = 2 * height / 11f;
    }

    void Update()
    {
        if (isPlaying)
        {
            updateUI(currSongTime);
            currSongTime += Time.deltaTime;
        }
    }

    void updateUI(float currSongTime)
    {
        // Clear previous notes
        foreach (var noteObj in instantiatedNotes)
        {
            Destroy(noteObj);
        }
        instantiatedNotes.Clear();
        
        

        // Hardcoded notes
        List<MidiNoteReader.NoteData> currentNotes = MidiNoteReader.GetNotesInTimeRange(songNotes, currSongTime, currSongTime + Bar_Duration);

        foreach (var note in currentNotes)
        {
            float tStart = (note.start - currSongTime) / Bar_Duration;
            float tEnd   = (note.end   - currSongTime) / Bar_Duration;
            
            float xStart = transform.position.x - widthLeft + (widthLeft + widthRight) * tStart;
            
            int midiBottom = 54; // E4
            int midiTop = 67;    // F5

            float yBottom = transform.position.y - heightBot;
            float yTop = transform.position.y + heightTop;
            
            float realMidiNumber = Mathf.Clamp(note.note, midiBottom, midiTop);
            
            float t = (float)(realMidiNumber - midiBottom) / (midiTop - midiBottom);
            float yPos = Mathf.Lerp(yBottom, yTop, t);
            
            Vector3 spawnPos = new Vector3(xStart, yPos, transform.position.z);
            
            GameObject noteObj = Instantiate(QuarterNote, spawnPos, Quaternion.identity, Canvas.transform);

            float noteLength = Mathf.Abs(note.end - note.start);
            Vector3 newScale = noteObj.transform.localScale;
            newScale.x = 0.5f * noteLength;
            noteObj.transform.localScale = newScale;
            
            instantiatedNotes.Add(noteObj);
        }
    }

    public void StartPlaying(float songLength, int bpm)
    {
        isPlaying = true;
        bpm = bpm;
        currSongLength = songLength;
    }
}
