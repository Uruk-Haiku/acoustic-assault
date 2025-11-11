using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

public class SongManager : MonoBehaviour
{
    public static SongManager Instance { get; private set; }

    [Header("Assign the KaraokeBoxUIManager from Inspector")]
    public KaraokeBoxUIManager karaokeManager;
    
    [Header("Assign the DamageCalculator from Inspector")]
    public DamageCalculator damageCalculator;
    
    [Header("Time Before Song Starts")]
    public float timeBeforeSongStarts = 0f;
    
    [Header("Toggle Backing Track")]
    public bool playBackingTrack = false;

    private AudioSource audioSourceBackingTrack;
    private AudioSource audioSourceSfx;

    [SerializeField] private float[] timeStamps;
    private int timeStampIndex = 0;

    public float songTime;
    private bool isPlayingSong = false;
    
    public int currentPlayer = 1;

    public int GetPlayerFromTime(float time)
    {
        for (int i = 0; i < timeStamps.Length; i++)
        {
            if (time < timeStamps[i])
            {
                return (i % 2 == 0) ? 1 : 2;
            }
        }
        return 0;
    }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // prevent duplicate managers
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // optional: persist between scenes
    }

    void Start()
    {
        AudioSource[] audioSources = GetComponents<AudioSource>();
        audioSourceBackingTrack = audioSources[0];
        audioSourceSfx = audioSources[1];
        songTime = -timeBeforeSongStarts;
        karaokeManager.pitchDetector = GameManager.GetPitchDetection(currentPlayer - 1);
        damageCalculator.pitchDetector = GameManager.GetPitchDetection(currentPlayer - 1);
    }

    void Update()
    {
        if (isPlayingSong)
        {
            songTime += Time.deltaTime;
            if (timeStampIndex < timeStamps.Length && songTime >= timeStamps[timeStampIndex])
            {
                currentPlayer = (currentPlayer == 1) ? 2 : 1;
                // TODO, we should simplify this so that it does not require explicit switching
                // just pull the current player from the song manager
                damageCalculator.SwitchPlayer();
                karaokeManager.pitchDetector = GameManager.GetPitchDetection(currentPlayer - 1);
                // Switch cursor image
                Sprite cursorSprite = Resources.Load<Sprite>((currentPlayer == 1) ? "UI/LoudnessCursor (2)" : "UI/LoudnessCursor (1)");
                if (cursorSprite != null)
                {
                    karaokeManager.Cursor.GetComponent<Image>().sprite = cursorSprite;
                }
                damageCalculator.pitchDetector = GameManager.GetPitchDetection(currentPlayer - 1);
                timeStampIndex++;
            }

            if (timeStampIndex >= timeStamps.Length)
            {
                damageCalculator.EndGame();
            }
        }
    }

    public void StartSong(string song = "IWantItThatWay")
    {
        isPlayingSong = true;
        //MidiNoteReader.MidiSong midiSong = MidiNoteReader.LoadMidiSongFromPath($"/{song}/{song}.mid");
        string path = Path.Combine(Application.streamingAssetsPath, "Songs", "IWantItThatWay", $"{"IWantItThatWay"}.mid");
        MidiNoteReader.MidiSong midiSong = MidiNoteReader.LoadMidiSongFromPath(path);
        karaokeManager.StartPlaying(midiSong, 0);
        damageCalculator.StartRecordingDamage();
        
        karaokeManager.pitchDetector = GameManager.GetPitchDetection(currentPlayer - 1);
        damageCalculator.pitchDetector = GameManager.GetPitchDetection(currentPlayer - 1);

        if (playBackingTrack)
        {
            AudioClip clip = Resources.Load<AudioClip>($"Music/Songs/{song}/{song}Backing");
            if (clip == null)
            {
                Debug.LogError($"AudioClip Assets/Resources/Music/Songs/{song}/{song}Backing.wav not found in Resources.");
                return;
            }
            Debug.Log($"AudioClip Assets/Resources/Music/Songs/{song}/{song}Backing.wav Loaded");
            audioSourceBackingTrack.clip = clip;
            audioSourceBackingTrack.PlayDelayed(timeBeforeSongStarts);
        }
        
    }

    public void EndSong()
    {
        audioSourceBackingTrack.Stop();
        audioSourceBackingTrack.time = 0f;
        audioSourceBackingTrack.clip = null;
        isPlayingSong = false;
    }

    public void PauseSong()
    {
        isPlayingSong = false;
        audioSourceBackingTrack.Pause();
        Time.timeScale = 0f;
    }
    public void UnPauseSong()
    {
        isPlayingSong = true;
        audioSourceBackingTrack.Play();
        Time.timeScale = 1f;
    }
}
