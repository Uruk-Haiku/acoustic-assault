using Lasp;
using System.Collections;
using UnityEngine;

public class DamageCalculator : MonoBehaviour
{
    [Header("GameObjects References")]
    public SimplePitchDetector pitchDetector;
    public SimpleMidiPlayer midiPlayer;
    // Damage Slider UI element to show damage
    public UnityEngine.UI.Slider damageSlider;
    // Health Slider UI element to show health
    public UnityEngine.UI.Slider healthSlider;
    // Button that does damage when pressed
    public UnityEngine.UI.Button damageButton;

    [Header("Loudness Settings")]
    // Whether to use loudness as a multiplier for damage
    [SerializeField] private bool applyLoundness = true;
    // Maximum loudness multiplier to multiply damage by
    [SerializeField] private float loudnessMultiplier = 2.0f;
    // Minimum and maximum loudness in dB for normalization
    [SerializeField] private float minLoudnessDb = -60f;
    [SerializeField] private float maxLoudnessDb = -20f;

    [Header("Parameters")]
    // Tolerance factor for frequency matching, the higher the more tolerance
    [SerializeField] private float frequencyToleranceFactor = 2.0f;

    // Total time length of the song in seconds
    [SerializeField] private float timeLength = 15.0f;

    private float MaximumDamage;

    private float damageAccumulated = 0f;

    // How often to calculate damage (in seconds)
    [SerializeField] private float damageCalculationInterval = 0.05f;

    // Frequency of the note that is playing
    private float currentTargetFrequency = 0f;

    void Awake()
    {
        // Calculate theoretical maximum damage: timelength * Coroutine calls per second * max damage per frame (100)
        // If loudness is applied, multiply by loudness multiplier
        if (applyLoundness)
            MaximumDamage = timeLength * (1 / damageCalculationInterval) * 100f * loudnessMultiplier;
        else
            MaximumDamage = timeLength * (1 / damageCalculationInterval) * 100f;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        damageSlider.value = 0f;
        StartCoroutine(DamageRoutine());
        damageButton.onClick.AddListener(DoDamage);
    }

    // Update is called once per frame
    void Update()
    {

    }
    private IEnumerator DamageRoutine()
    {
        while (true)
        {
            // Prevent bug that breaks slider UI at start of game
            if (damageSlider.value == float.NaN)
                damageSlider.value = 0f;
            // Calculate accumulated damage and visualize using the slider
            damageAccumulated += ApplyLoudnessMultiplier(CalculateDamage(pitchDetector.pitch), pitchDetector.loudness);
            //Debug.Log(damageAccumulated);
            damageSlider.value = Mathf.Max(0, damageAccumulated / MaximumDamage);
            yield return new WaitForSeconds(damageCalculationInterval);
        }
    }

    public void SetTargetFrequency(float targetFrequency)
    {
        currentTargetFrequency = targetFrequency;
    }

    private float CalculateDamage(float playerFrequency)
    {
        if (currentTargetFrequency == 0f)
            return 0f;
        // Calculate the difference between smoothed pitch and target frequency.
        float Difference = currentTargetFrequency - playerFrequency;
        float absDifference = Mathf.Abs(Difference);

        // Calculate damage based on the difference.
        // Maximum frequency difference to get score is under frequencyToleranceFactor * 100, decreases with difference
        // Final damage is clamped to 0 to avoid negative damage
        // Value of damage is between 0 and 100
        float damage = (Mathf.Max(0f, frequencyToleranceFactor * 100 - absDifference)) / frequencyToleranceFactor;
        return damage;
    }

    private float ApplyLoudnessMultiplier(float damage, float loudness)
    {
        // if loudness multiplier is not applied, return original damage
        if (!applyLoundness)
            return damage;

        // Clamp loudness between -minLoudnessDB and maxLoudnessdB, then normalize to 0-1 range
        float normalizedLoudness = Mathf.InverseLerp(minLoudnessDb, maxLoudnessDb, loudness);
        // Debug.Log("NormalizedLoudness: " + normalizedLoudness);
        return damage * normalizedLoudness * loudnessMultiplier;
    }

    public void DoDamage()
    {
        // Apply damage to health slider based on damage slider value, more detailed formula can be applied here
        healthSlider.value = Mathf.Max(0f, healthSlider.value - damageSlider.value);
    }
}
