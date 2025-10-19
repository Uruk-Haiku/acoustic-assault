using UnityEngine;

// Rudimentary SettingsManager that allows player 1 to open setting menu with button press esc, and player 2 with e
public class SettingsManager : MonoBehaviour
{
    [SerializeField] private GameObject settingsCanvasUI;
    [SerializeField] private SettingsPanel settingsPanel;
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
            }
        }
        else
        {
            GameManager.PauseGame();
            settingsPanel.currentPlayer = playerID;
            // Open settings for this player
            settingsCanvasUI.SetActive(true);
            settingsPanel.ShowForPlayer(playerID);
        }
    }
    public void ExitSettings()
    {
        if (settingsCanvasUI.activeSelf)
        {
            settingsCanvasUI.SetActive(false);
        }
    }
}
