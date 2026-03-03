using System;
using System.Reflection;
using UnityEngine;

public class BattleBootstrap : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SaveManager saveManager;
    [SerializeField] private AudioManager audioManager;

    [SerializeField] private GameConfig gameConfig;          // ScriptableObject
    [SerializeField] private DeckDefinition deckDefinition;  // 配布デッキ
    [SerializeField] private DeckManager deckManager;
    [SerializeField] private HandManager handManager;
    [SerializeField] private CardDealAnimator cardDealAnimator;

    [Header("Boot Options")]
    [SerializeField] private bool dealInitialHandOnStart = true;

    [SerializeField] private int fallbackInitialHandCount = 4;

    private void Awake()
    {
        if (saveManager == null)
        {
            var prop = typeof(SaveManager).GetProperty("I", BindingFlags.Public | BindingFlags.Static);
            if (prop != null) saveManager = prop.GetValue(null) as SaveManager;
        }
        if (audioManager == null)
        {
            var prop = typeof(AudioManager).GetProperty("I", BindingFlags.Public | BindingFlags.Static);
            if (prop != null) audioManager = prop.GetValue(null) as AudioManager;
        }

        CallIfExists(saveManager, "Load");
        CallIfExists(saveManager, "Apply");
        CallIfExists(saveManager, "Reload");

        ApplyAudioFromSaveSafely();

        // --- 参照抜けチェック ---
        if (gameConfig == null) Debug.LogWarning("[BattleBootstrap] GameConfig is not set. (fallbackInitialHandCount will be used)");
        if (deckDefinition == null) Debug.LogError("[BattleBootstrap] DeckDefinition is not set.");
        if (deckManager == null) Debug.LogError("[BattleBootstrap] DeckManager is not set.");
        if (handManager == null) Debug.LogError("[BattleBootstrap] HandManager is not set.");
        if (cardDealAnimator == null) Debug.LogError("[BattleBootstrap] CardDealAnimator is not set.");
    }

    private void Start()
    {
        TryInitDeck();
        TryInitHand();

        if (dealInitialHandOnStart)
        {
            int initial = GetInitialHandCount();
            TryDealInitial(initial);
        }
    }

    private void TryInitDeck()
    {
        if (deckManager == null || deckDefinition == null) return;

        if (CallIfExists(deckManager, "Init", deckDefinition)) return;
        if (CallIfExists(deckManager, "Initialize", deckDefinition)) return;
        if (CallIfExists(deckManager, "Setup", deckDefinition)) return;
        if (CallIfExists(deckManager, "Build", deckDefinition)) return;
        if (CallIfExists(deckManager, "SetDeck", deckDefinition)) return;

        Debug.LogWarning("[BattleBootstrap] DeckManager init method was not found. (Tried: Init/Initialize/Setup/Build/SetDeck)");
    }

    private void TryInitHand()
    {
        if (handManager == null) return;

        if (CallIfExists(handManager, "Init")) return;
        if (CallIfExists(handManager, "Initialize")) return;
        if (CallIfExists(handManager, "Setup")) return;

    }

    private void TryDealInitial(int initialCount)
    {
        if (cardDealAnimator == null)
        {
            Debug.LogWarning("[BattleBootstrap] CardDealAnimator is null. Cannot deal cards automatically.");
            return;
        }

        if (CallIfExists(cardDealAnimator, "DealInitialHand", initialCount)) return;
        if (CallIfExists(cardDealAnimator, "Deal", initialCount)) return;
        if (CallIfExists(cardDealAnimator, "DealCards", initialCount)) return;
        if (CallIfExists(cardDealAnimator, "PlayInitialDeal", initialCount)) return;

        Debug.LogWarning("[BattleBootstrap] Deal method was not found on CardDealAnimator. (Tried: DealInitialHand/Deal/DealCards/PlayInitialDeal)");
    }

    private int GetInitialHandCount()
    {
        int v;
        if (TryGetIntField(gameConfig, new[]
            {
                "startHandCount",
                "initialHandCount",
                "startHand",
                "startingHandCount",
                "initialHand",
                "startHand"
            }, out v))
        {
            return v;
        }

        return fallbackInitialHandCount;
    }

    private void ApplyAudioFromSaveSafely()
    {
        if (audioManager == null || saveManager == null) return;

        float bgm = 0.8f;
        float se = 0.8f;

        try
        {
            var mGetBgm = typeof(SaveManager).GetMethod("GetBgm", BindingFlags.Public | BindingFlags.Instance);
            var mGetSe = typeof(SaveManager).GetMethod("GetSe", BindingFlags.Public | BindingFlags.Instance);
            if (mGetBgm != null) bgm = (float)mGetBgm.Invoke(saveManager, new object[] { 0.8f });
            if (mGetSe != null) se = (float)mGetSe.Invoke(saveManager, new object[] { 0.8f });
        }
        catch { /* ここは失敗しても致命じゃない */ }

        if (CallIfExists(audioManager, "ApplySavedVolume")) return;
        if (CallIfExists(audioManager, "ApplyVolumeFromSave")) return;

        if (CallIfExists(audioManager, "SetBgm", bgm) | CallIfExists(audioManager, "SetSe", se)) return;

        if (CallIfExists(audioManager, "SetBgmVolume", bgm) | CallIfExists(audioManager, "SetSeVolume", se)) return;

        if (CallIfExists(audioManager, "SetVolumes", bgm, se)) return;

        Debug.LogWarning("[BattleBootstrap] Could not apply audio volume automatically. (No matching method found on AudioManager)");
    }


    private static bool CallIfExists(object target, string methodName, params object[] args)
    {
        if (target == null) return false;

        var type = target.GetType();
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var m in methods)
        {
            if (m.Name != methodName) continue;

            var ps = m.GetParameters();
            if ((args == null ? 0 : args.Length) != ps.Length) continue;

            try
            {
                m.Invoke(target, args);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[BattleBootstrap] Method call failed: {type.Name}.{methodName} : {e.Message}");
                return false;
            }
        }

        return false;
    }

    private static bool TryGetIntField(object target, string[] fieldCandidates, out int value)
    {
        value = 0;
        if (target == null) return false;

        var t = target.GetType();
        foreach (var name in fieldCandidates)
        {
            var f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null && f.FieldType == typeof(int))
            {
                value = (int)f.GetValue(target);
                return true;
            }

            var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (p != null && p.PropertyType == typeof(int) && p.CanRead)
            {
                value = (int)p.GetValue(target);
                return true;
            }
        }
        return false;
    }
}
