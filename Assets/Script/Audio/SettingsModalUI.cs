using UnityEngine;
using UnityEngine.UI;

public class SettingsModalUI : MonoBehaviour
{
    public GameObject root;
    public Slider bgmSlider;
    public Slider seSlider;
    public Toggle confirmToggle;

    public GameConfig config;

    private void Awake()
    {
        if(root)root.SetActive(false);
    }
    private void Start()
    {
        RefreshValues();
    }

    private void RefreshValues()
    {
        if (bgmSlider) bgmSlider.SetValueWithoutNotify(AudioManager.I != null
            ? AudioManager.I.bgmVolume : PlayerPrefs.GetFloat("BGM_VOLUME", PlayerPrefs.GetFloat("qpic_bgm", 0.7f)));
        if (seSlider) seSlider.SetValueWithoutNotify(AudioManager.I != null
            ? AudioManager.I.seVolume : PlayerPrefs.GetFloat("SE_VOLUME", PlayerPrefs.GetFloat("qpic_se", 1f)));
        if (confirmToggle) confirmToggle.SetIsOnWithoutNotify(SaveManager.I != null
            ? SaveManager.I.GetConfirm(config != null && config.useConfirm) : config != null && config.useConfirm);
    }

    public void Open() { RefreshValues(); if (root) root.SetActive(true); }
    public void Close() { if (root) root.SetActive(false); }

    public void OnBgmChanged()
    {
        float v=bgmSlider.value;
        SaveManager.I?.SetBgm(v);
        AudioManager.I?.SetBgmVolume(v);
    }

    public void OnSeChanged()
    {
        float v=seSlider.value;
        SaveManager.I?.SetSe(v);
        AudioManager.I?.SetSeVolume(v);
    }

    public void OnConfirmChanged(bool on)
    {
        SaveManager.I?.SetConfirm(on);
        if (config != null) config.useConfirm = on;
    }
}

