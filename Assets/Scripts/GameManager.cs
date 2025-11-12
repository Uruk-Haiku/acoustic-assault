using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Lasp;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public GameObject mainCamera;
    public GameObject winLoseCamera;
    public GameObject canvasObj;

    public GameObject pinkIdle;
    public GameObject pinkWin;
    public GameObject pinkLose;
    public GameObject greenIdle;
    public GameObject greenWin;
    public GameObject greenLose;
    public GameObject settingsCanvasUI;
    [SerializeField] public int[] initialPlayerIDs = { 0, 1 };
    public Dictionary<int, PitchDetector> pitchDetectors = new Dictionary<int, PitchDetector>();
    public Dictionary<int, GameObject> playerGameObjects = new Dictionary<int, GameObject>();
    private int playerScore = 0;

    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (settingsCanvasUI != null)
        {
            DontDestroyOnLoad(settingsCanvasUI);
        }
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        // Unsubscribe from event to prevent memory leaks
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        // Clear the instance if this is being destroyed
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Start()
    {
        CreateInitialPlayers();
    }

    void CreateInitialPlayers()
    {
        foreach (int playerID in initialPlayerIDs)
        {
            AddPlayer(playerID);
        }
    }

    public static bool AddPlayer(int playerID)
    {
        if (Instance == null) return false;
        if (Instance.pitchDetectors.ContainsKey(playerID))
        {
            Debug.LogWarning("Player with ID " + playerID + " already exists.");
            return false;
        }
        GameObject playerObj = new GameObject();
        playerObj.name = $"PitchDetector_Player{playerID}";
        DontDestroyOnLoad(playerObj);

        PitchDetector pitchDetector = playerObj.AddComponent<PitchDetector>();

        Instance.pitchDetectors[playerID] = pitchDetector;
        Instance.playerGameObjects[playerID] = playerObj;

        Debug.Log("Added player with ID " + playerID);
        return true;
    }

    public static bool RemovePlayer(int playerID)
    {
        if (Instance == null) return false;
        if (!Instance.pitchDetectors.ContainsKey(playerID))
        {
            Debug.LogWarning("Player with ID " + playerID + " does not exist.");
            return false;
        }

        // Destroy the GameObject when removing player
        if (Instance.playerGameObjects.TryGetValue(playerID, out GameObject obj))
        {
            Destroy(obj);
        }

        Instance.pitchDetectors.Remove(playerID);
        Instance.playerGameObjects.Remove(playerID);
        Debug.Log("Removed player with ID " + playerID);
        return true;
    }
    
    public static PitchDetector GetPitchDetection(int playerID)
    {
        if (Instance == null)
        {
            Debug.LogWarning("GameManager instance is null.");
            return null;
        }

        Debug.Log(Instance.pitchDetectors.TryGetValue(playerID, out PitchDetector det) ? det : null);
        return Instance.pitchDetectors.TryGetValue(playerID, out PitchDetector detector) ? detector : null;
    }

    public static GameObject GetPlayerGameObject(int playerID)
    {
        if (Instance == null)
        {
            Debug.LogWarning("GameManager instance is null.");
            return null;
        }
        return Instance.playerGameObjects.TryGetValue(playerID, out GameObject obj) ? obj : null;
    }
    
    public static int GetPlayerCount()
    {
        return Instance.pitchDetectors.Count;
    }

    public static bool HasPlayer(int playerID)
    {
        return Instance.pitchDetectors.ContainsKey(playerID);
    }

    public static int GetPlayerScore()
    {
        return Instance != null ? Instance.playerScore : 0;
    }

    public static void SetPlayerScore(int score)
    {
        if (Instance != null)
        {
            Instance.playerScore = score;
        }
    }

    public static void AddToPlayerScore(int points)
    {
        if (Instance != null)
        {
            Instance.playerScore += points;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log(scene.buildIndex);
        // Button startButton = GameObject.Find("StartSong")?.GetComponent<Button>();
        // if (startButton != null)
        // {
        //     startButton.onClick.AddListener(Instance.StartGame);
        // }

        // Find all relevant objects
        canvasObj = GameObject.Find("Canvas");
        mainCamera = GameObject.Find("Main Camera");
        winLoseCamera = GameObject.Find("Win Lose Camera");
        pinkIdle = GameObject.Find("pink_birb_idle");
        pinkWin = GameObject.Find("Pink_birb_win");
        pinkLose = GameObject.Find("Pink_birb_lose");
        greenIdle = GameObject.Find("green_birb_idle");
        greenWin = GameObject.Find("Green_birb_win");
        greenLose = GameObject.Find("Green_birb_lose");
        winLoseCamera.SetActive(false);
        pinkWin.SetActive(false);
        pinkLose.SetActive(false);
        greenWin.SetActive(false);
        greenLose.SetActive(false);

        if (scene.name.StartsWith("Level"))
        {
            char lastChar = scene.name[^1];
            int levelNum = lastChar - '0';

            // TODO: hardcoding level 2 (All I want for christmas) to set an additional pitch offset of 1 octave
            if (levelNum == 2)
            {
                foreach (var kvp in Instance.pitchDetectors)
                {
                    kvp.Value.songSpecificOffsetInSemitones = 12;
                }
            }
            else
            {
                foreach (var kvp in Instance.pitchDetectors)
                {
                    kvp.Value.songSpecificOffsetInSemitones = 0;
                }
            }
            SongManager.Instance.damageCalculator =  GameObject.Find("DamageCalculator")?.GetComponent<DamageCalculator>();
            SongManager.Instance.karaokeManager = GameObject.Find("KaraokeBox")?.GetComponent<KaraokeBoxUIManager>();
            Instance.StartGame(levelNum);
        }
    }
    public void StartGame(int levelNum)
    {
        StartCoroutine(StartGameCoroutine(levelNum));
    }

    IEnumerator StartGameCoroutine(int levelNum)
    {

        mainCamera.SetActive(true);
        winLoseCamera.SetActive(false);
        Animator animator = mainCamera.GetComponent<Animator>();
        animator.SetTrigger("StartAnim");
        GameObject startScreen = GameObject.Find("StartScreen");
        CanvasGroup canvasGroup = canvasObj.GetComponent<CanvasGroup>();
        startScreen.SetActive(false);
        if (canvasGroup != null)
        {
            yield return StartCoroutine(EaseInCanvas(canvasGroup));
        }
        SongManager.Instance.StartSong(levelNum);
    }

    public static void PauseGame()
    {
        SongManager.Instance?.PauseSong();
    }

    public static void UnPauseGame()
    {
        SongManager.Instance?.UnPauseSong();
    }
    
    IEnumerator EaseInCanvas(CanvasGroup canvasGroup)
    {
        float timer = 0;
        while (timer < 5f)
        {
            canvasGroup.alpha = Mathf.Max(0f, (timer - 2f) / 3f);
            timer += Time.deltaTime;
            yield return null;
        }
    }

    public static void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
    public static void RecreatePitchDetector(int playerID)
    {
        if (Instance == null) return;
        if (!Instance.pitchDetectors.ContainsKey(playerID))
        {
            Debug.LogWarning($"Player {playerID} doesn't exist!");
            return;
        }

        // Remove and destroy
        GameObject oldObj = Instance.playerGameObjects[playerID];
        Destroy(oldObj);
        Instance.pitchDetectors.Remove(playerID);
        Instance.playerGameObjects.Remove(playerID);

        // Recreate
        AddPlayer(playerID);

        Debug.Log($"Recreated PitchDetection for Player {playerID}");
    }
}