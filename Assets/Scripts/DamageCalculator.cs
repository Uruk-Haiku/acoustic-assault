using System;
using Lasp;
using System.Collections;
using UnityEngine;
using TMPro;

public class DamageCalculator : MonoBehaviour
{
    [Header("GameObjects References")]
    public PitchDetector pitchDetector;
    //public SimpleMidiPlayer midiPlayer;
    // Damage Slider UI element to show damage
    public UnityEngine.UI.Slider damageSlider1;
    public UnityEngine.UI.Slider damageSlider2;
    private UnityEngine.UI.Slider currentDamageSlider;
    // Health Slider UI element to show health
    public UnityEngine.UI.Slider healthSlider1;
    public UnityEngine.UI.Slider healthSlider2;
    // Button that does damage when pressed
    public UnityEngine.UI.Button damageButton;
    // Text element to show win message
    public PortraitsChange portraitsChange;

    [Header("Damage bars")]
    // Damage bar is the bar that the player will build up while singing and goes to zero
    // after the damage is applied every round
    public UnityEngine.UI.Slider damageBar1;
    public UnityEngine.UI.Slider damageBar2;
    private UnityEngine.UI.Slider currentDamageBar;

    [Header("Loudness Settings")]
    // Whether to use loudness as a multiplier for damage
    [SerializeField] private bool applyLoundness = true;
    // Maximum loudness multiplier to multiply damage by
    [SerializeField] private float loudnessMultiplier = 2.0f;
    // Minimum and maximum loudness in dB for normalization
    [SerializeField] private float minLoudnessDb = -40f;
    [SerializeField] private float maxLoudnessDb = 0f;

    [Header("Parameters")]
    // Tolerance factor for frequency matching, the higher the more tolerance
    [SerializeField] private float frequencyToleranceFactor = 2.0f;
    // Multiplier of maximum damage
    [SerializeField] private float maxDamageMultiplier = 1.0f;

    // Total time length of the song in seconds
    [SerializeField] private float timeLength = 15.0f;
    [SerializeField] private float[] timeStamps;
    private int currentTimestampIndex = 0;
    private bool Player1Singing;

    private float MaximumDamage;

    private float damageAccumulated1 = 0f;
    private float damageAccumulated2 = 0f;

    private float totalDamage1 = 0f;
    private float totalDamage2 = 0f;

    public float currentHealth1 = 1f;
    public float currentHealth2 = 1f;
    public bool isGameOver = false;
    public int gameEndState = 0;

    // How often to calculate damage (in seconds)
    [SerializeField] private float damageCalculationInterval = 0.05f;

    // Multiplier to apply to damage effect on health
    [SerializeField] private float damageEffectMultiplier = 0.3f;

    // Frequency of the note that is playing
    private float currentTargetFrequency = 0f;

    private bool gameEnded = false;

    public bool startRecording = false;

    void Awake()
    {
        // Calculate theoretical maximum damage: timelength * Coroutine calls per second * max damage per frame (100)
        // If loudness is applied, multiply by loudness multiplier
        // This should now be times Pow(2, 2.5)f for apply loudness, but will keep it like this so the slider visually looks like it's moving more when the player is louder
        if (applyLoundness)
            MaximumDamage = timeLength * (1 / damageCalculationInterval) * 100f * loudnessMultiplier * maxDamageMultiplier;
        else
            MaximumDamage = timeLength * (1 / damageCalculationInterval) * 100f * maxDamageMultiplier;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        damageSlider1.value = 1.0f;
        damageSlider2.value = 1.0f;
        currentDamageSlider = damageSlider2;

        damageBar1.value = 0f;
        damageBar2.value = 0f;
        currentDamageBar = damageBar1;

        Player1Singing = true;
        gameEnded = false;
    }

    public void StartRecordingDamage()
    {
        StartCoroutine(DamageRoutine());
        startRecording = true;
    }

