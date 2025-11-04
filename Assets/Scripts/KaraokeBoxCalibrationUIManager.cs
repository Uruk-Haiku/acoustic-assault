using System.Collections.Generic;
using Lasp;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class KaraokeBoxCalibrationUIManager : MonoBehaviour
{
    /* A class that displays midi songs on a singing UI. Default is that the UI displays 12 beats at a time. */

    public bool isPlaying = true;
    public float currSongTime;
    public GameObject FirstChild;
    public GameObject Cursor;

    private PitchDetector pitchDetector;
    private RectTransform cursorRectTransform;

    private float UITopFrequency;
    private float UIBotFrequency;

    private List<MidiNoteReader.NoteData> songNotes;

    private int playerID;

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
    private Dictionary<int, float> BuildMidiToYMap(int lowest, int highest, float unitSize = 8.25f)
    {
        Dictionary<int, float> map = new Dictionary<int, float>();
        for (int midi = lowest; midi <= highest; midi++)
        {
            map[midi] = GetNoteYPosition(midi, lowest, highest, unitSize);
        }

        return map;
    }

    private void OnEnable()
    {
        MidiNoteReader.MidiSong midiSong = MidiNoteReader.LoadMidiSongFromPath("IWantItThatWay/IWantItThatWay.mid");
        songNotes = ShiftOctaves(midiSong.notes, 0);
        var (lowest, highest) = MidiNoteReader.GetNoteRange(songNotes);
        midiToY = BuildMidiToYMap(lowest, highest);

        SettingsPanel settingsPanel = GetComponentInParent<SettingsPanel>();
        playerID = settingsPanel.currentPlayer;
        pitchDetector = GameManager.GetPitchDetection(playerID);

        var (lowestUINatural, highestUINatural) = GetUiRange(GetNearestNaturalNote((lowest + highest) / 2));

        Debug.Log($"Lowest UI Natural: {lowestUINatural},  Highest UI Natural: {highestUINatural}");

        UITopFrequency = 440f * Mathf.Pow(2f, (highestUINatural - 69) / 12f);
        UIBotFrequency = 440f * Mathf.Pow(2f, (lowestUINatural - 69) / 12f);

        Debug.Log($"UI Lowest Frequency: {UIBotFrequency}, UI Highest Frequency: {UITopFrequency}");

        cursorRectTransform = Cursor.GetComponent<RectTransform>();
    }

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

    void Update()
    {
        if (isPlaying)
        {
            UpdateCursorUI();
        }
    }

    void UpdateCursorUI()
    {
        float pitch = pitchDetector.offsetDisplayPitch;

        // Clamp frequency to UI range
        // float pitch_clamped = Mathf.Clamp(pitch, UIBotFrequency, UITopFrequency);

        // Logarithmic normalization (base 2 for octaves)
        float logPitch = Mathf.Log(pitch / UIBotFrequency, 2); // distance in octaves from bottom
        float logRange = Mathf.Log(UITopFrequency / UIBotFrequency, 2); // total range in octaves
        float pitch_normalized = logPitch / logRange;

        // Now linear interpolate in UI space
        // float yPos = Mathf.Lerp(-214.4f, -116f, pitch_normalized) + 214.4f;
        // cursorRectTransform.anchoredPosition = new Vector2(cursorRectTransform.anchoredPosition.x, yPos);
        cursorRectTransform.pivot = new Vector2(0.5f, pitch_normalized);
    }
}