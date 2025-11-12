using UnityEngine;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System.Collections.Generic;
using System.Linq;
using System.IO;

public static class MidiNoteReader
{
    public struct MidiSong{
        public string name;
        public float length;
        public float bpm;
        public List<NoteData> notes;
    }

    [System.Serializable]
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

    /// <summary>
    /// Static method to read a MIDI file and return a complete MidiSong with all metadata
    /// GameManager can call: var song = MidiNoteReader.LoadMidiSongFromPath("song.mid");
    /// </summary>
    /// <param name="fileName">Name of the MIDI file (without path)</param>
    /// <returns>MidiSong containing notes, length, BPM, and name</returns>
    public static MidiSong LoadMidiSongFromPath(string song)
    {
        List<NoteData> noteDataList = new List<NoteData>();
        float length = 0f;
        float bpm = 120f; // Default BPM
        
        try
        {
            
            // Read the MIDI file from Assets/MidiFiles/
            string path = Path.Combine(Application.streamingAssetsPath, "Songs", song, $"{song}.mid"); ;
            var midiFile = MidiFile.Read(path);
            
            // Get tempo map for accurate time conversion
            var tempoMap = midiFile.GetTempoMap();
            
            // Get song metadata
            length = (float)midiFile.GetDuration<MetricTimeSpan>().TotalSeconds;
            bpm = (float)tempoMap.GetTempoAtTime(new MidiTimeSpan(0)).BeatsPerMinute;
            
            // Extract all notes from all tracks
            var notes = midiFile.GetNotes();
            
            foreach (var note in notes)
            {
                // Convert MIDI time to seconds
                float startTime = (float)note.TimeAs<MetricTimeSpan>(tempoMap).TotalSeconds;
                float duration = (float)note.LengthAs<MetricTimeSpan>(tempoMap).TotalSeconds;
                float endTime = startTime + duration;
                
                // Get the note letter name
                string noteLetter = GetNoteName(note.NoteNumber);
                
                // Create note data
                NoteData noteData = new NoteData(
                    note: note.NoteNumber,
                    letter: noteLetter,
                    start: startTime,
                    end: endTime
                );
                
                noteDataList.Add(noteData);
            }
            
            // Sort by start time
            noteDataList = noteDataList.OrderBy(n => n.start).ToList();
            
            Debug.Log($"Successfully loaded {noteDataList.Count} notes from {song}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error reading MIDI file '{song}': {e.Message}");
        }
        
        return new MidiSong
        {
            name = song,
            length = length,
            bpm = bpm,
            notes = noteDataList
        };
    }
    
    /// <summary>
    /// Static method to read a MIDI file and return only the list of note data
    /// GameManager can call: var notes = MidiNoteReader.GetNotesFromMidi("song.mid");
    /// </summary>
    /// <param name="fileName">Name of the MIDI file (without path)</param>
    /// <returns>List of NoteData containing note number, letter, start time, and end time</returns>
    public static List<NoteData> GetNotesFromMidi(string fileName)
    {
        // Use the main method and extract just the notes
        return LoadMidiSongFromPath(fileName).notes;
    }
    
    /// <summary>
    /// Static method to read MIDI file from a full path
    /// </summary>
    public static List<NoteData> GetNotesFromMidiPath(string fullPath)
    {
        List<NoteData> noteDataList = new List<NoteData>();
        
        try
        {
            var midiFile = MidiFile.Read(fullPath);
            var tempoMap = midiFile.GetTempoMap();
            var notes = midiFile.GetNotes();
            
            foreach (var note in notes)
            {
                float startTime = (float)note.TimeAs<MetricTimeSpan>(tempoMap).TotalSeconds;
                float duration = (float)note.LengthAs<MetricTimeSpan>(tempoMap).TotalSeconds;
                
                noteDataList.Add(new NoteData(
                    note: note.NoteNumber,
                    letter: GetNoteName(note.NoteNumber),
                    start: startTime,
                    end: startTime + duration
                ));
            }
            
            noteDataList = noteDataList.OrderBy(n => n.start).ToList();
            Debug.Log($"Loaded {noteDataList.Count} notes from {fullPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error reading MIDI file from path: {e.Message}");
        }
        
        return noteDataList;
    }
    
    /// <summary>
    /// Convert MIDI note number to note name
    /// </summary>
    private static string GetNoteName(int midiNoteNumber)
    {
        if (midiNoteNumber < 0 || midiNoteNumber > 127) return "Invalid";
        
        string[] noteNames = {"C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"};
        int octave = (midiNoteNumber / 12) - 1;
        string noteName = noteNames[midiNoteNumber % 12];
        return $"{noteName}{octave}";
    }
    
    /// <summary>
    /// Get notes within a specific time range
    /// </summary>
    public static List<NoteData> GetNotesInTimeRange(List<NoteData> allNotes, float startTime, float endTime)
    {
        return allNotes.Where(n => n.start < endTime && n.end > startTime).ToList();
    }
    
    /// <summary>
    /// Get the note playing at a specific time (returns first one if multiple)
    /// </summary>
    public static NoteData? GetNoteAtTime(List<NoteData> allNotes, float time)
    {
        var note = allNotes.FirstOrDefault(n => time >= n.start && time < n.end);
        return note.note != 0 || note.start != 0 ? (NoteData?)note : null;
    }
    
    /// <summary>
    /// Debug helper to print notes to console
    /// </summary>
    public static void PrintNotes(List<NoteData> notes, int maxCount = 10)
    {
        Debug.Log($"=== MIDI Notes (showing {Mathf.Min(maxCount, notes.Count)} of {notes.Count}) ===");
        for (int i = 0; i < Mathf.Min(maxCount, notes.Count); i++)
        {
            var n = notes[i];
            Debug.Log($"  [{i}] {n.letter} (MIDI #{n.note}) | Start: {n.start:F3}s | End: {n.end:F3}s | Duration: {(n.end - n.start):F3}s");
        }
    }
    
    /// <summary>
    /// Get the lowest and highest MIDI note numbers from a list of notes.
    /// Returns (lowest, highest) as a tuple.
    /// </summary>
    public static (int lowest, int highest) GetNoteRange(List<NoteData> notes)
    {
        int lowestP = 0;
        int mP = 0;
        int lowest = 1000;
        int highest = 0;
        // int lowest = notes.Min(n => n.note);
        // int highest = notes.Max(n => n.note);
        for (int i = 0; i < notes.Count; i++)
        {
            var n = notes[i];
            if (n.note < lowest)
            {
                lowest = n.note;
                lowestP = i;
            }
            if (n.note > highest)
            {
                highest = n.note;
                mP = i;
            }
        }
        Debug.Log($"lowest {lowestP}");
        Debug.Log($"highest {mP}");

        return (lowest, highest);
    }
}