using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NoiseCeilingCalibration : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text levelText;
    public TMP_Text subTitleText;
    public Button recordButton;
    
    [Header("Recording Settings")]
    [SerializeField] private float recordTime = 3f; // Recording duration in seconds
    
    private float recordingTimer = 0f;
    private int playerID;
    private bool isRecording = false;
    private float maxDB = float.MinValue;
    private Lasp.SimplePitchDetector pitchDetector;
    
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
        // Get current loudness
        if (pitchDetector == null)
        {
            Debug.LogWarning($"Pitch detector not found for Player {playerID}");
            return;
        }

        float currentDB = pitchDetector.loudness;

        if (isRecording)
        {
            // Update recording timer with unscaled time
            recordingTimer += Time.unscaledDeltaTime;
            
            // Track maximum dB during recording
            if (currentDB > maxDB)
            {
                maxDB = currentDB;
            }
            
            // Update UI with recording status and max dB
            levelText.text = $"Recording...YELL!!!!!!({recordingTimer:F1}s/{recordTime:F1}s)";
            
            // Check if recording time is complete
            if (recordingTimer >= recordTime)
            {
                StopRecording();
            }
        }
        else
        {
            // Normal display mode
            levelText.text = $"Max Recorded: {(maxDB != float.MinValue ? maxDB.ToString("F1") : "N/A")}dB";
        }
    }
    
    public void StartRecording()
    {
        if (isRecording) return; // Prevent starting if already recording
        
        isRecording = true;
        recordingTimer = 0f;
        maxDB = float.MinValue;
        
        // Update button state
        if (recordButton != null)
        {
            recordButton.interactable = false;
            recordButton.GetComponentInChildren<TMP_Text>().text = "Recording...";
        }
        
        Debug.Log($"Started noise ceiling calibration for Player {playerID}");
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

        subTitleText.text = $"Recording complete! Max dB: {maxDB:F1}dB for Player {playerID}. Gain will be set so that this is at 0dB.";
        Debug.Log($"Recording complete! Max dB: {maxDB:F1}dB for Player {playerID}. Gain will be set so that this is at 0dB.");

        // Now based off maxDB, apply gain such that maxDB is at 0db.
        if (pitchDetector != null)
        {
            float desiredMaxDB = 0f;
            float gainAdjustment = desiredMaxDB - maxDB;
            pitchDetector.gain = gainAdjustment;
            Debug.Log($"Adjusted gain by {gainAdjustment:F1}dB. New gain: {pitchDetector.gain:F1}dB for Player {playerID}");
        }
    }
    
    private void ResetRecording()
    {
        isRecording = false;
        recordingTimer = 0f;
        maxDB = float.MinValue;
        
        // Reset button state
        if (recordButton != null)
        {
            recordButton.interactable = true;
            recordButton.GetComponentInChildren<TMP_Text>().text = "Record";
        }
    }
}