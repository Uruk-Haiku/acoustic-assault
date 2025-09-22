using System.Collections.Generic;
using Lasp;
using UnityEngine;

namespace lasp
{
    public class KaraokeBoxUIManager : MonoBehaviour
    {
        /* A class that displays midi songs on a singing UI. Default is that the UI displays 12 beats at a time. */
        public GameObject QuarterNote;
        public GameObject FirstChild;
        public GameObject PitchDetection;
        public GameObject Cursor;

        private List<MidiNoteReader.NoteData> songNotes;
        private SimplePitchDetector pitchDetector;
        private RectTransform rectTransform;
        private RectTransform cursorRectTransform;

        private float barDuration;
        private float currSongTime = -2f; // Starts 2 seconds before song starts to allow player to prepare
        private float currSongLength = 0f;
        private float bpm;
        private bool isPlaying = false;
        
        private List<GameObject> instantiatedNotes = new List<GameObject>();
        
        // Hardcoded values for Y offset of midi note values
        // 52 Lowest for now, 69 highest. Middle is 62 (should change it to 60 eventually)
        Dictionary<int, float> midiToY = new Dictionary<int, float>()
        {
            {52, -49.5f}, {53, -41.25f}, {54, -37.125f}, {55, -33f},
            {56, -28.875f}, {57, -24.75f}, {58, -20.625f}, {59, -16.5f},
            {60, -8.25f}, {61, -4.125f}, {62, 0f}, {63, 4.125f},
            {64, 8.25f}, {65, 16.5f}, {66, 20.625f}, {67, 24.75f},
            {68, 28.875f}, {69, 33f}
        };
        
        void Start()
        {
            MidiNoteReader.MidiSong midiSong = MidiNoteReader.LoadMidiSongFromPath("MaryHadALittleLamb.mid");
            Debug.Log($"Loaded MIDI Song: {midiSong.name}, Length: {midiSong.length}s, BPM: {midiSong.bpm}, Notes: {midiSong.notes.Count}");
            songNotes = midiSong.notes;
            // Hardcoded to always start playing for now
		    StartPlaying(midiSong.length, midiSong.bpm);
            rectTransform = GetComponent<RectTransform>();
            cursorRectTransform = Cursor.GetComponent<RectTransform>();
            pitchDetector = PitchDetection.GetComponent<SimplePitchDetector>();
        }

        void Update()
        {
            if (isPlaying)
            {
                UpdateMusicUI();
                currSongTime += Time.deltaTime;
                UpdateCursorUI();
            }
        }

        void UpdateMusicUI()
        {
            // Clear previous notes
            foreach (var noteObj in instantiatedNotes) {
                Destroy(noteObj);
            }
            instantiatedNotes.Clear();
        
            List<MidiNoteReader.NoteData> currentNotes = MidiNoteReader.GetNotesInTimeRange(
                songNotes, currSongTime, currSongTime + barDuration);

            foreach (var note in currentNotes) {
                float tStart = (note.start - currSongTime) / barDuration;
                
                // Find where note should spawn in UI pixel space
                float xStart = tStart * 800f;  
                float yOffset = midiToY[note.note];
                
                GameObject noteObj = Instantiate(QuarterNote, rectTransform);
                noteObj.transform.SetSiblingIndex(FirstChild.transform.GetSiblingIndex());
                RectTransform noteRectTransform = noteObj.GetComponent<RectTransform>();
                
                noteRectTransform.anchoredPosition = new Vector2(noteRectTransform.anchoredPosition.x + xStart, yOffset);
                
                // Calculate note length
                float noteLengthFactor = 12f * (note.end - note.start) / barDuration;
                noteRectTransform.sizeDelta = new Vector2(noteRectTransform.rect.width * noteLengthFactor, noteRectTransform.sizeDelta.y);
                instantiatedNotes.Add(noteObj);
            }
        }

        void UpdateCursorUI()
        {
            float pitch = pitchDetector.pitch;
            float pitch_clamped = Mathf.Clamp(pitch, 165f, 440f);
            float pitch_normalized = (pitch_clamped - 165f) / (440f - 165f);
            float yPos = Mathf.Lerp(-214.4f, -116f, pitch_normalized);
            cursorRectTransform.anchoredPosition = new Vector2(cursorRectTransform.anchoredPosition.x, yPos);
            
        }


        public void StartPlaying(float songLength, float beatsPerMinute)
        {
            bpm = beatsPerMinute;
            isPlaying = true;
            currSongLength = songLength;
            barDuration = 60.0f / bpm * 12f;
        }
    }

}
