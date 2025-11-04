using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Lasp
{
    //
    // Pitch detector using YINFFT algorithm
    //
    [AddComponentMenu("LASP/Pitch Detector")]
    public sealed class PitchDetector : MonoBehaviour
    {
        #region Editor attributes and public properties

        // System default device switch
        [SerializeField] bool _useDefaultDevice = true;
        public bool useDefaultDevice
        {
            get => _useDefaultDevice;
            set => TrySelectDevice(null);
        }

        // Device ID to use
        [SerializeField] string _deviceID = "";
        public string deviceID
        {
            get => _deviceID;
            set => TrySelectDevice(value);
        }

        // FFT resolution for pitch detection
        [SerializeField] int resolution = 2048;
        // Pitch detection range
        [SerializeField, Range(50, 300)] float minFrequency = 50;
        [SerializeField, Range(200, 1500)] float maxFrequency = 1000;
        [SerializeField, Range(50, 300)] float minRange = 60; // C3 (in MIDI note number)
        [SerializeField, Range(200, 1500)] float maxRange = 72; // C4 in MIDI note number
        [SerializeField] public int pitchOffsetInSemitones = 0;

        // Peak detection parameters
        [SerializeField, Range(0.01f, 1f)] float threshold = 0.90f;

        // Manual input gain (only used when auto gain is off)
        [SerializeField, Range(-10, 120)] public float gain = 0;
        // Dynamic range in dB
        [SerializeField, Range(1, 120)] public float dynamicRange = 80;
        // Smoothing
        [SerializeField, Range(0f, 0.95f)] float smoothingStrength = 0.8f;
        [SerializeField, Range(0f, 1f)] float snapStrength = 0.5f;

        #endregion

        #region Runtime public properties

        // Current input gain (dB)
        public float level => Stream?.GetChannelLevel(0) ?? kSilence;
        public float gainedLevel => level + gain;
        public float rawPitch = 0;
        public float displayPitch = 0;

        public float offsetDisplayPitch => displayPitch * math.pow(2f, pitchOffsetInSemitones / 12f);
        public float confidence = 0;

        #endregion

        #region Private members
        // Silence: Locally defined noise floor level (dBFS)
        const float kSilence = -240;
        public void TrySelectDevice(string id)
        {
            if (_stream != null)
                throw new InvalidOperationException
                  ("Stream is already open");

            _useDefaultDevice = string.IsNullOrEmpty(id);
            _deviceID = id;
        }

        // Input stream object with local cache
        InputStream Stream
          => (_stream != null && _stream.IsValid) ? _stream : CacheStream();

        InputStream CacheStream()
          => (_stream = _useDefaultDevice ?
               AudioSystem.GetDefaultInputStream() :
               AudioSystem.GetInputStream(_deviceID));

        InputStream _stream;

        // FFT buffer object with lazy initialization
        FftBuffer Fft => _fft ?? (_fft = new FftBuffer(resolution * 2, Stream.SampleRate));
        FftBuffer _fft;

        #endregion

        void Update()
        {
            // Hardcoding to channel 0
            Fft?.Push(Stream.GetChannelDataSlice(0));
            Fft?.Analyze(-gain -dynamicRange, -gain);
            float currPitch;
            (currPitch, confidence) = Fft.DetectPitch(minFrequency, maxFrequency);
            if (confidence < threshold)
            {
                rawPitch = 0; // We need this for calibration.
                // Display Pitch doesn't change
                return;
            }
            if (rawPitch == 0)
            {
                rawPitch = currPitch;
                return;
            }
            float ratio = currPitch / rawPitch ;
            // Common octave error: detecting 2x or 0.5x the frequency
            if (ratio > 1.8f && ratio < 2.2f)
            {
                currPitch = currPitch / 2f; // Octave too high
            }
            else if (ratio < 0.55f && ratio > 0.45f)
            {
                currPitch = currPitch * 2f; // Octave too low
            }
            ratio = currPitch / rawPitch;
            rawPitch = currPitch;
            // Whole note change or more no smoothing
            if (ratio > 1.122)
            {
                displayPitch = rawPitch;
            }
            else
            {
                displayPitch = displayPitch * smoothingStrength + rawPitch * (1f - smoothingStrength);
            }
            // Find nearest note
            OctaveNote note = OctaveNote.FromFrequency(displayPitch);
            // Only snap if we're close (within ±35 cents)
            if (math.abs(note.cent) < 35f)
            {
                float snapAmount = (1f - math.abs(note.cent) / 35f) * snapStrength;
                displayPitch = math.lerp(displayPitch, note.noteFrequency, snapAmount);
            }
        }
        void OnDisable()
        {
            _fft?.Dispose();
            _fft = null;
        }
    }
}