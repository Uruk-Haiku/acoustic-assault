using UnityEngine;

public class ScaleFromAudioClip : MonoBehaviour
{
    public AudioSource source;
    public Vector3 minScale;
    public Vector3 maxScale;
    public NewAudioAnalysis detector;

    public float loudnessSensibility = 100;
    public float threshhold = 0.5f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float loudness = detector.GetLoudnessFromAudioClip(source.timeSamples, source.clip) * loudnessSensibility;
        Debug.Log("Loudness: " + loudness);

        if (loudness < threshhold)
            loudness = 0;
            Debug.Log("Loudness reduced!!!");

        transform.localScale = Vector3.Lerp(minScale, maxScale, loudness);
    }
}
