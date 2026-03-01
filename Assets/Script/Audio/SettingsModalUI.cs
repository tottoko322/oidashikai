using UnityEngine;
using UnityEngine.UI;

public class SettingsModalUI : MonoBehaviour
{
    public GameObject root;
    public Slider bgmSlider;
    public Slider seSlider;
    public Toggle confirmToggle;

    public GameConfig config;

    private void Start()
    {
        if (SaveManager.I != null)
        {
            if (bgmSlider) bgmSlider.value = SaveManager.I.GetBgm();
            if (seSlider) seSlider.value = SaveManager.I.GetSe();
            if (confirmToggle) confirmToggle.isOn = SaveManager.I.GetConfirm(config != null && config.useConfirm);
        }
    }

    public void Open() { if (root) root.SetActive(true); }
    public void Close() { if (root) root.SetActive(false); }

    public void OnBgmChanged(float v)
    {
        SaveManager.I?.SetBgm(v);
        AudioManager.I?.SetBgmVolume(v);
    }

    public void OnSeChanged(float v)
    {
        SaveManager.I?.SetSe(v);
        AudioManager.I?.SetSeVolume(v);
    }

    public void OnConfirmChanged(bool on)
    {
        SaveManager.I?.SetConfirm(on);
        if (config != null) config.useConfirm = on;
    }
}
