using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Rudimentary SettingsManager that allows player 1 to open setting menu with button press esc, and player 2 with e
public class SettingsManager : MonoBehaviour
{
    [SerializeField] private GameObject settingsCanvasUI;
    [SerializeField] private SettingsPanel settingsPanel;

    public GameObject mainMenuPanel;
    public GameObject selectLevelPanel;

    public SelectedObjectManager selectedObjectManager;

    public Button[] buttonsToDisableWhenSettingsOpen;

    private MenuUIController menuUIController;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleSettingsForPlayer(0);
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            ToggleSettingsForPlayer(1);
        }

        if (Input.GetKeyDown(KeyCode.JoystickButton2))
        {
            ToggleSettingsForPlayer(0);
        }

        if (Input.GetKeyDown(KeyCode.JoystickButton3))
        {
            ToggleSettingsForPlayer(1);
        }
    }
    public void ToggleSettingsForPlayer(int playerID)
    {
        
        if (GameManager.GetCurrentTutorialStage() == 2 && playerID == 0)
        {
            GameManager.CloseTutorialPopup();
        }
        else if (GameManager.GetCurrentTutorialStage() == 4 && playerID == 1)
        {
            GameManager.CloseTutorialPopup();
        }
        
        
        // Check if canvas is currently active
        if (settingsCanvasUI.activeSelf)
        {
            if (playerID == settingsPanel.currentPlayer)
            {
                if (GameManager.GetCurrentTutorialStage() == 3 && playerID == 0)
                {
                    GameManager.CloseTutorialPopup();
                    GameManager.GoToNextTutorialStage();
                }
                else if (GameManager.GetCurrentTutorialStage() == 4 && playerID == 1)
                {
                    GameManager.GoToNextTutorialStage();
                }
                
                GameManager.UnPauseGame();
                settingsCanvasUI.SetActive(false);
                if (SceneManager.GetActiveScene().name == "MenuScreen")
                {
                    CheckBackGroundMenu();
                }
                else if (SceneManager.GetActiveScene().name == "SingingUI")
                {
                    menuUIController = FindAnyObjectByType<MenuUIController>();
                    if (menuUIController != null)
                    {
                        menuUIController.SelectStartSong();
                    }
                }
            }
        }
        else
        {
            GameManager.PauseGame();
            settingsPanel.currentPlayer = playerID;
            // Open settings for this player
            settingsCanvasUI.SetActive(true);
            settingsPanel.ShowForPlayer(playerID);
            if (SceneManager.GetActiveScene().name == "MenuScreen")
            {
                DisableNavigation();
            }
            selectedObjectManager.SetSelectedObject("BaseSettings");
        }
    }
    public void ExitSettings()
    {
        
        if (GameManager.GetCurrentTutorialStage() == 3)
        {
            GameManager.CloseTutorialPopup();
            GameManager.GoToNextTutorialStage();
        }
        else if (GameManager.GetCurrentTutorialStage() == 4)
        {
            GameManager.GoToNextTutorialStage();
        }
        
        if (settingsCanvasUI.activeSelf)
        {
            GameManager.UnPauseGame();
            settingsCanvasUI.SetActive(false);
            if (SceneManager.GetActiveScene().name == "MenuScreen")
            {
                CheckBackGroundMenu();
            }
            else if (SceneManager.GetActiveScene().name == "Level1")
            {
                menuUIController = FindAnyObjectByType<MenuUIController>();
                if (menuUIController != null)
                {
                    menuUIController.SelectStartSong();
                }
            }
        }
    }

    private void CheckBackGroundMenu()
    {
        if (mainMenuPanel.activeSelf)
        {
            selectedObjectManager.SetSelectedObject("MainMenu");
        }
        else if (selectLevelPanel.activeSelf)
        {
            selectedObjectManager.SetSelectedObject("SelectLevel");
        }
        EnableNavigation();
    }
    public void DisableNavigation()
    {
        foreach (var b in buttonsToDisableWhenSettingsOpen)
        {
            var nav = new Navigation { mode = Navigation.Mode.None };
            b.navigation = nav;
        }
    }

    public void EnableNavigation()
    {
        foreach (var b in buttonsToDisableWhenSettingsOpen)
        {
            var nav = new Navigation { mode = Navigation.Mode.Automatic };
            b.navigation = nav;
        }
    }
}

