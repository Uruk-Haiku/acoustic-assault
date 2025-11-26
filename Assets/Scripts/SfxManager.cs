using UnityEngine;
using UnityEngine.Audio;

public class SfxManager : MonoBehaviour
{
    public static SfxManager Instance { get; private set; }

    [SerializeField] private AudioSource sfxSource;

    public AudioMixerGroup sfxGroup;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();

        sfxSource.outputAudioMixerGroup = sfxGroup;
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
    }

    public void Play(AudioClip clip, float volume = 1f)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }
}