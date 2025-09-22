using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Lasp
{
    //
    // Simple pitch detector using peak detection in frequency spectrum
    //
    [AddComponentMenu("LASP/Simple Pitch Detector")]
    public sealed class SimplePitchDetector : MonoBehaviour
    {
        #region Editor attributes and public properties

        // System default device switch
        [SerializeField] bool _useDefaultDevice = true;
        public bool useDefaultDevice
          { get => _useDefaultDevice;
            set => TrySelectDevice(null); }

        // Device ID to use
        [SerializeField] string _deviceID = "";
        public string deviceID
          { get => _deviceID;
            set => TrySelectDevice(value); }

        // Channel Selection
        [SerializeField, Range(0, 15)] int _channel = 0;
        public int channel
          { get => _channel;
            set => _channel = value; }

        // FFT resolution for pitch detection
        [SerializeField] int _resolution = 2048;
        public int resolution
          { get => _resolution;
            set => _resolution = ValidateResolution(value); }

        // Pitch detection range
        [SerializeField, Range(50, 300)] float _minFrequency = 80;
        public float minFrequency
          { get => _minFrequency;
            set => _minFrequency = value; }

        [SerializeField, Range(200, 1500)] float _maxFrequency = 600;
        public float maxFrequency
          { get => _maxFrequency;
            set => _maxFrequency = value; }

        // Peak detection parameters
        [SerializeField, Range(0.01f, 1f)] float _peakThreshold = 0.4f;
        public float peakThreshold
          { get => _peakThreshold;
            set => _peakThreshold = value; }

        [SerializeField, Range(1, 10)] int _peakNeighborhood = 3;
        public int peakNeighborhood
          { get => _peakNeighborhood;
            set => _peakNeighborhood = value; }

        [SerializeField] bool _useHarmonicProduct = false;
        public bool useHarmonicProduct
          { get => _useHarmonicProduct;
            set => _useHarmonicProduct = value; }

        [SerializeField, Range(2, 5)] int _harmonicProductDepth = 3;
        public int harmonicProductDepth
          { get => _harmonicProductDepth;
            set => _harmonicProductDepth = value; }

        // Auto gain control switch
        [SerializeField] bool _autoGain = false;
        public bool autoGain
          { get => _autoGain;
            set => _autoGain = value; }

        // Manual input gain (only used when auto gain is off)
        [SerializeField, Range(-10, 120)] float _gain = 0;
        public float gain
          { get => _gain;
            set => _gain = value; }

        // Dynamic range in dB
        [SerializeField, Range(1, 120)] float _dynamicRange = 80;
        public float dynamicRange
          { get => _dynamicRange;
            set => _dynamicRange = value; }

        // Smoothing
        [SerializeField, Range(0f, 0.95f)] float _smoothing = 0.5f;
        public float smoothing
          { get => _smoothing;
            set => _smoothing = value; }

        #endregion

        #region Attribute validators

        static int ValidateResolution(int x)
        {
            if (x > 0 && (x & (x - 1)) == 0) return x;
            Debug.LogError("FFT resolution must be a power of 2.");
            return 1 << (int)math.max(1, math.round(math.log2(x)));
        }

        #endregion

        #region Runtime public properties

        // Current input gain (dB)
        public float currentGain => _autoGain ? -_head : _gain;

        // Current detected pitch in Hz (0 if no pitch detected)
        public float pitch => _smoothedPitch;

        // Raw detected pitch without smoothing
        public float rawPitch => _currentPitch;

        // Confidence/strength of the current pitch estimate (0-1)
        public float confidence => _currentConfidence;

        // Get pitch in MIDI note number (69 = A4 = 440Hz)
        public float midiNote => _smoothedPitch > 0 ? 69 + 12 * math.log2(_smoothedPitch / 440f) : 0;

        // Get current input loudness (dBFS)
        public float loudness => Stream?.GetChannelLevel(_channel) ?? kSilence;

        // Get pitch as musical note name
        public string noteName
        {
            get
            {
                if (_smoothedPitch <= 0) return "---";

                float midi = midiNote;
                int noteNumber = Mathf.RoundToInt(midi);
                float cents = (midi - noteNumber) * 100;

                string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
                int octave = noteNumber / 12 - 1;
                int noteIndex = noteNumber % 12;

                string centString = cents >= 0 ? $"+{cents:F0}" : $"{cents:F0}";
                return $"{noteNames[noteIndex]}{octave} {centString}¢";
            }
        }

        // Reset the auto gain state
        public void ResetAutoGain() => _head = kSilence;

        // Get the peak magnitude for debugging
        public float peakMagnitude => _peakMagnitude;

        #endregion

        #region Private members

        // Silence: Locally defined noise floor level (dBFS)
        const float kSilence = -240;

        // Nominal level of auto gain (recent maximum level)
        float _head = kSilence;

        // Sample rate (assumed)
        const float kSampleRate = 48000f;

        // Current detection results
        float _currentPitch;
        float _smoothedPitch;
        float _currentConfidence;
        float _peakMagnitude;

        // Harmonic product spectrum buffer
        NativeArray<float> _hpsBuffer;
        bool _hpsInitialized;

        // Check the status and try selecting the device.
        void TrySelectDevice(string id)
        {
            if (_stream != null)
                throw new System.InvalidOperationException
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
        FftBuffer Fft => _fft ?? (_fft = new FftBuffer(_resolution * 2));
        FftBuffer _fft;

        #endregion

        #region Pitch analysis

        void AnalyzePitch()
        {
            if (_fft == null) return;

            var spectrum = _fft.Spectrum;
            float binResolution = kSampleRate / (_resolution * 2);
            
            // Calculate bin range for our frequency range
            int minBin = Mathf.Max(1, Mathf.FloorToInt(_minFrequency / binResolution));
            int maxBin = Mathf.Min(spectrum.Length - 1, Mathf.CeilToInt(_maxFrequency / binResolution));
            
            float detectedFreq = 0;
            float peakValue = 0;
            
            if (_useHarmonicProduct)
            {
                // Harmonic Product Spectrum method
                detectedFreq = DetectPitchHPS(spectrum, binResolution, minBin, maxBin, out peakValue);
            }
            else
            {
                // Simple peak detection
                detectedFreq = DetectPitchPeak(spectrum, binResolution, minBin, maxBin, out peakValue);
            }
            
            _peakMagnitude = peakValue;
            
            // Update pitch if peak is above threshold
            if (peakValue > _peakThreshold)
            {
                _currentPitch = detectedFreq;
                _currentConfidence = math.saturate(peakValue);
                
                // Apply smoothing
                if (_smoothedPitch > 0)
                {
                    // Only smooth if we're within a reasonable range (avoid octave jumps)
                    float ratio = detectedFreq / _smoothedPitch;
                    if (ratio > 0.8f && ratio < 1.2f)
                    {
                        _smoothedPitch = math.lerp(_smoothedPitch, detectedFreq, 1f - _smoothing);
                    }
                    else
                    {
                        // Jump to new pitch if it's too different
                        _smoothedPitch = detectedFreq;
                    }
                }
                else
                {
                    _smoothedPitch = detectedFreq;
                }
            }
            else
            {
                // _currentPitch = 0;
                _currentConfidence = 0;
                // Decay smoothed pitch
                // _smoothedPitch = math.lerp(_smoothedPitch, 0, Time.deltaTime * 5f);
            }
        }

        float DetectPitchPeak(NativeArray<float> spectrum, float binResolution, int minBin, int maxBin, out float peakValue)
        {
            int peakBin = -1;
            peakValue = 0;
            
            // Find the highest peak in the frequency range
            for (int i = minBin; i <= maxBin; i++)
            {
                float magnitude = spectrum[i];
                
                // Check if this is a local maximum (peak)
                bool isPeak = true;
                for (int j = 1; j <= _peakNeighborhood; j++)
                {
                    if (i - j >= 0 && spectrum[i - j] > magnitude)
                    {
                        isPeak = false;
                        break;
                    }
                    if (i + j < spectrum.Length && spectrum[i + j] > magnitude)
                    {
                        isPeak = false;
                        break;
                    }
                }
                
                // Track the highest peak
                if (isPeak && magnitude > peakValue)
                {
                    peakValue = magnitude;
                    peakBin = i;
                }
            }
            
            if (peakBin < 0) return 0;
            
            // Parabolic interpolation for sub-bin accuracy
            float freq = peakBin * binResolution;
            
            if (peakBin > 0 && peakBin < spectrum.Length - 1)
            {
                float alpha = spectrum[peakBin - 1];
                float beta = spectrum[peakBin];
                float gamma = spectrum[peakBin + 1];
                
                if (beta > 0 && (alpha - 2 * beta + gamma) != 0)
                {
                    float p = 0.5f * (alpha - gamma) / (alpha - 2 * beta + gamma);
                    freq = (peakBin + p) * binResolution;
                }
            }
            
            return freq;
        }

        float DetectPitchHPS(NativeArray<float> spectrum, float binResolution, int minBin, int maxBin, out float peakValue)
        {
            // Initialize HPS buffer if needed
            if (!_hpsInitialized || _hpsBuffer.Length != spectrum.Length)
            {
                if (_hpsBuffer.IsCreated) _hpsBuffer.Dispose();
                _hpsBuffer = new NativeArray<float>(spectrum.Length, Allocator.Persistent);
                _hpsInitialized = true;
            }
            
            // Copy original spectrum
            NativeArray<float>.Copy(spectrum, _hpsBuffer);
            
            // Multiply downsampled versions (Harmonic Product)
            for (int h = 2; h <= _harmonicProductDepth; h++)
            {
                for (int i = minBin; i <= maxBin / h; i++)
                {
                    int harmonicBin = i * h;
                    if (harmonicBin < spectrum.Length)
                    {
                        _hpsBuffer[i] *= spectrum[harmonicBin];
                    }
                }
            }
            
            // Find peak in HPS result
            int peakBin = -1;
            peakValue = 0;
            
            for (int i = minBin; i <= maxBin / _harmonicProductDepth; i++)
            {
                float magnitude = _hpsBuffer[i];
                
                if (magnitude > peakValue)
                {
                    peakValue = magnitude;
                    peakBin = i;
                }
            }
            
            if (peakBin < 0) return 0;
            
            // Convert to frequency with interpolation
            float freq = peakBin * binResolution;
            
            if (peakBin > 0 && peakBin < _hpsBuffer.Length - 1)
            {
                float alpha = _hpsBuffer[peakBin - 1];
                float beta = _hpsBuffer[peakBin];
                float gamma = _hpsBuffer[peakBin + 1];
                
                if (beta > 0 && (alpha - 2 * beta + gamma) != 0)
                {
                    float p = 0.5f * (alpha - gamma) / (alpha - 2 * beta + gamma);
                    freq = (peakBin + p) * binResolution;
                }
            }
            
            // Normalize peak value for HPS (it gets very small due to multiplication)
            peakValue = math.pow(peakValue, 1f / _harmonicProductDepth);
            
            return freq;
        }

        #endregion

        #region MonoBehaviour implementation

        void OnDisable()
        {
            _fft?.Dispose();
            _fft = null;

            if (_hpsBuffer.IsCreated) _hpsBuffer.Dispose();
            _hpsInitialized = false;
        }

        void Update()
        {
            var input = Stream?.GetChannelLevel(_channel) ?? kSilence;
            var dt = Time.deltaTime;

            // Auto gain control
            if (_autoGain)
            {
                // Slowly return to the noise floor.
                const float kDecaySpeed = 0.6f;
                _head -= kDecaySpeed * dt;
                _head = Mathf.Max(_head, kSilence + _dynamicRange);

                // Pull up by input with a small headroom.
                var room = _dynamicRange * 0.05f;
                _head = Mathf.Clamp(input - room, _head, 0);
            }

            // FFT
            Fft?.Push(Stream.GetChannelDataSlice(_channel));
            Fft?.Analyze(-currentGain - _dynamicRange, -currentGain);

            // Pitch detection
            AnalyzePitch();
        }

        #endregion
    }
}