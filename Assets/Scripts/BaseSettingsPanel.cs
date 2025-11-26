using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class BaseSettingsPanel : MonoBehaviour
{
    public TMP_Text title;
    public GameObject settingsPanel;
    public GameObject mainMenu;
    public GameObject selectLevel;
    public SelectedObjectManager selectedObjectManager;

    private int dropdownIndex = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (EventSystem.current.currentSelectedGameObject == null)
        {
            if (settingsPanel.activeSelf)
            {
                Debug.Log("Re-selecting Base Settings Panel");
                switch (dropdownIndex)
                {
                    case 0:
                        SelectedObjectManager.Instance.SetSelectedObject("BaseSettings");
                        break;
                    case 1:
                        SelectedObjectManager.Instance.SetSelectedObject("OcativeSelection");
                        break;
                }
                SelectedObjectManager.Instance.SetSelectedObject("BaseSettings");
            }
        }
    }
    void OnEnable()
    {
        SettingsPanel settingsPanel = GetComponentInParent<SettingsPanel>();
        int playerID = settingsPanel.currentPlayer;

        title.SetText("Acoustic Assault Settings - Player " + settingsPanel.currentPlayer.ToString());
    }

    public void setDropdownIndex(int index)
    {
        dropdownIndex = index;
    }
}
