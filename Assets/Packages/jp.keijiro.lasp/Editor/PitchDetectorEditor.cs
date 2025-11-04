using UnityEngine;
using UnityEditor;

namespace Lasp.Editor
{
    //
    // Custom editor for PitchDetector component
    //
    [CustomEditor(typeof(PitchDetector))]
    [CanEditMultipleObjects]
    public sealed class PitchDetectorEditor : UnityEditor.Editor
    {
        #region Private members

        DeviceSelector _deviceSelector;
        PropertyFinder _finder;

        // Serialized properties for organized display
        SerializedProperty resolution;
        SerializedProperty minFrequency;
        SerializedProperty maxFrequency;
        SerializedProperty minRange;
        SerializedProperty maxRange;
        SerializedProperty pitchOffsetInSemitones;
        SerializedProperty threshold;
        SerializedProperty gain;
        SerializedProperty dynamicRange;
        SerializedProperty smoothingStrength;
        SerializedProperty snapStrength;

        #endregion

        #region Editor implementation

        void OnEnable()
        {
            try
            {
                _deviceSelector = new DeviceSelector(serializedObject);
                _finder = new PropertyFinder(serializedObject);

                // Cache property references
                resolution = _finder["resolution"];
                minFrequency = _finder["minFrequency"];
                maxFrequency = _finder["maxFrequency"];
                minRange = _finder["minRange"];
                maxRange = _finder["maxRange"];
                pitchOffsetInSemitones = _finder["pitchOffsetInSemitones"];
                threshold = _finder["threshold"];
                gain = _finder["gain"];
                dynamicRange = _finder["dynamicRange"];
                smoothingStrength = _finder["smoothingStrength"];
                snapStrength = _finder["snapStrength"];
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
                EditorGUILayout.PropertyField(serializedObject.FindProperty("deviceID"), new GUIContent("Device ID"));
            }
            
            EditorGUILayout.Space();

            // FFT Settings Section
            EditorGUILayout.LabelField("FFT Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(resolution);
            EditorGUILayout.Space();

            // Pitch Detection Section
            EditorGUILayout.LabelField("Pitch Detection Range", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(minFrequency, new GUIContent("Min Frequency (Hz)"));
            EditorGUILayout.PropertyField(maxFrequency, new GUIContent("Max Frequency (Hz)"));
            EditorGUILayout.PropertyField(minRange, new GUIContent("Min Range (MIDI)"));
            EditorGUILayout.PropertyField(maxRange, new GUIContent("Max Range (MIDI)"));
            EditorGUILayout.PropertyField(pitchOffsetInSemitones, new GUIContent("Pitch Offset (Semitones)"));
            EditorGUILayout.PropertyField(threshold, new GUIContent("Detection Threshold"));
            EditorGUILayout.Space();

            // Audio Processing Section
            EditorGUILayout.LabelField("Audio Processing", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(gain, new GUIContent("Input Gain (dB)"));
            EditorGUILayout.PropertyField(dynamicRange, new GUIContent("Dynamic Range (dB)"));
            EditorGUILayout.PropertyField(smoothingStrength, new GUIContent("Smoothing Strength"));
            EditorGUILayout.PropertyField(snapStrength, new GUIContent("Snap Strength"));
            EditorGUILayout.Space();

            // Runtime Information (only shown during play mode and single selection)
            if (Application.isPlaying && targets.Length == 1)
            {
                EditorGUILayout.LabelField("Runtime Information", EditorStyles.boldLabel);
                
                var detector = (PitchDetector)target;
                
                EditorGUI.BeginDisabledGroup(true);
                
                EditorGUILayout.FloatField("Raw Pitch (Hz)", detector.rawPitch);
                EditorGUILayout.FloatField("Display Pitch (Hz)", detector.displayPitch);
                EditorGUILayout.FloatField("Offset Display Pitch (Hz)", detector.offsetDisplayPitch);
                EditorGUILayout.FloatField("Confidence", detector.confidence);
                EditorGUILayout.FloatField("Level (dB)", detector.level);
                EditorGUILayout.FloatField("Gained Level (dB)", detector.gainedLevel);
                
                // Show note information if we have a valid pitch
                if (detector.displayPitch > 0)
                {
                    OctaveNote note = OctaveNote.FromFrequency(detector.offsetDisplayPitch);
                    EditorGUILayout.TextField("Note Name", note.ToString());
                    EditorGUILayout.FloatField("MIDI Note", OctaveNote.MidiNumFromFrequency(detector.offsetDisplayPitch));
                    EditorGUILayout.FloatField("Cent Offset", note.cent);
                }
                
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