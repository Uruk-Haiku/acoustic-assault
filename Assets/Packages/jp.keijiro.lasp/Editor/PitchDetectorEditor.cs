using UnityEngine;
using UnityEditor;

namespace Lasp.Editor
{
    //
    // Custom editor for SimplePitchDetector component
    //
    [CustomEditor(typeof(SimplePitchDetector))]
    [CanEditMultipleObjects]
    public sealed class SimplePitchDetectorEditor : UnityEditor.Editor
    {
        #region Private members

        DeviceSelector _deviceSelector;
        PropertyFinder _finder;

        // Serialized properties for organized display
        SerializedProperty _channel;
        SerializedProperty _resolution;
        SerializedProperty _minFrequency;
        SerializedProperty _maxFrequency;
        SerializedProperty _peakThreshold;
        SerializedProperty _peakNeighborhood;
        SerializedProperty _useHarmonicProduct;
        SerializedProperty _harmonicProductDepth;
        SerializedProperty _autoGain;
        SerializedProperty _gain;
        SerializedProperty _dynamicRange;
        SerializedProperty _smoothing;

        #endregion

        #region Editor implementation

        void OnEnable()
        {
            try
            {
                _deviceSelector = new DeviceSelector(serializedObject);
                _finder = new PropertyFinder(serializedObject);

                // Cache property references
                _channel = _finder["_channel"];
                _resolution = _finder["_resolution"];
                _minFrequency = _finder["_minFrequency"];
                _maxFrequency = _finder["_maxFrequency"];
                _peakThreshold = _finder["_peakThreshold"];
                _peakNeighborhood = _finder["_peakNeighborhood"];
                _useHarmonicProduct = _finder["_useHarmonicProduct"];
                _harmonicProductDepth = _finder["_harmonicProductDepth"];
                _autoGain = _finder["_autoGain"];
                _gain = _finder["_gain"];
                _dynamicRange = _finder["_dynamicRange"];
                _smoothing = _finder["_smoothing"];
            }
            catch
            {
                // Fallback for multi-object editing issues
                _deviceSelector = null;
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Device Selection Section
            EditorGUILayout.LabelField("Audio Input", EditorStyles.boldLabel);
            
            // Use DeviceSelector if available, otherwise fall back to default property fields
            if (_deviceSelector != null && targets.Length == 1)
            {
                _deviceSelector.ShowGUI();
            }
            else
            {
                // Fallback for multi-object editing or when DeviceSelector fails
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_useDefaultDevice"), new GUIContent("Default Device"));
                if (!serializedObject.FindProperty("_useDefaultDevice").boolValue || serializedObject.FindProperty("_useDefaultDevice").hasMultipleDifferentValues)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("_deviceID"), new GUIContent("Device ID"));
                }
            }
            
            EditorGUILayout.PropertyField(_channel);
            EditorGUILayout.Space();

            // FFT Settings Section
            EditorGUILayout.LabelField("FFT Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_resolution);
            EditorGUILayout.Space();

            // Pitch Detection Section
            EditorGUILayout.LabelField("Pitch Detection", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_minFrequency);
            EditorGUILayout.PropertyField(_maxFrequency);
            EditorGUILayout.PropertyField(_peakThreshold);
            EditorGUILayout.PropertyField(_peakNeighborhood);
            
            EditorGUILayout.PropertyField(_useHarmonicProduct);
            if (_useHarmonicProduct.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_harmonicProductDepth);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();

            // Audio Processing Section
            EditorGUILayout.LabelField("Audio Processing", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_autoGain);
            if (!_autoGain.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_gain);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.PropertyField(_dynamicRange);
            EditorGUILayout.PropertyField(_smoothing);
            EditorGUILayout.Space();

            // Runtime Information (only shown during play mode and single selection)
            if (Application.isPlaying && targets.Length == 1)
            {
                EditorGUILayout.LabelField("Runtime Information", EditorStyles.boldLabel);
                
                var detector = (SimplePitchDetector)target;
                
                EditorGUI.BeginDisabledGroup(true);
                
                EditorGUILayout.FloatField("Current Pitch (Hz)", detector.pitch);
                EditorGUILayout.FloatField("Raw Pitch (Hz)", detector.rawPitch);
                EditorGUILayout.FloatField("Confidence", detector.confidence);
                EditorGUILayout.FloatField("MIDI Note", detector.midiNote);
                EditorGUILayout.TextField("Note Name", detector.noteName);
                EditorGUILayout.FloatField("Current Gain (dB)", detector.currentGain);
                EditorGUILayout.FloatField("Peak Magnitude", detector.peakMagnitude);
                EditorGUILayout.FloatField("Loudness (dB)", detector.loudness);
                
                EditorGUI.EndDisabledGroup();
                
                // Auto-refresh during play mode
                if (Application.isPlaying)
                {
                    Repaint();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        #endregion
    }
}