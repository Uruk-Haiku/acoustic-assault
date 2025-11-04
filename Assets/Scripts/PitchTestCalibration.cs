using Lasp;
using TMPro;
using UnityEngine;

public class PitchTestCalibration : MonoBehaviour
{
    public TMP_Text offsetText;
    private int playerID;
    private PitchDetector pitchDetector;
    
    private float lastInputTime = 0f;
    private const float INPUT_DELAY = 0.2f; // 200ms delay between inputs

    void OnEnable()
    {
        // Get current player
        SettingsPanel settingsPanel = GetComponentInParent<SettingsPanel>();
        playerID = settingsPanel.currentPlayer;
        pitchDetector = GameManager.GetPitchDetection(playerID);
        offsetText.text = $"Pitch detection is off set by {pitchDetector.pitchOffsetInSemitones} semitones";
    }

    void Update()
    {
        // Only process input if enough time has passed since last input
        if (Time.time - lastInputTime < INPUT_DELAY)
            return;
            
        float verticalInput = Input.GetAxisRaw("Vertical");

        if (verticalInput > 0.5f || Input.GetKeyDown(KeyCode.UpArrow))
        {
            pitchDetector.pitchOffsetInSemitones += 1;
            offsetText.text = $"Pitch detection is off set by {pitchDetector.pitchOffsetInSemitones} semitones";
            lastInputTime = Time.time;
        }
        else if (verticalInput < -0.5f || Input.GetKeyDown(KeyCode.DownArrow))
        {
            pitchDetector.pitchOffsetInSemitones -= 1;
            offsetText.text = $"Pitch detection is off set by {pitchDetector.pitchOffsetInSemitones} semitones";
            lastInputTime = Time.time;
        }
    }
}
