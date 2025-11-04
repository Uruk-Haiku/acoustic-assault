using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;
using UnityEngine;

namespace Lasp
{
    //
    // SWIPE pitch detector component for robust pitch detection
    //
    [AddComponentMenu("LASP/SWIPE Pitch Detector")]
    public sealed class SwipePitchDetector : MonoBehaviour
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
        [SerializeField] int _resolution = 1024;
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

        // Algorithm parameters
        [SerializeField, Range(5, 30)] int _harmonicCount = 20;
        public int harmonicCount
          { get => _harmonicCount;
            set => _harmonicCount = value; }

        [SerializeField, Range(20, 200)] int _candidateResolution = 100;
        public int candidateResolution
          { get => _candidateResolution;
            set => _candidateResolution = value; }

        [SerializeField, Range(0.05f, 0.5f)] float _strengthThreshold = 0.15f;
        public float strengthThreshold
          { get => _strengthThreshold;
            set => _strengthThreshold = value; }

        [SerializeField] bool _usePrimeHarmonics = true;
        public bool usePrimeHarmonics
          { get => _usePrimeHarmonics;
            set => _usePrimeHarmonics = value; }

        // Auto gain control switch
        [SerializeField] bool _autoGain = true;
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
        public float pitch => _currentPitch;

        // Confidence/strength of the current pitch estimate (0-1)
        public float confidence => _currentConfidence;

        // Get pitch in MIDI note number (69 = A4 = 440Hz)
        public float midiNote => _currentPitch > 0 ? 69 + 12 * math.log2(_currentPitch / 440f) : 0;

