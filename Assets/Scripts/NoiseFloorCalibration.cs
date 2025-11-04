using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NoiseFloorCalibration : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text levelText;
    public Button recordButton;
    
    [Header("Recording Settings")]
    [SerializeField] private float recordTime = 3f; // Recording duration in seconds
    
    private float recordingTimer = 0f;
    private int playerID;
    private bool isRecording = false;
    private float avgDB = float.MinValue;
    private Lasp.PitchDetector pitchDetector;

    
    void OnEnable()
    {
        // Get current player
        SettingsPanel settingsPanel = GetComponentInParent<SettingsPanel>();
        playerID = settingsPanel.currentPlayer;
        pitchDetector = GameManager.GetPitchDetection(playerID);
        
        ResetRecording();
    }
    
    void OnDisable()
    {
        
    }
    
    // Update is called once per frame
    void Update()
    {
        if (pitchDetector == null)
        {
            Debug.LogWarning($"Pitch detector not found for Player {playerID}");
            return;
        }

        float currentDB = pitchDetector.gainedLevel;

        if (isRecording)
        {
            // Update recording timer with unscaled time
            recordingTimer += Time.unscaledDeltaTime;
            
            // Track average dB during recording
            avgDB += currentDB * Time.unscaledDeltaTime / recordTime;
            
            // Update UI with recording status and max dB
            levelText.text = $"Recording...Please be quiet...({recordingTimer:F1}s/{recordTime:F1}s)";
            
            // Check if recording time is complete
            if (recordingTimer >= recordTime)
            {
                StopRecording();
            }
        }
        else
        {
            // Normal display mode
            levelText.text = $"Avg Recorded: {(avgDB != float.MinValue ? avgDB.ToString("F1") : "N/A")}dB";
        }
    }
    
    public void StartRecording()
    {
        if (isRecording) return; // Prevent starting if already recording
        
        isRecording = true;
        recordingTimer = 0f;
        avgDB = 0f;

        // Update button state
        if (recordButton != null)
        {
            recordButton.interactable = false;
            recordButton.GetComponentInChildren<TMP_Text>().text = "Recording...";
        }

        Debug.Log($"Started noise floor calibration for Player {playerID}");
    }
    
    private void StopRecording()
    {
        isRecording = false;
        
        // Update button state
        if (recordButton != null)
        {
            recordButton.interactable = true;
            recordButton.GetComponentInChildren<TMP_Text>().text = "Record";
        }

        Debug.Log($"Recording complete! Max dB: {avgDB:F1}dB for Player {playerID}");

        // Now adjust dynamic range based on avgDB
        if (pitchDetector != null)
        {
            pitchDetector.dynamicRange = -avgDB;
            // TODO: keep this? Sneaking in some extra gain
            pitchDetector.gain += 4;
            Debug.Log($"Set dynamic range to {-avgDB:F1}dB for Player {playerID}");
        }
    }
    
    private void ResetRecording()
    {
        isRecording = false;
        recordingTimer = 0f;
        avgDB = float.MinValue;
        
        // Reset button state
        if (recordButton != null)
        {
            recordButton.interactable = true;
            recordButton.GetComponentInChildren<TMP_Text>().text = "Record";
        }
    }
}