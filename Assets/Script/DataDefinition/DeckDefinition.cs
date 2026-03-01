using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DeckEntry
{
    public CardData card;
    [Range(1, 9)] public int count = 1;
}

[CreateAssetMenu(menuName = "Qpic/Data/DeckDefinition")]
public class DeckDefinition : ScriptableObject
{
    public string deckId;
    public List<DeckEntry> entries = new();
}
