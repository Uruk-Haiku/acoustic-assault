using UnityEngine;
using System.Collections.Generic;

public class GameManagerExample : MonoBehaviour
{
    private List<MidiNoteReader.NoteData> songNotes;
    
    void Start()
    {
        // Load the MIDI file
        songNotes = MidiNoteReader.GetNotesFromMidi("HotelCalifornia");
        
        // Print first 20 notes for debugging
        MidiNoteReader.PrintNotes(songNotes, 20);
        
        // Example: Get all notes in the first 10 seconds
        var firstNotes = MidiNoteReader.GetNotesInTimeRange(songNotes, 0f, 10f);
        Debug.Log($"Found {firstNotes.Count} notes in first 10 seconds");
    }
    
    void Update()
    {
        // // Example: Check what note is playing at current time
        // float currentTime = Time.time; // Or your audio source time
        // var currentNote = MidiNoteReader.GetNoteAtTime(songNotes, currentTime);
        //
        // if (currentNote.HasValue)
        // {
        //     Debug.Log($"Currently playing: {currentNote.Value.letter}");
        // }
    }
}