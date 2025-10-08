using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // Example persistent data
    public int playerScore = 0;

    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log(scene.buildIndex);
        Button startButton = GameObject.Find("StartSong")?.GetComponent<Button>();
        if (startButton != null)
        {
            startButton.onClick.AddListener(StartGame);
        }
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

    // Load any scene by name
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}

