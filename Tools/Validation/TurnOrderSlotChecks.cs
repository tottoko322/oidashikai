using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class TurnOrderSlotChecks
{
    static int checks;
    static CoinTossController controller;
    static InputLockManager inputLock;
    static bool running;
    static double started;
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static TurnOrderSlotChecks()
    {
        EditorApplication.update += () =>
        {
            if (!running) return;
            EditorApplication.QueuePlayerLoopUpdate();
            if (EditorApplication.isPaused) EditorApplication.isPaused = false;
            if (EditorApplication.timeSinceStartup - started > 45)
            {
                System.IO.File.WriteAllText("slot-checks-result.txt", "Timed out: playing=" + EditorApplication.isPlaying +
                    ", paused=" + EditorApplication.isPaused + ", controller=" + (controller != null && controller.IsPlaying));
                EditorApplication.Exit(1);
            }
        };
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("SlotChecks", false))
            {
                SessionState.SetBool("SlotChecks", false);
                running = true; started = EditorApplication.timeSinceStartup;
                Application.runInBackground = true;
                controller = new GameObject("SlotControllerFixture").AddComponent<CoinTossController>();
                controller.slotUI = controller.gameObject.AddComponent<TurnOrderSlotUI>();
                controller.slotUI.fadeDuration = 0.01f;
                controller.spinDuration = 0.06f;
                controller.resultHold = 0.04f;
                controller.minimumRows = 6;
                inputLock = InputLockManager.I != null ? InputLockManager.I
                    : new GameObject("InputLockFixture").AddComponent<InputLockManager>();
                controller.StartCoroutine(SafeRun(Suite()));
            }
        };
    }
    public static void Run() { SessionState.SetBool("SlotChecks", true); EditorApplication.EnterPlaymode(); }
    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        checks++; Debug.Log("SLOT PASS: " + message);
    }
    static IEnumerator SafeRun(IEnumerator suite)
    {
        var stack = new Stack<IEnumerator>(); stack.Push(suite);
        int frames = 0;
        while (stack.Count > 0)
        {
            bool moved;
            object current = null;
            try
            {
                if (++frames > 10000) throw new Exception("Slot test did not finish");
                moved = stack.Peek().MoveNext();
                if (moved) current = stack.Peek().Current;
            }
            catch (Exception e)
            {
                foreach (var iterator in stack) (iterator as IDisposable)?.Dispose();
                Time.timeScale = 1f;
                System.IO.File.WriteAllText("slot-checks-result.txt", e.ToString());
                Debug.LogException(e); EditorApplication.Exit(1); yield break;
            }
            if (!moved) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
            if (current is IEnumerator nested) stack.Push(nested);
            else yield return current;
        }
        Time.timeScale = 1f;
        System.IO.File.WriteAllText("slot-checks-result.txt", "PASS " + checks);
        Debug.Log("ALL SLOT CHECKS PASSED: " + checks);
        EditorApplication.Exit(0);
    }
    static T Inactive<T>(string name) where T : Component
    {
        var go = new GameObject(name); go.SetActive(false); return go.AddComponent<T>();
    }
    static int SeedFor(CoinFace face)
    {
        for (int i = 0; i < 100; i++)
        {
            UnityEngine.Random.InitState(i);
            if ((UnityEngine.Random.value < 0.5f ? CoinFace.Q : CoinFace.I) == face) return i;
        }
        throw new Exception("Cannot find deterministic seed");
    }
    static GameObject Root() => (GameObject)typeof(TurnOrderSlotUI).GetField("root", Private).GetValue(controller.slotUI);
    static IEnumerator Suite()
    {
        foreach (bool first in new[] { true, false })
        {
            foreach (int minimum in new[] { -10, 6, 7, 14, 60, 100 })
            {
                int target = TurnOrderReel.StopIndex(first, minimum);
                Check(TurnOrderReel.IsFirstRow(target) == first, "Stop row agrees with result " + first + "/" + minimum);
                Check(target >= 6 && target <= 61, "Stop row stays within a bounded animation length " + minimum);
            }
        }
        Check(TurnOrderReel.Position(0f, 14) == 0f && TurnOrderReel.Position(1f, 14) == 14f,
            "Reel has exact start and stop positions");
        Check(TurnOrderReel.Position(-1f, 14) == 0f && TurnOrderReel.Position(2f, 14) == 14f,
            "Frame overruns clamp to the stop position");
        float previous = 0;
        for (int i = 1; i <= 10; i++)
        {
            float current = TurnOrderReel.Position(i / 10f, 14);
            Check(current >= previous, "Reel never reverses during slowdown " + i);
            previous = current;
        }
        float early = TurnOrderReel.Position(0.2f, 14) - TurnOrderReel.Position(0.1f, 14);
        float late = TurnOrderReel.Position(0.9f, 14) - TurnOrderReel.Position(0.8f, 14);
        Check(early > late, "The final reel movement is slower than its initial movement");

        var character = ScriptableObject.CreateInstance<CharacterData>();
        var context = SelectedCharacterContext.Ensure();
        context.Select(character, null);
        controller.decider = new GameObject("Decider").AddComponent<FirstTurnDecider>();
        controller.ui = new GameObject("LegacyCoinUI").AddComponent<CoinUI>();
        foreach (CoinFace firstFace in new[] { CoinFace.Q, CoinFace.I })
        {
            foreach (CoinFace face in new[] { CoinFace.Q, CoinFace.I })
            {
                character.faceForFirst = firstFace;
                int seed = SeedFor(face);
                UnityEngine.Random.InitState(seed);
                UnityEngine.Random.value.ToString();
                float nextRandom = UnityEngine.Random.value;
                UnityEngine.Random.InitState(seed);
                inputLock.Unlock();
                var play = controller.Play();
                Check(play.MoveNext() && controller.IsPlaying && !controller.ResultReady, "Intro starts before reporting a ready result");
                Check(inputLock.IsLocked, "Input is locked while the reel spins");
                var duplicate = controller.Play();
                Check(duplicate.MoveNext() && duplicate.Current == null, "A duplicate request waits for the existing reel");
                yield return play.Current;
                Check(!play.MoveNext(), "Intro completes after the visual presentation");
                Check(!duplicate.MoveNext(), "A waiting duplicate does not start another roll");
                bool expectedFirst = face == firstFace;
                Check(controller.ResultFace == face && controller.PlayerFirst == expectedFirst,
                    "Character coin mapping is preserved " + firstFace + "/" + face);
                Check(controller.ResultReady && !controller.IsPlaying && controller.slotUI.Completed,
                    "Completed intro publishes its result once");
                Check(controller.slotUI.HasStopped && controller.slotUI.StoppedOnFirst == expectedFirst,
                    "The visual stop and battle turn order agree");
                Check(controller.slotUI.CenterLabel == (expectedFirst ? "センコウ" : "コウコウ"), "The centered result text is correct");
                Check(!controller.slotUI.IsVisible && !inputLock.IsLocked, "Finished standalone intro hides and releases its input lock");
                Check(!controller.ui.gameObject.activeSelf, "The legacy coin UI stays hidden");
                Check(UnityEngine.Random.value == nextRandom, "Animation and duplicates do not reroll or consume random draws");
            }
        }

        inputLock.Lock();
        yield return controller.Play();
        Check(inputLock.IsLocked, "A pre-existing battle input lock is preserved");
        inputLock.Unlock();
        Time.timeScale = 0f;
        yield return controller.Play();
        Check(controller.ResultReady && controller.slotUI.Completed, "Reel and hold complete with paused game time");
        Time.timeScale = 1f;

        var cancelled = controller.Play();
        Check(cancelled.MoveNext(), "Cancelled intro starts normally");
        var visual = (IEnumerator)cancelled.Current;
        Check(visual.MoveNext() && controller.slotUI.IsVisible, "Cancelled visual owns a visible monitor");
        ((IDisposable)visual).Dispose(); ((IDisposable)cancelled).Dispose();
        Check(!controller.ResultReady && !controller.IsPlaying && !controller.slotUI.IsVisible && !inputLock.IsLocked,
            "Cancellation hides the monitor and does not publish an unfinished result");
        yield return controller.Play();
        Check(controller.ResultReady, "The intro can play again after cancellation");

        var interrupted = controller.Play(); interrupted.MoveNext();
        var interruptedVisual = (IEnumerator)interrupted.Current;
        interruptedVisual.MoveNext(); controller.slotUI.enabled = false;
        yield return interruptedVisual.Current;
        while (interruptedVisual.MoveNext()) yield return interruptedVisual.Current;
        interrupted.MoveNext();
        Check(!controller.ResultReady && !controller.slotUI.IsVisible && !inputLock.IsLocked,
            "Disabling the visual aborts instead of starting battle with an unfinished intro");
        controller.slotUI.enabled = true;

        var texture = new Texture2D(2, 2);
        var artwork = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), Vector2.one * 0.5f);
        controller.monitorArtwork = artwork;
        yield return controller.Play();
        Check(Root().transform.Find("MonitorFrame").GetComponent<Image>().sprite == artwork, "Monitor illustration can replace the placeholder");
        Check(!Root().transform.Find("MonitorFrame/PlaceholderStand").gameObject.activeSelf, "Illustration replacement removes the temporary stand");
        Check(Root().transform.Find("Backdrop").GetComponent<Image>().raycastTarget && Root().GetComponent<GraphicRaycaster>() != null,
            "The visible presentation has a full-screen input blocker");
        Check(Root().transform.Find("MonitorFrame/Screen").GetComponent<RectMask2D>() != null,
            "Scrolling labels are clipped inside the screen");
        controller.monitorArtwork = null;

        context.Select(null, null); controller.decider = null;
        yield return controller.Play();
        Check(controller.ResultReady && controller.PlayerFirst == (controller.ResultFace == CoinFace.Q),
            "Direct scene previews work without a selected character or decider");

        var battle = Inactive<BattleStateMachine>("BootFixture");
        battle.config = ScriptableObject.CreateInstance<GameConfig>();
        battle.winLose = Inactive<WinLoseSystem>("WinLose");
        battle.costManager = Inactive<CostManager>("Cost");
        battle.deckManager = Inactive<DeckManager>("Deck");
        battle.handManager = Inactive<HandManager>("Hand");
        battle.cardDealAnimator = Inactive<CardDealAnimator>("Deal");
        battle.coinToss = controller;
        battle.deckDefinition = ScriptableObject.CreateInstance<DeckDefinition>();
        battle.deckDefinition.entries.Add(new DeckEntry { card = ScriptableObject.CreateInstance<CardData>(), count = 1 });
        var boot = (IEnumerator)typeof(BattleStateMachine).GetMethod("Start", Private).Invoke(battle, null);
        Check(boot.MoveNext(), "Battle boot first waits on the turn-order presentation");
        Check(!battle.BattleReady && battle.handManager.handViews.Count == 0, "Gameplay and initial hand dealing wait for the intro");
        yield return boot.Current;
        Check(controller.ResultReady, "Battle receives a fully presented result");
        Check(boot.MoveNext(), "Battle proceeds to initial hand dealing after the result");
        Check(battle.turnSystem.Current == (controller.PlayerFirst ? TurnOwner.Player : TurnOwner.Enemy),
            "Battle turn system uses the displayed stop result");
        Check(!battle.BattleReady && inputLock.IsLocked, "Input stays locked while the initial hand is being dealt");
        ((IDisposable)boot).Dispose(); inputLock.Unlock();
    }
}
