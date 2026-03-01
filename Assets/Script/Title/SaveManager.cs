using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager I { get; private set; }

    const string KEY_BGM = "qpic_bgm";
    const string KEY_SE = "qpic_se";
    const string KEY_CONFIRM = "qpic_confirm";

    private void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    public float GetBgm(float def = 0.8f) => PlayerPrefs.GetFloat(KEY_BGM, def);
    public float GetSe(float def = 0.8f) => PlayerPrefs.GetFloat(KEY_SE, def);
    public bool GetConfirm(bool def = false) => PlayerPrefs.GetInt(KEY_CONFIRM, def ? 1 : 0) == 1;

    public void SetBgm(float v) { PlayerPrefs.SetFloat(KEY_BGM, Mathf.Clamp01(v)); PlayerPrefs.Save(); }
    public void SetSe(float v) { PlayerPrefs.SetFloat(KEY_SE, Mathf.Clamp01(v)); PlayerPrefs.Save(); }
    public void SetConfirm(bool on) { PlayerPrefs.SetInt(KEY_CONFIRM, on ? 1 : 0); PlayerPrefs.Save(); }
}
