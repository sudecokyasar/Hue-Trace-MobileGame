using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Arka Plan Müziði (Loop)")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField][Range(0f, 1f)] private float musicVolume = 0.6f;

    [Header("Ses Efektleri")]
    [Tooltip("Ýki ayný renk birleþince çalar.")]
    [SerializeField] private AudioClip connectionSfx;
    [Tooltip("Level tamamlandýðýnda çalar.")]
    [SerializeField] private AudioClip levelCompleteSfx;
    [Tooltip("Buz hücresinden geçilip kýrýldýðýnda çalar.")]
    [SerializeField] private AudioClip iceBreakSfx;
    [Tooltip("Herhangi bir UI butonuna týklandýðýnda çalar.")]
    [SerializeField] private AudioClip buttonClickSfx;

    [Header("Ses Efekti Ayarlarý")]
    [SerializeField][Range(0f, 1f)] private float sfxVolume = 1f;

    // PlayerPrefs anahtarý: müzik tercihini kalýcý hale getirir.
    private const string MusicEnabledKey = "MusicEnabled";

    private AudioSource musicSource;
    private AudioSource sfxSource;

    // Kaydedilmiþ tercihe göre müziðin açýk olup olmadýðý.
    // SettingsController'daki UI, butonun görselini (ikon vb.) bu
    // deðere göre ayarlayabilir.
    public bool IsMusicEnabled { get; private set; } = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Kaydedilmiþ tercihi oku (varsayýlan: açýk = 1).
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
        // Kullanýcý daha önce müziði kapattýysa burada hiç baþlatýlmaz.
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

    // SettingsController'daki müzik butonu BUNU çaðýrýyor.
    // Tercih PlayerPrefs'e kaydedilir, bir sonraki oyun açýlýþýnda da
    // (yani Start() tekrar çalýþtýðýnda) hatýrlanýr.
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