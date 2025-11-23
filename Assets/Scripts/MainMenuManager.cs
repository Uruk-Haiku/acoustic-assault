using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject selectLevelPanel;
    [SerializeField] private GameObject firstSelectedButton_MainMenu;
    [SerializeField] private GameObject firstSelectedButton_SelectLevel;

    public Button selectLevelButton;
    public Button quitButton;
    public Button backButton;

    // Level Buttons
    public Button level0Button;
    public string level0SceneName;
    
    public Button level1Button;
    public string level1SceneName;
    
    public Button level2Button;
    public string level2SceneName;
    

    public SelectedObjectManager selectedObjectManager;

    void Start()
    {
        // Hook buttons to methods
        selectLevelButton.onClick.AddListener(() => ShowPanel(selectLevelPanel));
        quitButton.onClick.AddListener(() => Application.Quit());
        backButton.onClick.AddListener(() => ShowPanel(mainMenuPanel));
        level0Button.onClick.AddListener(() => GameManager.LoadScene(level0SceneName));
        level1Button.onClick.AddListener(() => GameManager.LoadScene(level1SceneName));
        level2Button.onClick.AddListener(() => GameManager.LoadScene(level2SceneName));

        // Show main menu by default
        ShowPanel(mainMenuPanel);
    }

    void ShowPanel(GameObject panelToShow)
    {
        mainMenuPanel.SetActive(false);
        selectLevelPanel.SetActive(false);

        panelToShow.SetActive(true);
        StartCoroutine(SetSelectedNextFrame(panelToShow));
    }

    System.Collections.IEnumerator SetSelectedNextFrame(GameObject panelToShow)
    {
        // Wait one frame so Unity updates UI activation states
        yield return null;

        if (panelToShow == mainMenuPanel)
        {
            selectedObjectManager.SetSelectedObject("MainMenu");
        }
        else if (panelToShow == selectLevelPanel)
        {
            selectedObjectManager.SetSelectedObject("SelectLevel");
        }

        // Debug.Log("Selected button is now: " + EventSystem.current.currentSelectedGameObject.name);
    }
}
