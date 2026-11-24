using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    private const string MUSIC_PARAM = "MusicVolume";
    private const string SFX_PARAM = "SFXVolume";

    [SerializeField] private AudioMixer mixer;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider brightnessSlider;

    public static float MusicVolume => PlayerPrefs.GetFloat("music", 0.8f);
    public static float SfxVolume => PlayerPrefs.GetFloat("sfx", 0.8f);

    private float music = 0.8f;
    private float sfx = 0.8f;

    void Start()
    {
        music = PlayerPrefs.GetFloat("music", 0.8f);
        sfx = PlayerPrefs.GetFloat("sfx", 0.8f);
        float brightness = PlayerPrefs.GetFloat("brightness", 1f);

        if (musicSlider != null) musicSlider.SetValueWithoutNotify(music);
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(sfx);
        if (brightnessSlider != null) brightnessSlider.SetValueWithoutNotify(brightness);

        ApplyAudio();
        ApplyBrightness(brightness);
    }

    public void SetMusic(float value)
    {
        music = value;
        PlayerPrefs.SetFloat("music", value);
        ApplyAudio();
    }

    public void SetSfx(float value)
    {
        sfx = value;
        PlayerPrefs.SetFloat("sfx", value);
        ApplyAudio();
    }

    public void SetBrightness(float value)
    {
        PlayerPrefs.SetFloat("brightness", value);
        ApplyBrightness(value);
    }

    private void ApplyAudio()
    {
        if (mixer != null)
        {
            mixer.SetFloat(MUSIC_PARAM, ToDecibels(music));
            mixer.SetFloat(SFX_PARAM, ToDecibels(sfx));
        }

        AudioListener.volume = 1f;
    }

    private static void ApplyBrightness(float value)
    {
        Screen.brightness = Mathf.Clamp(value, 0.1f, 1f);
    }

    private static float ToDecibels(float value)
    {
        return Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f;
    }
}
