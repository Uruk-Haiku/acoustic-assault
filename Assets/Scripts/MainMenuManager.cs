using UnityEngine;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject selectLevelPanel;

    public Button selectLevelButton;
    public Button quitButton;
    public Button backButton;

    void Start()
    {
        // Hook buttons to methods
        selectLevelButton.onClick.AddListener(() => ShowPanel(selectLevelPanel));
        quitButton.onClick.AddListener(() => Application.Quit());
        backButton.onClick.AddListener(() => ShowPanel(mainMenuPanel));

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
