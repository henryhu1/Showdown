using UnityEngine;
using UnityEngine.Audio;

public class SoundMixerManager : MonoBehaviour
{
    [SerializeField] private AudioMixer m_audioMixer;

    public void SetMasterVolume(float level)
    {
        SetVolume("MasterVolume", level);
    }

    public void SetSoundFXVolume(float level)
    {
        SetVolume("SoundFXVolume", level);
    }

    public void SetMusicVolume(float level)
    {
        SetVolume("MusicVolume", level);
    }

    private void SetVolume(string mixer, float level)
    {
        // level should be in between 10^(-4) and 10^1
        m_audioMixer.SetFloat(mixer, Mathf.Log10(level) * 20f);
    }
}
