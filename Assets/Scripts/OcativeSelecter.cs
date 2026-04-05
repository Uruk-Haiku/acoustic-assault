using Lasp;
using TMPro;
using UnityEngine;

public class OcativeSelecter : MonoBehaviour
{
    private int playerID;
    private PitchDetector pitchDetector;
    private TMP_Dropdown dropdown;

    //private float lastInputTime = 0f;

    void Awake()
    {
        dropdown = GetComponent<TMP_Dropdown>();
    }

    void OnEnable()
    {
        // Get current player
        SettingsPanel settingsPanel = GetComponentInParent<SettingsPanel>();
        playerID = settingsPanel.currentPlayer;
        pitchDetector = GameManager.GetPitchDetection(playerID);

        // Refresh dropdown to match this player's current octave setting
        if (dropdown != null && pitchDetector != null)
        {
            int index = pitchDetector.pitchOffsetInSemitones switch
            {
                12 => 0,
                24 => 1,
                0 => 2,
                _ => 0
            };
            dropdown.SetValueWithoutNotify(index);
            dropdown.RefreshShownValue();
        }
    }

    void Update()
    {
    }

    public void OnDropdownValueChanged(int value)
    {
        SettingsPanel settingsPanel = GetComponentInParent<SettingsPanel>();
        playerID = settingsPanel.currentPlayer;
        pitchDetector = GameManager.GetPitchDetection(playerID);

        Debug.Log("Current Player" + settingsPanel.currentPlayer);

        switch(value)
        {
            case 0:
                pitchDetector.pitchOffsetInSemitones = 12;
                break;
            case 1:
                pitchDetector.pitchOffsetInSemitones = 24;
                break;
            case 2:
                pitchDetector.pitchOffsetInSemitones = 0;
                break;
        }
        Debug.Log("Ocative selected: " + pitchDetector.pitchOffsetInSemitones);
    }
}
