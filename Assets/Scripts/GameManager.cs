using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Lasp;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private GameObject pitchDetectionPrefab;
    public GameObject settingsCanvasUI;
    [SerializeField] public int[] initialPlayerIDs = { 0, 1 };
    public Dictionary<int, SimplePitchDetector> pitchDetectors = new Dictionary<int, SimplePitchDetector>();
    public Dictionary<int, GameObject> playerGameObjects = new Dictionary<int, GameObject>();
    private SongManager currSongManager;
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
        GameObject playerObj = Instantiate(Instance.pitchDetectionPrefab);
        playerObj.name = $"PitchDetector_Player{playerID}";
        DontDestroyOnLoad(playerObj);

        SimplePitchDetector pitchDetector = playerObj.AddComponent<SimplePitchDetector>();

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
    
    public static SimplePitchDetector GetPitchDetection(int playerID)
    {
        return Instance.pitchDetectors.TryGetValue(playerID, out SimplePitchDetector detector) ? detector : null;
    }

    public static GameObject GetPlayerGameObject(int playerID)
    {
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
        Button startButton = GameObject.Find("StartSong")?.GetComponent<Button>();
        if (startButton != null)
        {
            startButton.onClick.AddListener(Instance.StartGame);
        }

        currSongManager = GameObject.Find("SongManager")?.GetComponent<SongManager>();
    }
    public void StartGame()
    {
        GameObject canvasObj = GameObject.Find("Canvas");
        GameObject startScreen = GameObject.Find("StartScreen");
        CanvasGroup canvasGroup = canvasObj.GetComponent<CanvasGroup>();
        startScreen.SetActive(false);
        if (canvasGroup != null)
        {
            StartCoroutine(EaseInCanvas(canvasGroup));
        }
    }

    public static void PauseGame()
    {
        Instance?.currSongManager?.PauseSong();
    }

    public static void UnPauseGame()
    {
        Instance?.currSongManager?.UnPauseSong();
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
}