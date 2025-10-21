using UnityEngine;
using UnityEngine.UI;

public class MicLevelWriter : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] public TMPro.TMP_Text levelText;
    [SerializeField] public int playerID;

    // UI References
    [SerializeField] RectTransform _meterFillBar;
    [SerializeField] Image _meterFillImage;
    [SerializeField] RectTransform _dynamicRangeFloor;
    // [SerializeField] Text _levelText; // optional
    
    // Visual settings
    [SerializeField] Color _lowColor = Color.green;
    [SerializeField] Color _midColor = Color.yellow;
    [SerializeField] Color _highColor = Color.red;
    [SerializeField] float _warningThreshold = 0.7f;
    [SerializeField] float _dangerThreshold = 0.9f;
    [SerializeField] bool dynamicRangeEnabled = false;
    private Lasp.SimplePitchDetector pitchDetector;
    void OnEnable()
    {
        // Get current player
        SettingsPanel settingsPanel = GetComponentInParent<SettingsPanel>();
        playerID = settingsPanel.currentPlayer;
        // Get pitch detector
    }
    // Update is called once per frame
    void Update()
    {
        // This has to be here because if player changes mic, we still need to get new pitch detector.
        // Obvsiouly we could have a listener or something but not doing that for now. TODO
        pitchDetector = GameManager.GetPitchDetection(playerID);

        if (pitchDetector == null) return;
        // TODO is this stored in memory every frame? I just want to grab the reference and then use it
        levelText.text =
        pitchDetector.gainedLoudness.ToString("F1")
        +
        "dB";

        float level = (pitchDetector.gainedLoudness + 100) / 100; // Assuming this property exists

        // Update fill bar scale
        _meterFillBar.localScale = new Vector3(level, 1f, 1f);
        
        // Update color based on level
        Color meterColor;
        if (level < _warningThreshold)
            meterColor = Color.Lerp(_lowColor, _midColor, level / _warningThreshold);
        else if (level < _dangerThreshold)
            meterColor = Color.Lerp(_midColor, _highColor, 
                (level - _warningThreshold) / (_dangerThreshold - _warningThreshold));
        else
            meterColor = _highColor;

        _meterFillImage.color = meterColor;
        
        if (dynamicRangeEnabled && _dynamicRangeFloor != null)
        {
            _dynamicRangeFloor.gameObject.SetActive(dynamicRangeEnabled);

            // gainedLoudness assumes a peak of 0dB
            float dr = pitchDetector.dynamicRange;
            // Set Anchor scale x, We assume 100 db as full scale 
            _dynamicRangeFloor.localScale = new Vector3(1 - (dr / 100f), 1f, 1f);

        }
        // // Optional: Update text
        // if (_levelText != null)
        //     _levelText.text = $"{(level * 100):F1}%";
    }
}
