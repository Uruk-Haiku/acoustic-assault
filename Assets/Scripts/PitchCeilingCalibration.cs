using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Lasp;

public class PitchCeilingCalibration : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text helperText;
    public TMP_Text noteText;
    public Button recordButton;
    [SerializeField] Image _circularProgressBar;
    [SerializeField] Image _circularCentOffSetBar;

    [Header("Recording Settings")]
    [SerializeField] private float recordTime = 1.5f; // Recording duration in seconds

    [Header("Visual Settings")]
    [SerializeField] Color _progressColor = Color.green;
    [SerializeField] Color _incompleteColor = Color.gray;


    private float recordingTimer = 0f;
    private int playerID;
    private bool isRecording = false;
    private int calibratedMidiNum = 0;
    private Lasp.SimplePitchDetector pitchDetector;

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

        int currMidiNum = OctaveNote.MidiNumFromFrequency(pitchDetector.pitch);

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
        _circularCentOffSetBar.fillAmount = (OctaveNote.FromFrequency(pitchDetector.pitch).cent / 50f) + 0.5f;

        if (recordingTimer >= recordTime)
        {
            StopRecordingIfRangeIsValid();
        }
    }
    
    public void StartRecording()
    {
        if (isRecording) return; // Prevent starting if already recording
        pitchDetector.isPitchZeroWhenNone = true;
        
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

        Debug.Log($"Started noise ceiling calibration for Player {playerID}");
    }
    
    private void StopRecordingIfRangeIsValid()
    {
        // Show that progress is done
        // Set min vocal range (in MIDI note number) based on calibrated note
        pitchDetector.maxRange = calibratedMidiNum;
        // Check if there is at least one octave of range (Not just 12 semitones, but if C(N) to B(N) is included)
        // Update button state
        if (recordButton != null)
        {
            recordButton.interactable = true;
            recordButton.GetComponentInChildren<TMP_Text>().text = "Record";
        }


        isRecording = false;
        pitchDetector.isPitchZeroWhenNone = true;

        if (pitchDetector.octaveRange == -1)
        {
            _circularProgressBar.color = Color.red;
            Debug.LogWarning($"Pitch ceiling calibration failed! Vocal range must cover at least one octave for Player {playerID}");
            helperText.text = $"Calibration failed! Vocal range must cover at least one octave. Please try again.";
        }
        else
        {

            _circularProgressBar.color = Color.yellow;
            Debug.Log($"Pitch ceiling calibration succeeded! Vocal range is {pitchDetector.octaveRange} octaves for Player {playerID}");
            helperText.text = $"Calibration succeeded! Vocal range is octave #3 {pitchDetector.octaveRange}";
        }
    }
    
    private void ResetRecording()
    {
        isRecording = false;
        pitchDetector.isPitchZeroWhenNone = false;
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