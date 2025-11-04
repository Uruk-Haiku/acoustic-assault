using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Lasp;

public class PitchFloorCalibration : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text helperText;
    public TMP_Text noteText;
    public Button recordButton;
    [SerializeField] Image _circularProgressBar;
    [SerializeField] Image _circularCentOffSetBar;

    [Header("Recording Settings")]
    [SerializeField] private float recordTime = 1.2f; // Recording duration in seconds

    [Header("Visual Settings")]
    [SerializeField] Color _progressColor = Color.green;
    [SerializeField] Color _incompleteColor = Color.gray;


    private float recordingTimer = 0f;
    private int playerID;
    private bool isRecording = false;
    private int calibratedMidiNum = 0;
    private PitchDetector pitchDetector;

    void Start()
    {
        // Set circular progress to radial fill
        _circularProgressBar.type = Image.Type.Filled;
        _circularProgressBar.fillMethod = Image.FillMethod.Radial360;
        _circularProgressBar.fillOrigin = (int)Image.Origin360.Top;
        _circularProgressBar.fillAmount = 0f;
        _circularProgressBar.color = _incompleteColor;

        // Set cent offset bar to vertical fill
        _circularCentOffSetBar.type = Image.Type.Filled;
        _circularCentOffSetBar.fillMethod = Image.FillMethod.Vertical;
        _circularCentOffSetBar.fillOrigin = (int)Image.OriginVertical.Bottom;
        _circularCentOffSetBar.fillAmount = 0f;
        _circularCentOffSetBar.color = Color.teal;
    }

    void OnEnable()
    {
        // Get current player
        SettingsPanel settingsPanel = GetComponentInParent<SettingsPanel>();
        playerID = settingsPanel.currentPlayer;
        pitchDetector = GameManager.GetPitchDetection(playerID);
        ResetRecording();
    }

    // Update is called once per frame
    void Update()
    {

        if (!isRecording)
        {
            noteText.text = calibratedMidiNum != 0 ? $"{OctaveNote.FromMidiNum(calibratedMidiNum)}" : "N/A";
            return;
        }

        if (pitchDetector == null)
        {
            Debug.LogWarning($"Pitch detector not found for Player {playerID}");
            return;
        }

        int currMidiNum = OctaveNote.MidiNumFromFrequency(pitchDetector.rawPitch);

        helperText.text = $"Recording...Please hold your pitch)";
        noteText.text = OctaveNote.FromMidiNum(currMidiNum).ToString();

        if (currMidiNum != calibratedMidiNum)
        {
            calibratedMidiNum = currMidiNum;
            recordingTimer = 0f;
            _circularProgressBar.color = _progressColor;
            return;

        }

        // If pitch is silent (midiNum output is 0) then don't progress
        if (currMidiNum == 0)
        {
            return;
        }

        recordingTimer += Time.unscaledDeltaTime;
        float progress = Mathf.Clamp01(recordingTimer / recordTime);
        _circularProgressBar.fillAmount = progress;
        _circularCentOffSetBar.fillAmount = (OctaveNote.FromFrequency(pitchDetector.rawPitch).cent / 50f) + 0.5f;

        if (recordingTimer >= recordTime)
        {
            StopRecording();
        }
    }
    
    public void StartRecording()
    {
        if (isRecording) return; // Prevent starting if already recording
        
        isRecording = true;
        recordingTimer = 0f;
        calibratedMidiNum = 0;

        _circularProgressBar.fillAmount = 0f;
        _circularProgressBar.color = _incompleteColor;
        _circularCentOffSetBar.fillAmount = 0.5f;

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

        // Show that progress is done
         _circularProgressBar.color = Color.yellow;
        // Set min vocal range (in MIDI note number) based on calibrated note
        
        // Update button state
        if (recordButton != null)
        {
            recordButton.interactable = true;
            recordButton.GetComponentInChildren<TMP_Text>().text = "Record";
        }

        Debug.Log($"Recording complete! Pitch Floor: {OctaveNote.FromMidiNum(calibratedMidiNum)} for Player {playerID}");
        helperText.text = $"Recording complete! Pitch Floor: {OctaveNote.FromMidiNum(calibratedMidiNum)}";
    }
    
    private void ResetRecording()
    {
        isRecording = false;
        recordingTimer = 0f;
        calibratedMidiNum = 0;
        _circularProgressBar.fillAmount = 0f;
        
        // Reset button state
        if (recordButton != null)
        {
            recordButton.interactable = true;
            recordButton.GetComponentInChildren<TMP_Text>().text = "Record";
        }
    }
}