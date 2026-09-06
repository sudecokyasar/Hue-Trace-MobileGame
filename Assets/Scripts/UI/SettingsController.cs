using UnityEngine;
using UnityEngine.UI;

public class SettingsController : MonoBehaviour
{
    [SerializeField] private GameObject settingsPanel;



    [Header("Music Settings")]
    [SerializeField] private Button musicButton;
    [SerializeField] private Sprite musicOnSprite;
    [SerializeField] private Sprite musicOffSprite;
    [SerializeField] private Slider musicSlider;

    [Header("SFX (Sound Effects) Settings")]
    [SerializeField] private Button sfxButton;
    [SerializeField] private Sprite sfxOnSprite;
    [SerializeField] private Sprite sfxOffSprite;
    [SerializeField] private Slider sfxSlider;

    private bool isMusicMuted = false;
    private bool isSfxMuted = false;

    private float lastMusicVolume = 1f;
    private float lastSfxVolume = 1f;

    private AudioManager audioManager;

    private void Awake()
    {
        audioManager = FindFirstObjectByType<AudioManager>();
    }

    private void Start()
    {
        LoadSettings();

        if (musicButton != null) musicButton.onClick.AddListener(ToggleMusic);
        if (sfxButton != null) sfxButton.onClick.AddListener(ToggleSfx);

        if (musicSlider != null) musicSlider.onValueChanged.AddListener(SetMusicVolume);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(SetSfxVolume);
    }

    private void LoadSettings()
    {
        isMusicMuted = PlayerPrefs.GetInt("MusicMuted", 0) == 1;
        isSfxMuted = PlayerPrefs.GetInt("SfxMuted", 0) == 1;

        float savedMusicVol = PlayerPrefs.GetFloat("MusicVolume", 1f);
        float savedSfxVol = PlayerPrefs.GetFloat("SfxVolume", 1f);

        float initialMusic = isMusicMuted ? 0f : savedMusicVol;
        float initialSfx = isSfxMuted ? 0f : savedSfxVol;

        if (musicSlider != null) musicSlider.value = initialMusic;
        if (sfxSlider != null) sfxSlider.value = initialSfx;

        ApplyMusicVolume(initialMusic);
        ApplySfxVolume(initialSfx);

        UpdateMusicUI();
        UpdateSfxUI();
    }

    public void ToggleMusic()
    {
        isMusicMuted = !isMusicMuted;

        if (isMusicMuted)
        {
            if (musicSlider != null)
            {
                lastMusicVolume = musicSlider.value > 0 ? musicSlider.value : 1f;
                musicSlider.value = 0f;
            }
            ApplyMusicVolume(0f);
        }
        else
        {
            float targetVol = lastMusicVolume > 0 ? lastMusicVolume : 1f;
            if (musicSlider != null) musicSlider.value = targetVol;
            ApplyMusicVolume(targetVol);
        }

        PlayerPrefs.SetInt("MusicMuted", isMusicMuted ? 1 : 0);
        PlayerPrefs.Save();
        UpdateMusicUI();
    }

    public void SetMusicVolume(float volume)
    {
        isMusicMuted = volume <= 0.001f;
        if (!isMusicMuted) lastMusicVolume = volume;

        PlayerPrefs.SetFloat("MusicVolume", volume);
        PlayerPrefs.SetInt("MusicMuted", isMusicMuted ? 1 : 0);
        PlayerPrefs.Save();

        ApplyMusicVolume(volume);
        UpdateMusicUI();
    }

    private void ApplyMusicVolume(float volume)
    {
        if (audioManager != null)
        {
            audioManager.SetMusicVolume(volume);
        }
    }

    private void UpdateMusicUI()
    {
        if (musicButton != null)
        {
            Image btnImage = musicButton.GetComponent<Image>();
            if (btnImage != null)
            {
                btnImage.sprite = isMusicMuted ? musicOffSprite : musicOnSprite;
            }
        }
    }

    public void ToggleSfx()
    {
        isSfxMuted = !isSfxMuted;

        if (isSfxMuted)
        {
            if (sfxSlider != null)
            {
                lastSfxVolume = sfxSlider.value > 0 ? sfxSlider.value : 1f;
                sfxSlider.value = 0f;
            }
            ApplySfxVolume(0f);
        }
        else
        {
            float targetVol = lastSfxVolume > 0 ? lastSfxVolume : 1f;
            if (sfxSlider != null) sfxSlider.value = targetVol;
            ApplySfxVolume(targetVol);
        }

        PlayerPrefs.SetInt("SfxMuted", isSfxMuted ? 1 : 0);
        PlayerPrefs.Save();
        UpdateSfxUI();
    }

    public void SetSfxVolume(float volume)
    {
        isSfxMuted = volume <= 0.001f;
        if (!isSfxMuted) lastSfxVolume = volume;

        PlayerPrefs.SetFloat("SfxVolume", volume);
        PlayerPrefs.SetInt("SfxMuted", isSfxMuted ? 1 : 0);
        PlayerPrefs.Save();

        ApplySfxVolume(volume);
        UpdateSfxUI();
    }

    private void ApplySfxVolume(float volume)
    {
        if (audioManager != null)
        {
            audioManager.SetSFXVolume(volume);
        }
    }

    private void UpdateSfxUI()
    {
        if (sfxButton != null)
        {
            Image btnImage = sfxButton.GetComponent<Image>();
            if (btnImage != null)
            {
                btnImage.sprite = isSfxMuted ? sfxOffSprite : sfxOnSprite;
            }
        }
    }

    public void OnClickBack()
    {
        settingsPanel.SetActive(false);
    }
}