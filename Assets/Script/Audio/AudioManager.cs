using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager I { get; private set; }

    [Header("Sources")]
    public AudioSource bgmSource;
    public AudioSource seSource;

    [Header("SE Clips")]
    public AudioClip click;
    public AudioClip dragStart;
    public AudioClip drop;
    public AudioClip cancel;

    private void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        ApplySavedVolume();
    }

    public void ApplySavedVolume()
    {
        if (SaveManager.I == null) return;
        SetBgmVolume(SaveManager.I.GetBgm());
        SetSeVolume(SaveManager.I.GetSe());
    }

    public void SetBgmVolume(float v)
    {
        if (bgmSource) bgmSource.volume = Mathf.Clamp01(v);
    }

    public void SetSeVolume(float v)
    {
        if (seSource) seSource.volume = Mathf.Clamp01(v);
    }

    public void PlayClick() => PlaySe(click);
    public void PlayDragStart() => PlaySe(dragStart);
    public void PlayDrop() => PlaySe(drop);
    public void PlayCancel() => PlaySe(cancel);

    public void PlaySe(AudioClip clip)
    {
        if (clip == null || seSource == null) return;
        seSource.PlayOneShot(clip, seSource.volume);
    }

}