        // Get pitch as musical note name
        public string noteName
        {
            get
            {
                if (_currentPitch <= 0) return "---";
                
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
        float _currentConfidence;

        // SWIPE algorithm arrays
        NativeArray<float> _candidateFrequencies;
        NativeArray<float> _pitchStrengths;
        NativeArray<float> _harmonicWeights;
        NativeArray<int> _primeNumbers;
        bool _arraysInitialized;

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
        FftBuffer Fft => _fft ?? (_fft = new FftBuffer(_resolution * 2, Stream.SampleRate));
        FftBuffer _fft;

        #endregion

        #region Array initialization

        void EnsureArraysInitialized()
        {
            if (_arraysInitialized) return;

            // Initialize candidate frequencies (log spacing)
            _candidateFrequencies = new NativeArray<float>(_candidateResolution, Allocator.Persistent);
            float logMin = math.log(_minFrequency);
            float logMax = math.log(_maxFrequency);
            float logStep = (logMax - logMin) / (_candidateResolution - 1);
            
            for (int i = 0; i < _candidateResolution; i++)
            {
                _candidateFrequencies[i] = math.exp(logMin + i * logStep);
            }

            // Initialize harmonic weights (sqrt decay)
            _harmonicWeights = new NativeArray<float>(_harmonicCount, Allocator.Persistent);
            for (int i = 0; i < _harmonicCount; i++)
            {
                _harmonicWeights[i] = math.sqrt(1f / (i + 1));
            }

            // Initialize prime numbers
            var primes = new int[] { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71 };
            int primeCount = math.min(primes.Length, _harmonicCount);
            _primeNumbers = new NativeArray<int>(primeCount, Allocator.Persistent);
            for (int i = 0; i < primeCount; i++)
            {
                _primeNumbers[i] = primes[i];
            }

            // Initialize pitch strengths array
            _pitchStrengths = new NativeArray<float>(_candidateResolution, Allocator.Persistent);

            _arraysInitialized = true;
        }

        void DisposeArrays()
        {
            if (!_arraysInitialized) return;

            if (_candidateFrequencies.IsCreated) _candidateFrequencies.Dispose();
            if (_pitchStrengths.IsCreated) _pitchStrengths.Dispose();
            if (_harmonicWeights.IsCreated) _harmonicWeights.Dispose();
            if (_primeNumbers.IsCreated) _primeNumbers.Dispose();

            _arraysInitialized = false;
        }

        #endregion

        #region Pitch analysis

        void AnalyzePitch()
        {
            if (_fft == null) return;

            EnsureArraysInitialized();

            var spectrum = _fft.Spectrum;
            float binResolution = kSampleRate / (_resolution * 2);

            // Calculate pitch strength for each candidate using Burst job
            var job = new PitchStrengthJob
            {
                spectrum = spectrum,
                candidateFreqs = _candidateFrequencies,
                harmonicWeights = _harmonicWeights,
                primeNumbers = _primeNumbers,
                pitchStrengths = _pitchStrengths,
                binResolution = binResolution,
                spectrumSize = spectrum.Length,
                usePrimeHarmonics = _usePrimeHarmonics
            };
            
            job.Schedule(_candidateResolution, 32).Complete();
            
            // Find the maximum strength and corresponding frequency
            float maxStrength = 0;
            int maxIndex = -1;
            
            for (int i = 0; i < _candidateResolution; i++)
            {
                if (_pitchStrengths[i] > maxStrength)
                {
                    maxStrength = _pitchStrengths[i];
                    maxIndex = i;
                }
            }
            
            // Apply threshold and parabolic interpolation for sub-bin accuracy
            if (maxStrength > _strengthThreshold && maxIndex >= 0)
            {
                float interpolatedFreq = _candidateFrequencies[maxIndex];
                
                // Parabolic interpolation if not at boundaries
                if (maxIndex > 0 && maxIndex < _candidateResolution - 1)
                {
                    float alpha = _pitchStrengths[maxIndex - 1];
                    float beta = _pitchStrengths[maxIndex];
                    float gamma = _pitchStrengths[maxIndex + 1];
                    
                    float p = 0.5f * (alpha - gamma) / (alpha - 2 * beta + gamma);
                    
                    // Interpolate frequency (log scale)
                    float logFreq = math.log(interpolatedFreq);
                    float logStep = math.log(_candidateFrequencies[1]) - math.log(_candidateFrequencies[0]);
                    interpolatedFreq = math.exp(logFreq + p * logStep);
                }
                
                _currentPitch = interpolatedFreq;
                _currentConfidence = math.saturate(maxStrength);
            }
            else
            {
                _currentPitch = 0;
                _currentConfidence = 0;
            }
        }

        #endregion

        #region MonoBehaviour implementation

        void OnDisable()
        {
            _fft?.Dispose();
            _fft = null;

            DisposeArrays();
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

    #region Burst Job (separate from main class to avoid ambiguity)

    [BurstCompile(CompileSynchronously = true)]
    struct PitchStrengthJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float> spectrum;
        [ReadOnly] public NativeArray<float> candidateFreqs;
        [ReadOnly] public NativeArray<float> harmonicWeights;
        [ReadOnly] public NativeArray<int> primeNumbers;
        [WriteOnly] public NativeArray<float> pitchStrengths;
        
        [ReadOnly] public float binResolution;
        [ReadOnly] public int spectrumSize;
        [ReadOnly] public bool usePrimeHarmonics;

        public void Execute(int candidateIndex)
        {
            float fundamentalFreq = candidateFreqs[candidateIndex];
            float strength = 0;
            float totalWeight = 0;
            
            int harmonicCount = usePrimeHarmonics ? 
                primeNumbers.Length : harmonicWeights.Length;
            
            for (int h = 0; h < harmonicCount; h++)
            {
                // Get harmonic number (either sequential or prime)
                int harmonicNumber = usePrimeHarmonics ? primeNumbers[h] : (h + 1);
                
                // Calculate the frequency bin for this harmonic
                float harmonicFreq = fundamentalFreq * harmonicNumber;
                float binFloat = harmonicFreq / binResolution;
                
                // Skip if outside spectrum range
                if (binFloat >= spectrumSize - 1) break;
                
                // Linear interpolation between bins for sub-bin accuracy
                int binLow = (int)binFloat;
                int binHigh = math.min(binLow + 1, spectrumSize - 1);
                float fraction = binFloat - binLow;
                
                float magnitude = math.lerp(spectrum[binLow], spectrum[binHigh], fraction);
                
                // Apply harmonic weight
                float weight = harmonicWeights[math.min(h, harmonicWeights.Length - 1)];
                
                // Accumulate weighted magnitude
                strength += magnitude * weight;
                totalWeight += weight;
            }
            
            // Normalize by total weight
            pitchStrengths[candidateIndex] = totalWeight > 0 ? strength / totalWeight : 0;
        }
    }

    #endregion
}