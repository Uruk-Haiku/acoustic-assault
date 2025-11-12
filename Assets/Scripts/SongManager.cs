using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using TMPro;
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

    [Header("Toggle Backing Track")]
    public bool playBackingTrack = false;

    private AudioSource audioSourceBackingTrack;
    private AudioSource audioSourceSfx;

    [SerializeField] private float[] timeStamps1;
    [SerializeField] private float[] timeStamps2;

    private float[] timeStamps;
    private int timeStampIndex = 0;

    public float songTime;
    private bool isPlayingSong = false;
    public bool isLevelFinished = false;

    public int currentPlayer = 1;

    // TODO: HARDCODING TO 2 PLAYERS. DON'T KEEP THIS
    public List<EmotionScore> currentEmotionList = new List<EmotionScore> { EmotionScore.Good, EmotionScore.Good };
    public List<float> emotionScoreList = new List<float> { 0f, 0f };
    public List<float> timeToGetPerfectScoreList = new List<float> { 0f, 0f };
    public List<float> emotionScoreRatioList
    {
        get
        {
            List<float> ratios = new List<float>();
            for (int i = 0; i < emotionScoreList.Count; i++)
            {
                float ratio = timeToGetPerfectScoreList[i] > 0 ? emotionScoreList[i] / timeToGetPerfectScoreList[i] : 0f;
                ratios.Add(ratio);
            }
            return ratios;
        }
    }
    float emotionScoreDecaySpeed = 0.03f;

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
    }

    void Update()
    {
        if (isPlayingSong)
        {
            updatePopup();
            songTime += Time.deltaTime;
            if (timeStampIndex < timeStamps.Length && songTime >= timeStamps[timeStampIndex])
            {
                currentPlayer = (currentPlayer == 1) ? 2 : 1;
                // TODO, we should simplify this so that it does not require explicit switching
                // just pull the current player from the song manager
                damageCalculator.SwitchPlayer();
                karaokeManager.pitchDetector = GameManager.GetPitchDetection(currentPlayer - 1);
                // Switch cursor image
                // This should really be happening in karaokeManager but idc.
                Sprite cursorSprite = Resources.Load<Sprite>((currentPlayer == 1) ? "UI/LoudnessCursor (2)" : "UI/LoudnessCursor (1)");
                if (cursorSprite != null)
                {
                    karaokeManager.Cursor.GetComponent<Image>().sprite = cursorSprite;
                }
                damageCalculator.pitchDetector = GameManager.GetPitchDetection(currentPlayer - 1);
                timeStampIndex++;
                float timeToGetPerfectScore = timeStamps[timeStampIndex] - timeStamps[timeStampIndex - 1];
                timeToGetPerfectScoreList[currentPlayer - 1] += timeToGetPerfectScore;
                // Assume that player has already sang half perfectly for given verse
                emotionScoreList[currentPlayer - 1] += 0.5f * timeToGetPerfectScore;
            }

            if (!isLevelFinished && (timeStampIndex >= timeStamps.Length || damageCalculator.isGameOver))
            {
                isLevelFinished = true;
                GameManager.Instance.canvasObj.SetActive(false);
                int winResult = damageCalculator.gameEndState;
                // damageCalculator.ShowWinningMessage(damageCalculator.gameEndState);
                // turn off song
                audioSourceBackingTrack.Stop();
                // turn on sound
                audioSourceSfx.Play();
                // switchCamera
                GameManager.Instance.mainCamera.SetActive(false);
                GameManager.Instance.winLoseCamera.SetActive(true);

                GameManager.Instance.greenIdle.SetActive(false);
                GameManager.Instance.pinkIdle.SetActive(false);
                if (winResult == 0)
                {
                    GameManager.Instance.greenWin.SetActive(true);
                    GameManager.Instance.pinkWin.SetActive(true);
                    // TODO: fix this
                    TMP_Text winText = GameObject.Find("WinText").GetComponent<TMP_Text>();
                    winText.text = "It's a Tie!";
                }
                else if (winResult == 1)
                {
                    GameManager.Instance.greenLose.SetActive(true);
                    GameManager.Instance.pinkWin.SetActive(true);

                    TMP_Text winText = GameObject.Find("WinText").GetComponent<TMP_Text>();
                    winText.text = "Player 1 Wins!";
                }
                else if (winResult == 2)
                {
                    GameManager.Instance.greenWin.SetActive(true);
                    GameManager.Instance.pinkLose.SetActive(true);
                    TMP_Text winText = GameObject.Find("WinText").GetComponent<TMP_Text>();
                    winText.text = "Player 2 Wins!";

                }
                StartCoroutine(ExitToMainMenu());
            }
        }
    }

    IEnumerator ExitToMainMenu()
    {
        yield return new WaitForSeconds(10);
        audioSourceSfx.Stop();
        isPlayingSong = false;
        damageCalculator.startRecording = false;
        songTime = 0;
        timeStampIndex = 0;
        SongManager.Instance.EndSong();
        GameManager.LoadScene("MenuScreen");
    }

    public void StartSong(int levelNum)
    {
        string song = "";
        if (levelNum == 1)
        {
            timeStamps = timeStamps1;
            song = "IWantItThatWay";
        }
        else if (levelNum == 2)
        {
            timeStamps = timeStamps2;
            song = "AllIWantForChristmas";
        }

        Debug.Log(song);

        songTime = 0;
        timeStampIndex = 0;
        isPlayingSong = true;

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
            audioSourceBackingTrack.PlayDelayed(0);
        }

        // Load sfx

        MidiNoteReader.MidiSong midiSong = MidiNoteReader.LoadMidiSongFromPath(song);
        if (levelNum == 1)
        {
            karaokeManager.StartPlaying(midiSong, 9f);
        }
        else if (levelNum == 2)
        {
            karaokeManager.StartPlaying(midiSong, 8f);
        }

        damageCalculator.StartRecordingDamage();

        karaokeManager.pitchDetector = GameManager.GetPitchDetection(currentPlayer - 1);
        damageCalculator.pitchDetector = GameManager.GetPitchDetection(currentPlayer - 1);

        for (int i = 0; i < 2; i++)
        {
            currentEmotionList.Add(EmotionScore.Good);
            emotionScoreList.Add(0);
            timeToGetPerfectScoreList.Add(0);
        }


        print(currentPlayer - 1);
        print(timeToGetPerfectScoreList.Count);

        // Init values for first player
        timeToGetPerfectScoreList[0] = timeStamps[timeStampIndex];
        emotionScoreList[0] = 0.5f * timeToGetPerfectScoreList[0];

        isLevelFinished = false;
    }

    public void EndSong()
    {
        audioSourceBackingTrack.Stop();
        audioSourceBackingTrack.time = 0f;
        audioSourceBackingTrack.clip = null;
    }

    public void PauseSong()
    {
        if (songTime > 0)
        {
            isPlayingSong = false;
            audioSourceBackingTrack.Pause();
        }
        Time.timeScale = 0f;
    }
    public void UnPauseSong()
    {
        if (songTime > 0)
        {
            isPlayingSong = true;
            audioSourceBackingTrack.Play();
        }
        Time.timeScale = 1f;
    }
    void updatePopup()
    {
        if (karaokeManager.isNullNote)
        {
            return;
        }
        // Only apply decay when decreasing (target is lower than current)
        if (karaokeManager.isSparkling)
        {
            emotionScoreList[currentPlayer - 1] += 3 * Time.deltaTime;
        }
        else
        {
            emotionScoreList[currentPlayer - 1] = Mathf.Lerp(emotionScoreList[currentPlayer - 1], 0, emotionScoreDecaySpeed * Time.deltaTime);
        }

        // Determine emotion from smoothed score
        EmotionScore newEmotion = currentEmotionList[currentPlayer - 1];
        float emotionScoreRatio = emotionScoreList[currentPlayer - 1] / timeToGetPerfectScoreList[currentPlayer - 1];

        if (emotionScoreRatioList[currentPlayer - 1] >= 0.7f)
        {
            newEmotion = EmotionScore.Great;
        }
        else if (emotionScoreRatioList[currentPlayer - 1] >= 0.35f)
        {
            newEmotion = EmotionScore.Good;
        }
        else
        {
            newEmotion = EmotionScore.Bad;
        }

        if (newEmotion != currentEmotionList[currentPlayer - 1])
        {
            currentEmotionList[currentPlayer - 1] = newEmotion;
            damageCalculator.portraitsChange.ShowPopupForPlayer(currentPlayer - 1, newEmotion);
        }
    }
}
