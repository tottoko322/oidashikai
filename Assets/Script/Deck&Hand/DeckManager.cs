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

    public bool SetupFromDefinition(DeckDefinition definition)
    {
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

        Debug.Log("[DeckManager] definition name = " + definition.name);
        Debug.Log("[DeckManager] entry count = " + definition.entries.Count);

        int totalRequested = 0;

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

            int count = entry.count;

            Debug.Log($"[DeckManager] Entry {e} / Card = {entry.card.displayName} / Count = {count}");

            if (count <= 0)
            {
                Debug.LogWarning($"[DeckManager] Entry {e} ({entry.card.displayName}) has count <= 0, so it will not be added.");
                continue;
            }

            totalRequested += count;

            for (int i = 0; i < count; i++)
            {
                runtimeDeck.Add(entry.card);
            }
        }

        Shuffle(runtimeDeck);

        Debug.Log($"[DeckManager] SetupFromDefinition complete. Requested = {totalRequested} / Deck count = {runtimeDeck.Count}");
        return runtimeDeck.Count > 0;
    }

    public bool SetupFromModeAndCharacter()
    {
        runtimeDeck.Clear();
        index = 0;

        if (forcedDeckDefinition != null && forcedDeckDefinition.entries != null && forcedDeckDefinition.entries.Count > 0)
        {
            Debug.Log($"[DeckManager] Using forced deck: {forcedDeckDefinition.name}");
            return SetupFromDefinition(forcedDeckDefinition);
        }

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

        if (SelectedCharacterContext.I == null)
        {
            Debug.LogError("[DeckManager] SelectedCharacterContext.I is null.");
            return false;
        }

        var ch = SelectedCharacterContext.I.selected;
        if (ch == null)
        {
            Debug.LogError("[DeckManager] selected character is null.");
            return false;
        }

        if (ch.distributedDeck == null)
        {
            Debug.LogError("[DeckManager] selected character's distributedDeck is null.");
            return false;
        }

        if (ch.distributedDeck.entries == null || ch.distributedDeck.entries.Count == 0)
        {
            Debug.LogError("[DeckManager] selected character deck entries are empty.");
            return false;
        }

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

        card = runtimeDeck[index];
        index++;
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
