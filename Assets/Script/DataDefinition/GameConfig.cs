using UnityEngine;

[CreateAssetMenu(menuName = "Qpic/Data/GameConfig")]
public class GameConfig : ScriptableObject
{
    [Header("Hand / Deck")]
    public int maxHand = 8;
    public int deckLimit = 15;
    public int sameCardLimit = 2;

    [Header("Battle")]
    public int startHandCount = 4;
    public int maxCost = 5;
    public int startCost = 0;

    [Header("Options")]
    public bool useConfirm = false;
}