    public void SwitchPlayer()
    {
        if (Player1Singing)
        {
            currentDamageSlider = damageSlider1;
            currentDamageBar = damageBar2;
        }
        else
        {
            portraitsChange.ChangePortraits(); // Change portraits on hit
            CheckWinning(); // Check if there's a winner before applying damage
            DoDamageToPlayer1();
            totalDamage2 += damageAccumulated2;
            damageAccumulated2 = 0f;

            DoDamageToPlayer2();
            totalDamage1 += damageAccumulated1;
            damageAccumulated1 = 0f; // Reset accumulated damage for next round
            currentDamageSlider = damageSlider2;

            currentDamageBar = damageBar1;
            damageBar1.value = 0f;
            damageBar2.value = 0f;
        }
        Player1Singing = !Player1Singing;
    }

    // Update is called once per frame
    void Update()
    {
        // Track song playback time
        if (!startRecording)
        {
            return;
        }
    }
    private IEnumerator DamageRoutine()
    {
        while (true)
        {
            // Prevent bug that breaks slider UI at start of game
            if (currentDamageSlider.value == float.NaN)
                currentDamageSlider.value = 1.0f;
            if (Player1Singing)
            {
                // Calculate accumulated damage and visualize using the slider
                damageAccumulated1 += ApplyLoudnessMultiplier(CalculateDamage(pitchDetector.offsetDisplayPitch), pitchDetector.gainedLevel);
                //Debug.Log(damageAccumulated);
                currentDamageSlider.value = Mathf.Max(0, (currentHealth2 - ((totalDamage1 + damageAccumulated1) / (6f * MaximumDamage)) * damageEffectMultiplier));
                //currentHealth2 = currentDamageSlider.value;

                // Damage bar increase for player1
                currentDamageBar.value = Mathf.Max(0, damageAccumulated1 / MaximumDamage * damageEffectMultiplier);
            }
            else
            {
                // Calculate accumulated damage and visualize using the slider
                damageAccumulated2 += ApplyLoudnessMultiplier(CalculateDamage(pitchDetector.offsetDisplayPitch), pitchDetector.gainedLevel);
                //Debug.Log(damageAccumulated);
                currentDamageSlider.value = Mathf.Max(0, (currentHealth1 - ((totalDamage2 + damageAccumulated2) / (6f * MaximumDamage)) * damageEffectMultiplier));
                //currentHealth1 = currentDamageSlider.value;

                // Damage bar increase for player2
                currentDamageBar.value = Mathf.Max(0, damageAccumulated2 / MaximumDamage * damageEffectMultiplier);
            }
            yield return new WaitForSeconds(damageCalculationInterval);
        }
    }

    public void SetTargetFrequency(float targetFrequency)
    {
        currentTargetFrequency = targetFrequency;
    }

    #region Damage Calculation
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

        // TODO: set this when we set pitch detector rather than reading every loop
        minLoudnessDb = -pitchDetector.dynamicRange;
        // Clamp loudness between -minLoudnessDB and maxLoudnessdB, then normalize to 0-1 range
        float normalizedLoudness = Mathf.InverseLerp(minLoudnessDb, maxLoudnessDb, loudness);
        // Changed formula to make loudness more apparent in slider
        return damage * MathF.Pow((1 + normalizedLoudness), 2.5f) * loudnessMultiplier;
    }
    #endregion

    private void DoDamageToPlayer1()
    {
        // Apply damage to health slider based on damage slider value, more detailed formula can be applied here
        healthSlider1.value = Mathf.Max(0f, damageSlider1.value);
    }

    private void DoDamageToPlayer2()
    {
        // Apply damage to health slider based on damage slider value, more detailed formula can be applied here
        healthSlider2.value = Mathf.Max(0f, damageSlider2.value);
    }

    private void CheckWinning()
    {
        if (damageSlider1.value < 0.1 && damageSlider2.value < 0.1)
        {
            isGameOver = true;
            gameEndState = 0;
            return;
        }
        if (damageSlider2.value < 0.1)
        {
                isGameOver = true;
                gameEndState = 1;
            return;
        }

        if (damageSlider2.value < 0.1)
        {
                isGameOver = true;
                gameEndState = 2;
            return;
        }
    }
}
