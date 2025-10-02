using UnityEngine;
public class SongManager : MonoBehaviour
{
    [Header("Assign the KaraokeBoxUIManager from Inspector")]
    public KaraokeBoxUIManager karaokeManager;
    
    [Header("Time Before Song Starts")]
    public float timeBeforeSongStarts = 2f;
    
    [Header("Toggle Backing Track")]
    public bool playBackingTrack = false;
    
    private AudioSource audioSourceBackingTrack;

    void Start()
    {
        audioSourceBackingTrack = GetComponent<AudioSource>();
    }
    
    
    public void StartSong(string song = "IWantItThatWay")
    {
        MidiNoteReader.MidiSong midiSong = MidiNoteReader.LoadMidiSongFromPath($"/{song}/{song}.mid");
        karaokeManager.StartPlaying(midiSong, -timeBeforeSongStarts);

        if (playBackingTrack)
        {
            AudioClip clip = Resources.Load<AudioClip>($"Music/Songs/{song}/{song}Backing");
            if (clip == null)
            {
                Debug.LogError($"AudioClip Assets/Resources/Music/Songs/{song}/{song}Backing.wav not found in Resources.");
                return;
            }

            audioSourceBackingTrack.clip = clip;
            audioSourceBackingTrack.PlayDelayed(timeBeforeSongStarts);
        }
    }
}
