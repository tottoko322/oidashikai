using UnityEngine;

public enum RefSource { None, Hand, Discard, Deck }
public enum RefMode { None, Self, ChooseOne, RandomOne, Top, LastPlayed }

[System.Serializable]
public class SelectionRule
{
    public bool isOptional = false; // キャンセル可
    public int minSelect = 1;
    public int maxSelect = 1;
    public RefSource source = RefSource.Hand;
}
