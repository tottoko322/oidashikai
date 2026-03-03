using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public List<CardData> runtimeDeck = new();
    private int index;

    [Header("Refs")]
    public WinLoseSystem winLose;

    public int Remaining => runtimeDeck.Count - index;

    public bool SetupFromModeAndCharacter()
    {
        runtimeDeck.Clear();
        index = 0;

        if (ModeContext.I.mode == GameMode.DeckBuildBattle)
        {
            if (DeckRuntimeContext.I != null && DeckRuntimeContext.I.builtDeck.Count > 0)
            {
                runtimeDeck.AddRange(DeckRuntimeContext.I.builtDeck);
            }
        }

        if (runtimeDeck.Count == 0)
        {
            var ch = SelectedCharacterContext.I.selected;
            if (ch == null || ch.distributedDeck == null) return false;

            foreach (var e in ch.distributedDeck.entries)
                for (int i = 0; i < e.count; i++)
                    runtimeDeck.Add(e.card);
        }

        Shuffle(runtimeDeck);
        return runtimeDeck.Count > 0;
    }

    public bool TryDraw(out CardData card)
    {
        if (Remaining <= 0)
        {
            card = null;
            return false; // ドロー要求時に0なら敗北
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
