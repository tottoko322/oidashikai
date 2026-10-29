using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class MenuMusicRevealChecks
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static int checks;
    static bool running;
    static double started;
    static MenuMusicRevealChecks()
    {
        EditorApplication.update += () =>
        {
            if (!running) return;
            EditorApplication.QueuePlayerLoopUpdate();
            if (EditorApplication.isPaused) EditorApplication.isPaused = false;
            if (EditorApplication.timeSinceStartup - started > 45) EditorApplication.Exit(1);
        };
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("MusicRevealChecks", false)) return;
            SessionState.SetBool("MusicRevealChecks", false);
            running = true; started = EditorApplication.timeSinceStartup;
            var host = new GameObject("TestHost").AddComponent<CoinTossController>();
            host.gameObject.AddComponent<AudioListener>();
            UnityEngine.Object.DontDestroyOnLoad(host.gameObject);
            host.StartCoroutine(SafeRun(Suite()));
        };
    }
    public static void Run() { SessionState.SetBool("MusicRevealChecks", true); EditorApplication.EnterPlaymode(); }
    static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
        checks++; Debug.Log("MUSIC/REVEAL PASS: " + message);
    }
    static IEnumerator SafeRun(IEnumerator suite)
    {
        var stack = new Stack<IEnumerator>(); stack.Push(suite);
        while (stack.Count > 0)
        {
            bool moved; object current = null;
            try { moved = stack.Peek().MoveNext(); if (moved) current = stack.Peek().Current; }
            catch (Exception error)
            {
                foreach (var iterator in stack) (iterator as IDisposable)?.Dispose();
                System.IO.File.WriteAllText("music-reveal-checks-result.txt", error.ToString());
                Debug.LogException(error); EditorApplication.Exit(1); yield break;
            }
            if (!moved) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
            if (current is IEnumerator nested) stack.Push(nested); else yield return current;
        }
        System.IO.File.WriteAllText("music-reveal-checks-result.txt", "PASS " + checks);
        Debug.Log("ALL MUSIC/REVEAL CHECKS PASSED: " + checks); EditorApplication.Exit(0);
    }
    static AudioManager Audio(Scene scene, AudioClip clip)
    {
        var root = new GameObject("AudioFixture"); root.SetActive(false);
        SceneManager.MoveGameObjectToScene(root, scene);
        var sourceObject = new GameObject("SeparateBgmSource");
        SceneManager.MoveGameObjectToScene(sourceObject, scene);
        var source = sourceObject.AddComponent<AudioSource>(); source.playOnAwake = false;
        var audio = root.AddComponent<AudioManager>();
        audio.bgmSource = source; audio.seSource = root.AddComponent<AudioSource>(); audio.bgmClip = clip;
        root.SetActive(true); return audio;
    }
    static IEnumerator Suite()
    {
        var titleClip = AudioClip.Create("TitleTrack", 441000, 1, 44100, false);
        var battleClip = AudioClip.Create("BattleTrack", 441000, 1, 44100, false);
        var title = SceneManager.CreateScene("TitleFixture");
        var audio = Audio(title, titleClip); yield return null;
        Check(audio.bgmSource.isPlaying, "Title music starts");
        Check(audio.bgmSource.transform.IsChildOf(audio.transform), "Separate BGM source follows the persistent manager");
        yield return SceneManager.UnloadSceneAsync(title);
        foreach (string menu in new[] { "Dic", "Rule", "CharacterSelect" })
        {
            var scene = SceneManager.CreateScene(menu + "Fixture");
            float before = audio.bgmSource.time;
            yield return new WaitForSecondsRealtime(0.08f);
            Check(AudioManager.I == audio && audio.bgmSource.isPlaying && audio.bgmSource.clip == titleClip,
                menu + " keeps title music after scene unload");
            Check(audio.bgmSource.time >= before, menu + " does not restart the track");
            yield return SceneManager.UnloadSceneAsync(scene);
        }
        var returnTitle = SceneManager.CreateScene("ReturnTitleFixture");
        Audio(returnTitle, titleClip); yield return null;
        Check(AudioManager.I == audio && audio.bgmSource.isPlaying, "Returning to title reuses its existing track");
        Check(UnityEngine.Object.FindObjectsByType<AudioManager>(FindObjectsSortMode.None).Length == 1,
            "Returning to title does not leave duplicate audio managers");
        var battleScene = SceneManager.CreateScene("BattleFixture");
        var battleAudio = Audio(battleScene, battleClip); yield return null;
        Check(AudioManager.I == battleAudio && battleAudio.bgmSource.isPlaying && audio == null,
            "Entering battle replaces menu music with battle music");
        var titleAudio = Audio(returnTitle, titleClip); yield return null;
        Check(AudioManager.I == titleAudio && titleAudio.bgmSource.isPlaying && battleAudio == null,
            "Returning from battle starts title music");

        var canvas = new GameObject("RevealCanvas", typeof(RectTransform), typeof(Canvas));
        canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        ((RectTransform)canvas.transform).sizeDelta = new Vector2(1920f, 1080f);
        var reveal = canvas.AddComponent<CpuCardRevealUI>();
        var config = ScriptableObject.CreateInstance<CpuBattleConfig>();
        config.enterDuration = 0.01f; config.returnDuration = 0.08f;
        var portrait = new GameObject("EnemyPortrait", typeof(RectTransform)).GetComponent<RectTransform>();
        portrait.SetParent(canvas.transform, false); portrait.sizeDelta = new Vector2(400f, 400f);
        portrait.anchoredPosition = new Vector2(0f, 300f);
        reveal.Initialize(canvas.transform, GameFonts.Default, portrait, config);
        Check(config.holdDuration == 1.5f, "Regular card hold defaults to 1.5 seconds");
        var attackCard = ScriptableObject.CreateInstance<CardData>(); attackCard.attack = 3;
        foreach (bool skip in new[] { true, false })
        {
            var go = new GameObject("DefenseBattle"); go.SetActive(false);
            var battle = go.AddComponent<BattleStateMachine>();
            var handObject = new GameObject("Hand"); handObject.SetActive(false);
            battle.handManager = handObject.AddComponent<HandManager>();
            battle.cpuCardRevealUI = reveal;
            var view = new GameObject("DefenseCard").AddComponent<CardView>();
            var data = ScriptableObject.CreateInstance<CardData>(); data.defense = 1; view.Bind(data);
            battle.handManager.handViews.Add(view);
            typeof(BattleStateMachine).GetField("<PlayerHP>k__BackingField", Private).SetValue(battle, 10);
            typeof(BattleStateMachine).GetField("<EnemyHP>k__BackingField", Private).SetValue(battle, 10);
            typeof(BattleStateMachine).GetField("<BattleReady>k__BackingField", Private).SetValue(battle, true);
            var defense = (IEnumerator)typeof(BattleStateMachine).GetMethod("CoDefendPlayer", Private).Invoke(battle, new object[] { 3 });
            yield return DriveChoice(reveal.Show(attackCard, "コウゲキ", "ATK 3", defense), battle, reveal, view, skip);
            Check(!reveal.IsVisible && battle.PlayerHP == (skip ? 7 : 8),
                (skip ? "Skip" : "Defense") + " returns the card and resolves damage once");
        }
        float holdStarted = Time.realtimeSinceStartup;
        yield return InspectTimed(reveal.Show(attackCard, "こうか", "draw"), reveal);
        Check(Time.realtimeSinceStartup - holdStarted >= 1.5f && !reveal.IsVisible,
            "Non-attack cards remain visible for the longer hold and then return");
    }
    static IEnumerator InspectTimed(IEnumerator animation, CpuCardRevealUI reveal)
    {
        var stack = new Stack<IEnumerator>(); stack.Push(animation);
        float openedAt = -1f; bool closed = false;
        while (stack.Count > 0)
        {
            if (!stack.Peek().MoveNext()) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
            if (stack.Peek().Current is IEnumerator nested) { stack.Push(nested); continue; }
            if (openedAt < 0f && reveal.IsVisible)
            {
                reveal.OpenDetails(); openedAt = Time.realtimeSinceStartup;
            }
            if (!closed && openedAt >= 0f && Time.realtimeSinceStartup - openedAt > 1.7f)
            {
                Check(reveal.IsVisible && reveal.IsDetailOpen, "Reading details pauses the regular card timeout");
                reveal.CloseDetails(); closed = true;
            }
            yield return stack.Peek().Current;
        }
        Check(closed && !reveal.IsDetailOpen, "The regular card resumes its timer and cleans up details");
    }
    static IEnumerator DriveChoice(IEnumerator animation, BattleStateMachine battle, CpuCardRevealUI reveal, CardView card, bool skip)
    {
        var stack = new Stack<IEnumerator>(); stack.Push(animation);
        float waitingSince = -1f; bool chosen = false;
        while (stack.Count > 0)
        {
            bool moved = stack.Peek().MoveNext();
            if (!moved) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
            if (stack.Peek().Current is IEnumerator nested) { stack.Push(nested); continue; }
            if (battle.IsWaitingForDefense && !chosen)
            {
                if (waitingSince < 0f) waitingSince = Time.realtimeSinceStartup;
                if (Time.realtimeSinceStartup - waitingSince >= 0.2f)
                {
                    Check(reveal.IsVisible && battle.PlayerHP == 10, "Attack card and damage remain pending until a choice");
                    var text = reveal.transform.Find("CpuCardReveal/Usage/Text").GetComponent<TMP_Text>();
                    Check(text.text.Contains("ATK 3"), "The actual incoming attack value remains visible");
                    Check(reveal.transform.Find("CpuCardReveal").localScale.x == 0.7f,
                        "The waiting attack card uses the compact scale");
                    var cpuRect = (RectTransform)reveal.transform.Find("CpuCardReveal");
                    var cpuCorners = new Vector3[4]; cpuRect.GetWorldCorners(cpuCorners);
                    var portraitCorners = new Vector3[4];
                    ((RectTransform)reveal.transform.Find("EnemyPortrait")).GetWorldCorners(portraitCorners);
                    Check(cpuCorners[3].x < portraitCorners[0].x, "CPU card is beside the character on the left");
                    var popup = new GameObject("HandPopupFixture").AddComponent<CardPopupUI>();
                    popup.root = new GameObject("HandPopup", typeof(RectTransform));
                    popup.root.transform.SetParent(reveal.transform, false);
                    popup.Open(card);
                    Check(popup.root.GetComponent<Canvas>().overrideSorting &&
                        popup.root.GetComponent<Canvas>().sortingOrder > 32000,
                        "Hand inspection stays above CPU cards, detail overlays and guidance");
                    popup.Close(false);
                    reveal.OpenDetails();
                    Check(reveal.IsDetailOpen, "Clicking the compact card opens a large detail view");
                    reveal.CloseDetails();
                    Check(!reveal.IsDetailOpen && reveal.IsVisible && battle.IsWaitingForDefense,
                        "Closing details preserves the pending defense choice and compact card");
                    if (skip) battle.SkipDefense(); else battle.SelectDefenseCard(card);
                    Check(reveal.IsVisible, "Choosing starts a return animation rather than hiding instantly");
                    chosen = true;
                }
            }
            if (battle.PlayerHP < 10 && reveal.IsVisible) throw new Exception("Damage resolved before card returned");
            yield return stack.Peek().Current;
        }
        Check(chosen, "Attack presentation accepted a player choice");
    }
}

