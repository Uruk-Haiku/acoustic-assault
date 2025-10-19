using TMPro;
using UnityEngine;

public class BaseSettingsPanel : MonoBehaviour
{
    public TMP_Text title;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    { 
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void OnEnable()
    {
        SettingsPanel settingsPanel = GetComponentInParent<SettingsPanel>();
        int playerID = settingsPanel.currentPlayer;

        title.SetText("Acoustic Assault Settings - Player " + settingsPanel.currentPlayer.ToString());
    }
}
