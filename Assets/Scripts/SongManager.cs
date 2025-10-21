using System.Runtime.InteropServices;
using UnityEngine;

public class SongManager : MonoBehaviour
{
    public static SongManager Instance { get; private set; }

    [Header("Assign the KaraokeBoxUIManager from Inspector")]
    public KaraokeBoxUIManager karaokeManager;
    
    [Header("Assign the DamageCalculator from Inspector")]
    public DamageCalculator damageCalculator;
    
    [Header("Time Before Song Starts")]
    public float timeBeforeSongStarts = 2f;
    
    [Header("Toggle Backing Track")]
    public bool playBackingTrack = false;
    
    private AudioSource audioSourceBackingTrack;

    [SerializeField] private float[] timeStamps;
    private int timeStampIndex = 0;

    private float songTime;
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
        audioSourceBackingTrack = GetComponent<AudioSource>();
        songTime = -timeBeforeSongStarts;
        karaokeManager.pitchDetector = GameManager.GetPitchDetection(currentPlayer - 1);
        damageCalculator.pitchDetector = GameManager.GetPitchDetection(currentPlayer - 1);
    }

    void Update()
    {
        if (isPlayingSong)
        {
            songTime += Time.deltaTime;
            Debug.Log(songTime);
            if (timeStampIndex < timeStamps.Length && songTime >= timeStamps[timeStampIndex])
            {
                currentPlayer = (currentPlayer == 1) ? 2 : 1;
                // TODO, we should simplify this so that it does not require explicit switching
                // just pull the current player from the song manager
                damageCalculator.SwitchPlayer();
                karaokeManager.pitchDetector = GameManager.GetPitchDetection(currentPlayer - 1);
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
        MidiNoteReader.MidiSong midiSong = MidiNoteReader.LoadMidiSongFromPath($"/{song}/{song}.mid");
        karaokeManager.StartPlaying(midiSong, -timeBeforeSongStarts);
        damageCalculator.StartRecordingDamage(-timeBeforeSongStarts - 9f);

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

    public void EndSong()
    {
        audioSourceBackingTrack.Stop();
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
