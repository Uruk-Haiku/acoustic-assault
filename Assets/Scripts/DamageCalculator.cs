using Lasp;
using System.Collections;
using UnityEngine;
using TMPro;

public class DamageCalculator : MonoBehaviour
{
    [Header("GameObjects References")]
    public SimplePitchDetector pitchDetector;
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
    public TextMeshProUGUI winText;

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
    // Multiplier of maximum damage
    [SerializeField] private float maxDamageMultiplier = 1.0f;

    // Total time length of the song in seconds
    [SerializeField] private float timeLength = 15.0f;
    [SerializeField] private float[] timeStamps;
    private int currentTimestampIndex = 0;
    private bool Player1Singing;
    private float songTime = 0f;


    private float MaximumDamage;

    private float damageAccumulated1 = 0f;
    private float damageAccumulated2 = 0f;

    // How often to calculate damage (in seconds)
    [SerializeField] private float damageCalculationInterval = 0.05f;

    // Frequency of the note that is playing
    private float currentTargetFrequency = 0f;

    private bool gameEnded = false;

    private bool startRecording = false;

    void Awake()
    {
        // Calculate theoretical maximum damage: timelength * Coroutine calls per second * max damage per frame (100)
        // If loudness is applied, multiply by loudness multiplier
        if (applyLoundness)
            MaximumDamage = timeLength * (1 / damageCalculationInterval) * 100f * loudnessMultiplier * maxDamageMultiplier;
        else
            MaximumDamage = timeLength * (1 / damageCalculationInterval) * 100f * maxDamageMultiplier;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        damageSlider1.value = 0f;
        damageSlider2.value = 0f;
        currentDamageSlider = damageSlider1;
        songTime = 0f;
        Player1Singing = true;
        gameEnded = false;
    }

    public void StartRecordingDamage(float startSongTime)
    {
        StartCoroutine(DamageRoutine());
        startRecording = true;
        songTime = startSongTime;
    }

    public void SwitchPlayer()
    {
        if (Player1Singing)
        {
            currentDamageSlider = damageSlider2;
        }
        else
        {
            CheckWinning(); // Check if there's a winner before applying damage
            DoDamageToPlayer1();
            damageAccumulated2 = 0f;
            damageSlider2.value = 0f;

            DoDamageToPlayer2();
            damageAccumulated1 = 0f; // Reset accumulated damage for next round
            damageSlider1.value = 0f; // Reset the slider for the next round
            currentDamageSlider = damageSlider1;
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
        songTime += Time.deltaTime;
    }
    private IEnumerator DamageRoutine()
    {
        while (true)
        {
            // Prevent bug that breaks slider UI at start of game
            if (currentDamageSlider.value == float.NaN)
                currentDamageSlider.value = 0f;
            if (Player1Singing)
            {
            // Calculate accumulated damage and visualize using the slider
            damageAccumulated1 += ApplyLoudnessMultiplier(CalculateDamage(pitchDetector.pitch), pitchDetector.loudness);
            //Debug.Log(damageAccumulated);
            currentDamageSlider.value = Mathf.Max(0, damageAccumulated1 / MaximumDamage);
            }
            else
            {
                // Calculate accumulated damage and visualize using the slider
                damageAccumulated2 += ApplyLoudnessMultiplier(CalculateDamage(pitchDetector.pitch), pitchDetector.loudness);
                //Debug.Log(damageAccumulated);
                currentDamageSlider.value = Mathf.Max(0, damageAccumulated2 / MaximumDamage);
            }
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

    private void DoDamageToPlayer1()
    {
        // Apply damage to health slider based on damage slider value, more detailed formula can be applied here
        healthSlider1.value = Mathf.Max(0f, healthSlider1.value - damageSlider2.value * 0.3f);
    }

    private void DoDamageToPlayer2()
    {
        // Apply damage to health slider based on damage slider value, more detailed formula can be applied here
        healthSlider2.value = Mathf.Max(0f, healthSlider2.value - damageSlider1.value * 0.3f);
    }

    private void CheckWinning()
    {
        if (healthSlider1.value <= (damageSlider2.value * 0.3f) && healthSlider2.value <= (damageSlider1.value * 0.3f))
        {
            if (damageSlider1.value > damageSlider2.value)
            {
                ShowWinningMessage(1);
            }
            else if (damageSlider2.value > damageSlider1.value)
            {
                ShowWinningMessage(2);
            }
            else
            {
                ShowWinningMessage(0); // Draw
            }
        }
        else if (healthSlider1.value <= (damageSlider2.value * 0.3f))
        {
            ShowWinningMessage(2);
        }
        else if (healthSlider2.value <= (damageSlider1.value * 0.3f))
        {
            ShowWinningMessage(1);
        }
    }

    public void EndGame()
    {
        if (gameEnded) return;
        if (healthSlider1.value > healthSlider2.value)
        {
            ShowWinningMessage(1);
        }
        else if (healthSlider2.value > healthSlider1.value)
        {
            ShowWinningMessage(2);
        }
        else
        {
            ShowWinningMessage(0); // Draw
        }
    }

    public void ShowWinningMessage(int winningPlayer)
    {
        switch(winningPlayer)
        {
            case 0:
                winText.text = "It's a Draw!";
                gameEnded = true;
                break;
            case 1:
                winText.text = "Player 1 Wins!";
                gameEnded = true;
                break;
            case 2:
                winText.text = "Player 2 Wins!";
                gameEnded = true;
                break;
        }

        StartCoroutine(ExitToMainMenu());
    }

    IEnumerator ExitToMainMenu()
    {
        yield return new WaitForSeconds(5);
        SongManager.Instance.EndSong();
        GameManager.Instance.LoadScene("MenuScreen");
    }
}
