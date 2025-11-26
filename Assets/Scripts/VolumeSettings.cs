using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeSettings : MonoBehaviour
{
    public AudioMixer audioMixer;
    public Slider musicSlider;
    public Slider sfxSlider;

    void Start()
    {
        musicSlider.value = 10;
        sfxSlider.value = 10;

        SetMusicVolume(musicSlider.value);
        SetSFXVolume(sfxSlider.value);
    }

    public void SetMusicVolume(float value)
    {
        Debug.Log("Setting music volume to: " + value);
        float dB = Mathf.Lerp(-40f, 0f, value / 10f);
        audioMixer.SetFloat("MusicVolume", dB);
    }

    public void SetSFXVolume(float value)
    {
        Debug.Log("Setting SFX volume to: " + value);
        float dB = Mathf.Lerp(-40f, 0f, value / 10f);
        audioMixer.SetFloat("SFXVolume", dB);
    }
}
