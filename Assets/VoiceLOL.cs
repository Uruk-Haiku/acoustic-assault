using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class VoiceLOL : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI loudnessText;
    public TextMeshProUGUI pitchText;
    public TextMeshProUGUI timerText;
    public Button recordButton;

    [Header("Settings")]
    public int sampleWindow = 2048; // Increased window size
    public float detectionThreshold = 0f; // Minimum volume to consider as input

    private AudioClip microphoneClip;
    private bool isRecording = false;
    private string microphoneDevice;
    private float recordingTime = 0f;
    private const float MAX_RECORDING_TIME = 30f;

    //Key variables for pitch detection
    private AudioSource audioSource;
    private float smoothedPitch = 1;
    private float pitchSmoothingFactor = 1f; // Adjust for more/less smoothing

    void Start()
    {
        // Get default microphone device
        if (Microphone.devices.Length > 0)
        {
            microphoneDevice = Microphone.devices[0];
            Debug.Log("Using microphone: " + microphoneDevice);
            statusText.text = "Ready to record";
        }
        else
        {
            statusText.text = "No microphone found";
            Debug.Log("No Microphone found");
            recordButton.interactable = false;
        }

        // Set up button click event
        recordButton.onClick.AddListener(ToggleRecording);

        // Initialize UI
        loudnessText.text = "Loudness: 0 dB";
        pitchText.text = "Pitch: 0 Hz";
        timerText.text = "Time: 0.00s";
        isRecording = false;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        if (isRecording)
        {
            // Update recording timer
            recordingTime += Time.deltaTime;
            timerText.text = $"Time: {recordingTime:F2}s";

            // Analyze the audio input
            AnalyzeAudio();

            // Stop after 30 seconds
            if (recordingTime >= MAX_RECORDING_TIME)
            {
                StopRecording();
            }
        }
    }

    public void ToggleRecording()
    {
        if (isRecording)
        {
            StopRecording();
        }
        else
        {
            StartRecording();
        }
    }

    void StartRecording()
    {
        Debug.Log("Starting recording...");

        isRecording = true;
        recordingTime = 0f;
        smoothedPitch = 0; // Reset pitch on new recording

        // Start recording
        microphoneClip = Microphone.Start(microphoneDevice, true, 10, 44100);
        audioSource.clip = microphoneClip;
        audioSource.loop = true;
        audioSource.Play();
        statusText.text = "Recording...";
    }

    void StopRecording()
    {
        Debug.Log("Stopping recording...");

        isRecording = false;

        // Stop recording
        Microphone.End(microphoneDevice);
        audioSource.Stop();
        statusText.text = "Recording stopped";

    }

    void AnalyzeAudio()
    {
        // Get the current microphone position
        int micPosition = Microphone.GetPosition(microphoneDevice) - sampleWindow;
        if (micPosition < 0) micPosition += microphoneClip.samples;

        // Extract data from the audio clip
        float[] samples = new float[sampleWindow];
        microphoneClip.GetData(samples, micPosition);

        // Calculate loudness (RMS)
        float sum = 0;
        for (int i = 0; i < sampleWindow; i++)
        {
            sum += samples[i] * samples[i];
        }
        float rms = Mathf.Sqrt(sum / sampleWindow);
        float db = 20 * Mathf.Log10(rms / 0.1f); // Convert to dB

        // Only calculate pitch if there's significant input
        float rawPitch = 0;
        if (rms > detectionThreshold)
        {
            //rawPitch = EstimatePitch(samples, 44100);

            //BENS NEW IMPLEMENTATION
            float[] spectrumData = new float[2048];
            audioSource.GetSpectrumData(spectrumData, 0, FFTWindow.Rectangular);//BTODO confirm this is good

            // Simple peak detection
            float maxMagnitude = 0f;
            int maxIndex = 0;
            for (int i = 0; i < spectrumData.Length; i++)
            {
                if (spectrumData[i] > maxMagnitude)
                {
                    maxMagnitude = spectrumData[i];
                    maxIndex = i;
                }
            }
            rawPitch = maxIndex * (AudioSettings.outputSampleRate / 2f) / (spectrumData.Length * 2);


            /*
            // Apply smoothing
            if (rawPitch > 0)
            {
                if (smoothedPitch == 0)
                    smoothedPitch = rawPitch;
                else
                    smoothedPitch = pitchSmoothingFactor * rawPitch +
                                   (1 - pitchSmoothingFactor) * smoothedPitch;
            }
            */
            smoothedPitch = rawPitch;
        }

        // Update UI
        loudnessText.text = $"Loudness: {db:F2} dB";
        pitchText.text = $"Pitch: {smoothedPitch:F2} Hz";

        Debug.Log($"RMS: {rms}, First sample: {samples[0]}, Last sample: {samples[sampleWindow-1]}");
    }

    void OnDestroy()
    {
        Debug.Log("Killing microphone");
        // Clean up
        if (Microphone.IsRecording(microphoneDevice))
        {
            Microphone.End(microphoneDevice);
        }
    }

    public float GetCurrentPitch()
    {
        return smoothedPitch;
    }
}

