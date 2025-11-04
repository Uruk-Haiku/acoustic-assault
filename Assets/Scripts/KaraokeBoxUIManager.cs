using System.Collections.Generic;
using Lasp;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KaraokeBoxUIManager : MonoBehaviour
{
    /* A class that displays midi songs on a singing UI. Default is that the UI displays 12 beats at a time. */
    public ParticleSystem uiParticles;
    public GameObject QuarterNote;
    public GameObject FirstChild;
    public GameObject Cursor;
    public DamageCalculator damageCalculator;
    public TextMeshProUGUI lyricsText;

    private List<MidiNoteReader.NoteData> songNotes;
    public SimplePitchDetector pitchDetector;
    private RectTransform rectTransform;
    private RectTransform cursorRectTransform;
    private RectTransform lyricsRectTransform;

    private float barDuration;
    private float currSongTime = -2f; // Starts 2 seconds before song starts to allow player to prepare
    private float currSongLength = 0f;
    private float bpm;
    private bool isPlaying = false;

    private float UITopFrequency;
    private float UIBotFrequency;

    private List<GameObject> instantiatedNotes = new List<GameObject>();

    private Dictionary<int, float> midiToY = new Dictionary<int, float>();
    
    private Dictionary<int, Color> playerColors = new Dictionary<int, Color>
    {
        {0, Color.white},
        {1, Color.red},
        {2, Color.blue},
    };

    // Natural notes are spaced 1 step apart, sharps are halfway
    private readonly Dictionary<int, float> noteOffsets = new Dictionary<int, float>
    {
        { 0, 0f }, // C
        { 1, 0.5f }, // C#
        { 2, 1f }, // D
        { 3, 1.5f }, // D#
        { 4, 2f }, // E
        { 5, 3f }, // F 
        { 6, 3.5f }, // F#
        { 7, 4f }, // G
        { 8, 4.5f }, // G#
        { 9, 5f }, // A
        { 10, 5.5f }, // A#
        { 11, 6f } // B
    };

    private readonly HashSet<int> naturalSemitones = new HashSet<int> { 0, 2, 4, 5, 7, 9, 11 };

    private int GetNearestNaturalNote(int midiNote)
    {
        int semitone = midiNote % 12;
        if (naturalSemitones.Contains(semitone))
        {
            return midiNote;
        }
        else
        {
            return midiNote + 1;
        }
    }

    // Convert a MIDI note to "steps" relative to C0
    private float GetStepsFromC0(int midiNote)
    {
        int semitone = midiNote % 12;
        int octave = midiNote / 12;
        return octave * 7f + noteOffsets[semitone];
    }

    // Get Y position with the song’s middle note at y = 0
    private float GetNoteYPosition(int midiNote, int lowest, int highest, float unitSize)
    {
        int midNote = GetNearestNaturalNote((lowest + highest) / 2);
        float stepsFromC0 = GetStepsFromC0(midiNote);
        float stepsMid = GetStepsFromC0(midNote);

        // Offset so that the middle is 0
        return (stepsFromC0 - stepsMid) * unitSize;
    }

    // Build mapping for a given note range
    private Dictionary<int, float> BuildMidiToYMap(int lowest, int highest, float unitSize = 8.25f)
    {
        Dictionary<int, float> map = new Dictionary<int, float>();
        for (int midi = lowest; midi <= highest; midi++)
        {
            map[midi] = GetNoteYPosition(midi, lowest, highest, unitSize);
        }

        return map;
    }

    // Get the get natural note upwards
    private int NextNaturalUp(int midiNote)
    {
        int n = midiNote + 1;
        while (!naturalSemitones.Contains(n % 12)) n++;
        return n;
    }

    // Get the get natural note downwards
    private int NextNaturalDown(int midiNote)
    {
        int n = midiNote - 1;
        while (!naturalSemitones.Contains(n % 12)) n--;
        return n;
    }

    // Get the top and bottom natural notes of the UI boundaries
    private (int lowestNatural, int highestNatural) GetUiRange(int middleNatural)
    {
        int low = middleNatural;
        int high = middleNatural;

        for (int i = 0; i < 6; i++)
            low = NextNaturalDown(low);

        for (int i = 0; i < 6; i++)
            high = NextNaturalUp(high);

        return (low, high);
    }

    // Shift the octave of the song notes
    private List<MidiNoteReader.NoteData> ShiftOctaves(List<MidiNoteReader.NoteData> midiNotes, int octaveOffset)
    {
        List<MidiNoteReader.NoteData> shiftedNotes = new List<MidiNoteReader.NoteData>();
        foreach (var note in midiNotes)
        {
            int shiftedNoteNumber = Mathf.Max(0, note.note + octaveOffset * 12);

            shiftedNotes.Add(new MidiNoteReader.NoteData(
                shiftedNoteNumber,
                note.letter,
                note.start,
                note.end
            ));
        }

        return shiftedNotes;
    }


    void Start()
    {
        MidiNoteReader.MidiSong midiSong = MidiNoteReader.LoadMidiSongFromPath("IWantItThatWay/IWantItThatWay.mid");
        var (lowest1, highest1) = MidiNoteReader.GetNoteRange(midiSong.notes);
        Debug.Log(lowest1);
        Debug.Log(highest1);
        songNotes = ShiftOctaves(midiSong.notes, 0);
        var (lowest, highest) = MidiNoteReader.GetNoteRange(songNotes);
        midiToY = BuildMidiToYMap(lowest, highest);

        Debug.Log($"Loaded MIDI Song: {midiSong.name}, Length: {midiSong.length}s, BPM: {midiSong.bpm}, Notes: {midiSong.notes.Count}, Lowest: {lowest}, Highest: {highest}");

        var (lowestUINatural, highestUINatural) = GetUiRange(GetNearestNaturalNote((lowest + highest) / 2));

        Debug.Log($"Lowest UI Natural: {lowestUINatural},  Highest UI Natural: {highestUINatural}");

        UITopFrequency = 440f * Mathf.Pow(2f, (highestUINatural - 69) / 12f);
        UIBotFrequency = 440f * Mathf.Pow(2f, (lowestUINatural - 69) / 12f);

        Debug.Log($"UI Lowest Frequency: {UIBotFrequency}, UI Highest Frequency: {UITopFrequency}");

        rectTransform = GetComponent<RectTransform>();
        cursorRectTransform = Cursor.GetComponent<RectTransform>();
        lyricsRectTransform = lyricsText.GetComponent<RectTransform>();

        // GameManager uses 0 indexing for players
        // TODO ideally we should refactor to initialize karaoke manager (and other sub managers) in songManager
    }

    void Update()
    {
        if (isPlaying)
        {
            UpdateMusicUI();
            currSongTime += Time.deltaTime;
            UpdateCursorUI();
            UpdateDamage();
        }
    }

    void UpdateMusicUI()
    {
        // Clear previous notes
        foreach (var noteObj in instantiatedNotes)
        {
            Destroy(noteObj);
        }

        instantiatedNotes.Clear();

        List<MidiNoteReader.NoteData> currentNotes = MidiNoteReader.GetNotesInTimeRange(
            songNotes, currSongTime, currSongTime + barDuration);

        float tStartLyrics = (9f - currSongTime) / barDuration;
        lyricsRectTransform.anchoredPosition = new Vector2(-389.5f + tStartLyrics * 800f, lyricsRectTransform.anchoredPosition.y);

        foreach (var note in currentNotes)
        {
            float tStart = (note.start - currSongTime) / barDuration;

            // Find where note should spawn in UI pixel space
            float xStart = tStart * 800f;
            float yOffset = midiToY[note.note];

            GameObject noteObj = Instantiate(QuarterNote, rectTransform);
            int currentPlayer = SongManager.Instance.GetPlayerFromTime(note.start);
            noteObj.GetComponent<Image>().color = playerColors[currentPlayer];
            noteObj.transform.SetSiblingIndex(FirstChild.transform.GetSiblingIndex());

            // TODO in future pre-instantiate quarter notes for efficiency
            RectTransform noteRectTransform = noteObj.GetComponent<RectTransform>();

            noteRectTransform.anchoredPosition = new Vector2(noteRectTransform.anchoredPosition.x + xStart, yOffset);

            // Calculate note length
            float noteLengthFactor = 12f * (note.end - note.start) / barDuration;
            noteRectTransform.sizeDelta = new Vector2(noteRectTransform.rect.width * noteLengthFactor,
                noteRectTransform.sizeDelta.y);
            instantiatedNotes.Add(noteObj);
        }
    }

    void UpdateCursorUI()
    {
        float pitch = pitchDetector.shiftedPitch;

        // Clamp frequency to UI range
        float pitch_clamped = Mathf.Clamp(pitch, UIBotFrequency, UITopFrequency);

        // Logarithmic normalization (base 2 for octaves)
        float logPitch = Mathf.Log(pitch_clamped / UIBotFrequency, 2); // distance in octaves from bottom
        float logRange = Mathf.Log(UITopFrequency / UIBotFrequency, 2); // total range in octaves
        float pitch_normalized = logPitch / logRange;

        // Now linear interpolate in UI space
        float yPos = Mathf.Lerp(-214.4f, -116f, pitch_normalized);
        cursorRectTransform.anchoredPosition = new Vector2(cursorRectTransform.anchoredPosition.x, yPos);
    }

    void UpdateDamage()
    {
        MidiNoteReader.NoteData? note = MidiNoteReader.GetNoteAtTime(songNotes, currSongTime + 0.297f * barDuration);
        if (note != null)
        {
            float fTarget = 440f * Mathf.Pow(2f, (note.Value.note - 69f) / 12f);
            damageCalculator.SetTargetFrequency(fTarget);
            
            float fUpper = fTarget * Mathf.Pow(2f, 1f / 12f);    // +1 semitone
            float fLower = fTarget / Mathf.Pow(2f, 1f / 12f);    // -1 semitone
            float fInput = damageCalculator.pitchDetector.shiftedPitch;

            if (fInput >= fLower && fInput <= fUpper)
            {
                // Compute how close we are in semitone space (0 = perfect, 1 = edge)
                float diffSemitones = Mathf.Abs(12f * Mathf.Log(fInput / fTarget, 2f));
                float t = Mathf.Clamp01(diffSemitones / 1f); // 0 to 1 semitone range
                float rate = Mathf.Lerp(30f, 8f, t);
                
                Color finalColor = new Color(1f, 0.992f, 0.043f);
                
                var main = uiParticles.main;
                main.startColor = finalColor;
                
                var emission = uiParticles.emission;
                emission.rateOverTime = rate;

                uiParticles.Play();
            }
            else
            {
                uiParticles.Stop();
            }

        }
        else
        {
            damageCalculator.SetTargetFrequency(0f);
        }
    }


    public void StartPlaying(MidiNoteReader.MidiSong midiSong, float timeBeforeSongStarts)
    {
        bpm = midiSong.bpm;
        isPlaying = true;
        currSongLength = midiSong.length;
        barDuration = 60.0f / bpm * 12f;
        currSongTime = timeBeforeSongStarts - 0.297f * barDuration;
    }
}