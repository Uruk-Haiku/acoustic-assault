using Lasp;
using TMPro;
using UnityEngine;

public class OcativeSelecter : MonoBehaviour
{
    private int playerID;
    private PitchDetector pitchDetector;

    //private float lastInputTime = 0f;

    void OnEnable()
    {
        // Get current player
        SettingsPanel settingsPanel = GetComponentInParent<SettingsPanel>();
        playerID = settingsPanel.currentPlayer;
        pitchDetector = GameManager.GetPitchDetection(playerID);
    }

    void Update()
    {
        //float verticalInput = Input.GetAxisRaw("Vertical");

        //if (verticalInput > 0.5f || Input.GetKeyDown(KeyCode.UpArrow))
        //{
        //    pitchDetector.pitchOffsetInSemitones += 1;
        //    lastInputTime = Time.time;
        //}
        //else if (verticalInput < -0.5f || Input.GetKeyDown(KeyCode.DownArrow))
        //{
        //    pitchDetector.pitchOffsetInSemitones -= 1;
        //    lastInputTime = Time.time;
        //}
    }

    public void OnDropdownValueChanged(int value)
    {
        SettingsPanel settingsPanel = GetComponentInParent<SettingsPanel>();
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
