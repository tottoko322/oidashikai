using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager I { get; private set; }

    [Header("Sources")]
    public AudioSource bgmSource;
    public AudioSource seSource;

    [Header("BGM")]
    public AudioClip bgmClip;

    [Header("SE Clips")]
    public AudioClip buffClip;
    public AudioClip buttonClip;
    public AudioClip damageClip;
    public AudioClip deadClip;
    public AudioClip drawClip;
    public AudioClip healClip;
    public AudioClip turnEndClip;
    public AudioClip turnStartClip;
    public AudioClip youLoseClip;
    public AudioClip youWinClip;

    [Header("Volume")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float bgmVolume = 0.7f;
    [Range(0f, 1f)] public float seVolume = 1f;

    [Header("Fade")]
    public float defaultFadeTime = 0.5f;

    private Coroutine bgmFadeCoroutine;

    private const string MasterKey = "MASTER_VOLUME";
    private const string BgmKey = "BGM_VOLUME";
    private const string SeKey = "SE_VOLUME";

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
    }

    private void Start()
    {
        LoadVolume();
        ApplySavedVolume();

        if (bgmSource != null && bgmClip != null)
        {
            PlayBgm(bgmClip, true);
        }
    }

    // =========================
    // Volume
    // =========================
    public void ApplySavedVolume()
    {
        if (bgmSource != null)
            bgmSource.volume = masterVolume * bgmVolume;

        if (seSource != null)
            seSource.volume = masterVolume * seVolume;
    }

    public void SaveVolume()
    {
        PlayerPrefs.SetFloat(MasterKey, masterVolume);
        PlayerPrefs.SetFloat(BgmKey, bgmVolume);
        PlayerPrefs.SetFloat(SeKey, seVolume);
        PlayerPrefs.Save();
    }

    public void LoadVolume()
    {
        if (PlayerPrefs.HasKey(MasterKey))
            masterVolume = PlayerPrefs.GetFloat(MasterKey);

        if (PlayerPrefs.HasKey(BgmKey))
            bgmVolume = PlayerPrefs.GetFloat(BgmKey);

        if (PlayerPrefs.HasKey(SeKey))
            seVolume = PlayerPrefs.GetFloat(SeKey);
    }

    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        ApplySavedVolume();
        SaveVolume();
    }

    public void SetBgmVolume(float value)
    {
        bgmVolume = Mathf.Clamp01(value);
        ApplySavedVolume();
        SaveVolume();
    }

    public void SetSeVolume(float value)
    {
        seVolume = Mathf.Clamp01(value);
        ApplySavedVolume();
        SaveVolume();
    }

    // =========================
    // SE
    // =========================
    private void PlayOneShot(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || seSource == null) return;

        float volume = Mathf.Clamp01(masterVolume * seVolume * volumeScale);
        seSource.PlayOneShot(clip, volume);
    }

    public void PlayBuff() => PlayOneShot(buffClip);
    public void PlayButton() => PlayOneShot(buttonClip);
    public void PlayDamage() => PlayOneShot(damageClip);
    public void PlayDead() => PlayOneShot(deadClip);
    public void PlayDraw() => PlayOneShot(drawClip);
    public void PlayHeal() => PlayOneShot(healClip);
    public void PlayTurnEnd() => PlayOneShot(turnEndClip);
    public void PlayTurnStart() => PlayOneShot(turnStartClip);
    public void PlayYouLose() => PlayOneShot(youLoseClip);
    public void PlayYouWin() => PlayOneShot(youWinClip);

    // 既存コード互換
    public void PlayClick() => PlayButton();
    public void PlayDragStart() => PlayButton();
    public void PlayDrop() => PlayBuff();
    public void PlayCancel() => PlayButton();

    // =========================
    // BGM
    // =========================
    public void PlayBgm(AudioClip clip, bool loop = true)
    {
        if (bgmSource == null || clip == null) return;

        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.volume = masterVolume * bgmVolume;
        bgmSource.Play();
    }

    public void StopBgm()
    {
        if (bgmSource == null) return;
        bgmSource.Stop();
    }

    public void FadeOutBgm(float duration = -1f)
    {
        if (bgmSource == null) return;

        if (duration <= 0f)
            duration = defaultFadeTime;

        if (bgmFadeCoroutine != null)
            StopCoroutine(bgmFadeCoroutine);

        bgmFadeCoroutine = StartCoroutine(CoFadeBgm(0f, duration, true));
    }

    public void FadeInBgm(AudioClip clip, bool loop = true, float duration = -1f)
    {
        if (bgmSource == null || clip == null) return;

        if (duration <= 0f)
            duration = defaultFadeTime;

        if (bgmFadeCoroutine != null)
            StopCoroutine(bgmFadeCoroutine);

        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.volume = 0f;
        bgmSource.Play();

        bgmFadeCoroutine = StartCoroutine(CoFadeBgm(masterVolume * bgmVolume, duration, false));
    }

    private IEnumerator CoFadeBgm(float targetVolume, float duration, bool stopAtEnd)
    {
        if (bgmSource == null) yield break;

        float startVolume = bgmSource.volume;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            bgmSource.volume = Mathf.Lerp(startVolume, targetVolume, t);
            yield return null;
        }

        bgmSource.volume = targetVolume;

        if (stopAtEnd && Mathf.Approximately(targetVolume, 0f))
        {
            bgmSource.Stop();
        }

        bgmFadeCoroutine = null;
    }
}
