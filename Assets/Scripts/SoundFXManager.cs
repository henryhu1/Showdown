using UnityEngine;

public class SoundFXManager : MonoBehaviour
{
    public static SoundFXManager Instance { get; private set; }

    [SerializeField] private AudioSource m_SoundFXObject;
    [SerializeField] private AudioClip m_TickSoundClip;
    [SerializeField] private AudioClip m_EndTickSoundClip;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance);
        }
        Instance = this;
        DontDestroyOnLoad(this);
    }

    public void PlayTickSound(float volume)
    {
        PlaySoundFXClip(m_TickSoundClip, Vector3.zero, volume);
    }

    public void PlayEndTickSound(float volume)
    {
        PlaySoundFXClip(m_EndTickSoundClip, Vector3.zero, volume);
    }

    public void PlaySoundFXClip(AudioClip clip, Vector3 position, float volume, bool loop = false)
    {
        AudioSource audioSource = Instantiate(m_SoundFXObject, position, Quaternion.identity);

        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.loop = loop;
        audioSource.Play();
        float clipLength = clip.length;

        Destroy(audioSource.gameObject, clipLength);
    }
}
