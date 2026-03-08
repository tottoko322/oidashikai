using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public List<CardData> runtimeDeck = new();
    private int index;

    [Header("Refs")]
    public WinLoseSystem winLose;

    [Header("Debug / Override")]
    public DeckDefinition forcedDeckDefinition;

    public int Remaining => runtimeDeck.Count - index;

    // 追加：指定DeckDefinitionから直接デッキを作る
    public bool SetupFromDefinition(DeckDefinition definition)
    {
        Debug.Log("[DeckManager] definition name = " + definition.name);
        Debug.Log("[DeckManager] entry count = " + definition.entries.Count);

        runtimeDeck.Clear();
        index = 0;

        if (definition == null)
        {
            Debug.LogError("[DeckManager] SetupFromDefinition: definition is null.");
            return false;
        }

        if (definition.entries == null || definition.entries.Count == 0)
        {
            Debug.LogError("[DeckManager] SetupFromDefinition: entries are empty.");
            return false;
        }

        for (int e = 0; e < definition.entries.Count; e++)
        {
            var entry = definition.entries[e];
            if (entry == null)
            {
                Debug.LogWarning($"[DeckManager] Entry {e} is null.");
                continue;
            }

            if (entry.card == null)
            {
                Debug.LogWarning($"[DeckManager] Entry {e} card is null.");
                continue;
            }

            int count = Mathf.Max(0, entry.count);
            Debug.Log($"[DeckManager] Add {entry.card.displayName} x{count}");

            for (int i = 0; i < count; i++)
            {
                runtimeDeck.Add(entry.card);
            }
        }

        Shuffle(runtimeDeck);

        Debug.Log($"[DeckManager] SetupFromDefinition complete. Deck count = {runtimeDeck.Count}");
        return runtimeDeck.Count > 0;
    }

    public bool SetupFromModeAndCharacter()
    {
        runtimeDeck.Clear();
        index = 0;

        // まずは BattleBootstrap / BattleStateMachine から渡された強制デッキを最優先
        if (forcedDeckDefinition != null && forcedDeckDefinition.entries != null && forcedDeckDefinition.entries.Count > 0)
        {
            Debug.Log($"[DeckManager] Using forced deck: {forcedDeckDefinition.name}");
            return SetupFromDefinition(forcedDeckDefinition);
        }

        // 構築デッキバトル時
        if (ModeContext.I != null && ModeContext.I.mode == GameMode.DeckBuildBattle)
        {
            if (DeckRuntimeContext.I != null && DeckRuntimeContext.I.builtDeck.Count > 0)
            {
                runtimeDeck.AddRange(DeckRuntimeContext.I.builtDeck);
                Shuffle(runtimeDeck);
                Debug.Log($"[DeckManager] Using built deck. Deck count = {runtimeDeck.Count}");
                return runtimeDeck.Count > 0;
            }
        }

        // 配布デッキ
        if (SelectedCharacterContext.I == null) return false;

        var ch = SelectedCharacterContext.I.selected;
        if (ch == null || ch.distributedDeck == null) return false;
        if (ch.distributedDeck.entries == null || ch.distributedDeck.entries.Count == 0) return false;

        bool ok = SetupFromDefinition(ch.distributedDeck);
        Debug.Log($"[DeckManager] Using selected character deck. Deck count = {runtimeDeck.Count}");
        return ok;
    }

    public bool TryDraw(out CardData card)
    {
        if (Remaining <= 0)
        {
            card = null;
            return false;
        }

        card = runtimeDeck[index++];
        return true;
    }

    public static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
