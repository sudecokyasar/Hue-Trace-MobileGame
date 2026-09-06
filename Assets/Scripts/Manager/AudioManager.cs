using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Loop Music")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField][Range(0f, 1f)] private float musicVolume = 0.6f;

    [Header("Ses Efektleri")]
    [SerializeField] private AudioClip connectionSfx;
    [SerializeField] private AudioClip levelCompleteSfx;
    [SerializeField] private AudioClip iceBreakSfx;
    [SerializeField] private AudioClip buttonClickSfx;

    [Header("Ses Efekti Ayarlari")]
    [SerializeField][Range(0f, 1f)] private float sfxVolume = 1f;

    private const string MusicEnabledKey = "MusicEnabled";

    private AudioSource musicSource;
    private AudioSource sfxSource;


    public bool IsMusicEnabled { get; private set; } = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        IsMusicEnabled = PlayerPrefs.GetInt(MusicEnabledKey, 1) == 1;

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = musicVolume;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.volume = sfxVolume;
    }

    private void Start()
    {
        if (IsMusicEnabled)
        {
            PlayMusic();
        }
    }

    public void PlayMusic()
    {
        if (musicSource == null || backgroundMusic == null) return;
        if (musicSource.isPlaying && musicSource.clip == backgroundMusic) return;

        musicSource.clip = backgroundMusic;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource == null) return;
        musicSource.Stop();
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = volume;
        if (musicSource != null)
        {
            musicSource.volume = volume;
        }
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = volume;
        if (sfxSource != null)
        {
            sfxSource.volume = volume;
        }
    }


    public void SetMusicEnabled(bool enabled)
    {
        IsMusicEnabled = enabled;
        PlayerPrefs.SetInt(MusicEnabledKey, enabled ? 1 : 0);
        PlayerPrefs.Save();

        if (enabled)
        {
            PlayMusic();
        }
        else
        {
            StopMusic();
        }
    }

    public void PlayConnectionSfx()
    {
        PlayOneShot(connectionSfx);
    }

    public void PlayLevelCompleteSfx()
    {
        PlayOneShot(levelCompleteSfx);
    }

    public void PlayIceBreakSfx()
    {
        PlayOneShot(iceBreakSfx);
    }

    public void PlayButtonClickSfx()
    {
        PlayOneShot(buttonClickSfx);
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    public bool IsMusicPlaying()
    {
        return musicSource != null && musicSource.isPlaying;
    }
}