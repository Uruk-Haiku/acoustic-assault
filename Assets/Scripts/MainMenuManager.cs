using UnityEngine;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject selectLevelPanel;

    public Button selectLevelButton;
    public Button quitButton;
    public Button backButton;

    // Level Buttons
    public Button level1Button;
    public string level1SceneName;

    void Start()
    {
        // Hook buttons to methods
        selectLevelButton.onClick.AddListener(() => ShowPanel(selectLevelPanel));
        quitButton.onClick.AddListener(() => Application.Quit());
        backButton.onClick.AddListener(() => ShowPanel(mainMenuPanel));
        level1Button.onClick.AddListener(() => GameManager.Instance.LoadScene(level1SceneName));

        // Show main menu by default
        ShowPanel(mainMenuPanel);
    }

    void ShowPanel(GameObject panelToShow)
    {
        mainMenuPanel.SetActive(false);
        selectLevelPanel.SetActive(false);

        panelToShow.SetActive(true);
    }
}
