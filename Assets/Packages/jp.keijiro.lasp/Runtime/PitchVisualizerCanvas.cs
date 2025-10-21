// using UnityEngine;
// using UnityEngine.UI;
// using Unity.Mathematics;

// namespace Lasp
// {
//     [AddComponentMenu("LASP/Pitch Visualizer Canvas")]
//     [RequireComponent(typeof(SimplePitchDetector))]
//     public sealed class PitchVisualizerCanvas : MonoBehaviour
//     {
//         [Header("UI References")]
//         [SerializeField] RectTransform _visualizerPanel;
//         [SerializeField] RectTransform _pitchBar;
//         [SerializeField] Text _pitchText;
//         [SerializeField] Text _noteText;
//         [SerializeField] Slider _confidenceSlider;
//         [SerializeField] GameObject _gridLinePrefab;

//         [Header("Settings")]
//         [SerializeField] bool _logarithmicScale = true;
//         [SerializeField, Range(0f, 1f)] float _smoothing = 0.15f;
//         [SerializeField] Gradient _confidenceGradient;

//         [SerializeField] SimplePitchDetector _pitchDetector;
//         float _currentPosition;
//         float _minFreq;
//         float _maxFreq;

//         void Start()
//         {
//             // _pitchDetector = GetComponent<SwipePitchDetector>();
//             if (_pitchDetector == null)
//             {
//                 Debug.LogError("PitchVisualizerCanvas: No PitchDetector component found. Please attach a PitchDetector component.");
//                 enabled = false;
//                 return;
//             }

//             if (_visualizerPanel == null || _pitchBar == null)
//             {
//                 Debug.LogWarning("PitchVisualizerCanvas: Missing UI references. Please assign them in the inspector.");
//                 return;
//             }

//             UpdateFrequencyRange();
//             GenerateGridLines();
//         }

//         void Update()
//         {
//             if (_pitchDetector == null || _pitchBar == null) return;

//             UpdateFrequencyRange();

//             float pitch = _pitchDetector.pitch;
//             float confidence = _pitchDetector.confidence;

//             if (pitch > 0 && confidence > 0)
//             {
//                 // Calculate target position
//                 float normalizedPos = FrequencyToNormalizedPosition(pitch);

//                 // Smooth transition
//                 _currentPosition = Mathf.Lerp(_currentPosition, normalizedPos,
//                     Time.deltaTime * (1f - _smoothing) * 20f);

//                 // Update bar position
//                 Vector2 anchoredPos = _pitchBar.anchoredPosition;
//                 anchoredPos.y = _visualizerPanel.rect.height * (_currentPosition - 0.5f);
//                 _pitchBar.anchoredPosition = anchoredPos;

//                 // Update bar visibility
//                 _pitchBar.gameObject.SetActive(true);

//                 // Update bar color based on confidence
//                 if (_confidenceGradient != null)
//                 {
//                     var image = _pitchBar.GetComponent<Image>();
//                     if (image != null)
//                     {
//                         Color color = _confidenceGradient.Evaluate(confidence);
//                         color.a = 0.3f + confidence * 0.7f;
//                         image.color = color;
//                     }
//                 }
//             }
//             else
//             {
//                 _pitchBar.gameObject.SetActive(false);
//             }

//             // Update text displays
//             if (_pitchText != null)
//             {
//                 _pitchText.text = pitch > 0 ? $"{pitch:F1} Hz" : "---";
//             }

//             if (_noteText != null)
//             {
//                 _noteText.text = _pitchDetector.noteName;
//             }

//             if (_confidenceSlider != null)
//             {
//                 _confidenceSlider.value = confidence;

//                 // Optional: Update slider fill color based on confidence
//                 if (_confidenceGradient != null)
//                 {
//                     var fillRect = _confidenceSlider.fillRect;
//                     if (fillRect != null)
//                     {
//                         var fillImage = fillRect.GetComponent<Image>();
//                         if (fillImage != null)
//                         {
//                             fillImage.color = _confidenceGradient.Evaluate(confidence);
//                         }
//                     }
//                 }
//             }
//         }

//         void UpdateFrequencyRange()
//         {
//             if (math.abs(_minFreq - _pitchDetector.minFrequency) > 0.01f ||
//                 math.abs(_maxFreq - _pitchDetector.maxFrequency) > 0.01f)
//             {
//                 _minFreq = _pitchDetector.minFrequency;
//                 _maxFreq = _pitchDetector.maxFrequency;
//             }
//         }

//         float FrequencyToNormalizedPosition(float freq)
//         {
//             if (_logarithmicScale)
//             {
//                 if (freq <= 0) return 0;
//                 float logFreq = math.log(freq);
//                 float logMin = math.log(_minFreq);
//                 float logMax = math.log(_maxFreq);
//                 return math.saturate((logFreq - logMin) / (logMax - logMin));
//             }
//             else
//             {
//                 return math.saturate((freq - _minFreq) / (_maxFreq - _minFreq));
//             }
//         }

//         void GenerateGridLines()
//         {
//             if (_gridLinePrefab == null || _visualizerPanel == null) return;

//             // Clear existing grid lines
//             foreach (Transform child in _visualizerPanel)
//             {
//                 if (child != _pitchBar && child.name.Contains("GridLine"))
//                 {
//                     Destroy(child.gameObject);
//                 }
//             }

//             // Generate new grid lines for musical notes
//             for (float freq = GetNearestNoteFreq(_minFreq); freq <= _maxFreq; freq = GetNextNoteFreq(freq))
//             {
//                 float normalizedPos = FrequencyToNormalizedPosition(freq);

//                 GameObject gridLine = Instantiate(_gridLinePrefab, _visualizerPanel);
//                 gridLine.name = $"GridLine_{freq:F0}Hz";

//                 RectTransform rect = gridLine.GetComponent<RectTransform>();
//                 if (rect != null)
//                 {
//                     Vector2 anchoredPos = rect.anchoredPosition;
//                     anchoredPos.y = _visualizerPanel.rect.height * (normalizedPos - 0.5f);
//                     rect.anchoredPosition = anchoredPos;

//                     // Make octave lines more visible
//                     int midiNote = Mathf.RoundToInt(69 + 12 * math.log2(freq / 440f));
//                     bool isOctave = (midiNote % 12) == 0;

//                     Image lineImage = gridLine.GetComponent<Image>();
//                     if (lineImage != null)
//                     {
//                         Color color = lineImage.color;
//                         color.a = isOctave ? 0.4f : 0.2f;
//                         lineImage.color = color;
//                     }
//                 }
//             }
//         }

//         float GetNearestNoteFreq(float freq)
//         {
//             float midi = 69 + 12 * math.log2(freq / 440f);
//             int nearestMidi = Mathf.RoundToInt(midi);
//             return 440f * math.pow(2, (nearestMidi - 69) / 12f);
//         }

//         float GetNextNoteFreq(float freq)
//         {
//             return freq * math.pow(2, 1f / 12f);
//         }
//     }
// }