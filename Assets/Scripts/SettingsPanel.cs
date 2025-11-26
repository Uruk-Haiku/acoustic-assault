using System.Collections.Generic;
using UnityEngine;

// The SettingsPanel script is just to hold the user ID of the player relevant to settings.
public class SettingsPanel : MonoBehaviour
{
    public enum SettingsPage
    {
        Base,
        Calibration
    }
    public int currentPlayer;
    public SettingsPage currPage;
    public List<GameObject> subPanels;
    public SelectedObjectManager selectedObjectManager;

    public void ShowForPlayer(int playerID)
    {
        currentPlayer = playerID;
        subPanels.ForEach((subPanel) =>
        {
            subPanel.SetActive(false);
        });
        currPage = SettingsPage.Base;
        subPanels[0].SetActive(true);
    }

    public void SwitchPage(SettingsPage page)
    {
        subPanels[(int)currPage].SetActive(false);
        currPage = page;
        subPanels[(int)currPage].SetActive(true);
    }

    public void SwitchPage(int page)
    {
        subPanels[(int)currPage].SetActive(false);
        currPage = (SettingsPage)page;
        subPanels[(int)currPage].SetActive(true);

        if (page == 1)
        {
            SelectedObjectManager.Instance.SetSelectedObject("DeviceSelection");
        }
    }
}
