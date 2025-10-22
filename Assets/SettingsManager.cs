using UnityEngine;
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
        // Check if canvas is currently active
        if (settingsCanvasUI.activeSelf)
        {
            if (playerID == settingsPanel.currentPlayer)
            {
                GameManager.UnPauseGame();
                settingsCanvasUI.SetActive(false);
                CheckBackGroundMenu();
            }
        }
        else
        {
            GameManager.PauseGame();
            settingsPanel.currentPlayer = playerID;
            // Open settings for this player
            settingsCanvasUI.SetActive(true);
            settingsPanel.ShowForPlayer(playerID);
            DisableNavigation();
            selectedObjectManager.SetSelectedObject("BaseSettings");
        }
    }
    public void ExitSettings()
    {
        if (settingsCanvasUI.activeSelf)
        {
            settingsCanvasUI.SetActive(false);
            CheckBackGroundMenu();
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

