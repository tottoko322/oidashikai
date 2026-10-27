using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class AudioPromptChecks
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static int checks;
    static AudioPromptChecks()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("AudioPromptChecks", false))
            {
                SessionState.SetBool("AudioPromptChecks", false); Suite();
            }
        };
    }
    public static void Run() { SessionState.SetBool("AudioPromptChecks", true); EditorApplication.EnterPlaymode(); }
    static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
        checks++; Debug.Log("AUDIO/PROMPT PASS: " + message);
    }
    static void Suite()
    {
        var keys = new[] { "MASTER_VOLUME", "BGM_VOLUME", "SE_VOLUME", "qpic_bgm", "qpic_se" };
        var values = new Dictionary<string, float>();
        foreach (string key in keys) if (PlayerPrefs.HasKey(key)) values[key] = PlayerPrefs.GetFloat(key);
        int exitCode = 0;
        try
        {
            PlayerPrefs.SetFloat("MASTER_VOLUME", 1f);
            PlayerPrefs.SetFloat("BGM_VOLUME", 0.7f);
            PlayerPrefs.SetFloat("qpic_bgm", 0f);
            PlayerPrefs.SetFloat("SE_VOLUME", 1f);
            var owner = new GameObject("AudioFixture");
            var audio = owner.AddComponent<AudioManager>();
            audio.bgmSource = owner.AddComponent<AudioSource>();
            audio.seSource = owner.AddComponent<AudioSource>();
            audio.bgmClip = AudioClip.Create("TitleFixture", 44100, 1, 44100, false);
            typeof(AudioManager).GetMethod("Start", Private).Invoke(audio, null);
            Check(Mathf.Approximately(audio.bgmVolume, 0.7f), "Audio loads the canonical volume instead of the stale legacy value");
            Check(audio.bgmSource.clip == audio.bgmClip && audio.bgmSource.loop && audio.bgmSource.isPlaying,
                "Scene BGM starts and loops");
            Check(Mathf.Approximately(audio.bgmSource.volume, 0.7f), "BGM has an audible source volume");
            var battleClip = AudioClip.Create("BattleFixture", 44100, 1, 44100, false);
            audio.PlayBgm(battleClip);
            Check(audio.bgmSource.clip == battleClip && audio.bgmSource.isPlaying, "Battle BGM can start with the saved volume");
            var save = new GameObject("SaveFixture").AddComponent<SaveManager>();
            Check(Mathf.Approximately(save.GetBgm(), 0.7f), "Settings read the same canonical BGM volume");
            var panel = new GameObject("SettingsFixture").AddComponent<SettingsModalUI>();
            panel.root = new GameObject("Panel");
            panel.bgmSlider = new GameObject("BgmSlider", typeof(RectTransform)).AddComponent<Slider>();
            panel.seSlider = new GameObject("SeSlider", typeof(RectTransform)).AddComponent<Slider>();
            int notifications = 0;
            panel.bgmSlider.onValueChanged.AddListener(_ => notifications++);
            panel.Open();
            Check(Mathf.Approximately(panel.bgmSlider.value, 0.7f) && notifications == 0,
                "Opening settings refreshes the slider without invoking its change handler");
            Check(Mathf.Approximately(PlayerPrefs.GetFloat("BGM_VOLUME"), 0.7f), "Opening settings does not overwrite saved volume");
            panel.bgmSlider.SetValueWithoutNotify(0.4f); panel.OnBgmChanged();
            Check(Mathf.Approximately(audio.bgmSource.volume, 0.4f) &&
                Mathf.Approximately(PlayerPrefs.GetFloat("BGM_VOLUME"), 0.4f) &&
                Mathf.Approximately(PlayerPrefs.GetFloat("qpic_bgm"), 0.4f), "User volume changes update playback and both save formats");
            audio.SetBgmVolume(0f); audio.LoadVolume();
            Check(audio.bgmVolume == 0f, "An intentional mute is preserved rather than forcibly reset");
            PlayerPrefs.DeleteKey("BGM_VOLUME"); PlayerPrefs.SetFloat("qpic_bgm", 0.6f); audio.LoadVolume();
            Check(Mathf.Approximately(audio.bgmVolume, 0.6f), "Legacy-only settings migrate without losing their volume");
            var defense = new GameObject("DefenseFixture").AddComponent<DefenseSelectUI>();
            defense.root = new GameObject("DefenseRoot");
            defense.message = new GameObject("Prompt", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            GameFonts.Apply(defense.message); defense.message.enabled = false;
            defense.BeginSelection();
            Check(defense.root.activeSelf && defense.message.enabled && defense.message.text.Contains("ぼうぎょ") &&
                defense.message.text.Contains("すきっぷ"), "Defense waiting shows the next available actions");
            Check(GameFonts.Default.HasCharacters(defense.message.text), "The action prompt has every glyph in JikiTourai");
            defense.BeginSelection("");
            Check(!string.IsNullOrWhiteSpace(defense.message.text), "An empty caller prompt cannot hide the action guidance");
            defense.EndSelection();
            Check(!defense.root.activeSelf, "Action guidance closes when defense selection ends");
            System.IO.File.WriteAllText("audio-prompt-checks-result.txt", "PASS " + checks);
            Debug.Log("ALL AUDIO/PROMPT CHECKS PASSED: " + checks);
        }
        catch (Exception error)
        {
            exitCode = 1; Debug.LogException(error);
            System.IO.File.WriteAllText("audio-prompt-checks-result.txt", error.ToString());
        }
        finally
        {
            foreach (string key in keys)
                if (values.TryGetValue(key, out float value)) PlayerPrefs.SetFloat(key, value);
                else PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
        EditorApplication.Exit(exitCode);
    }
}
