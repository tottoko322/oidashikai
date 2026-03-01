using UnityEngine;

public enum CoinFace { Q, I }

[CreateAssetMenu(menuName = "Qpic/Data/CharacterData")]
public class CharacterData : ScriptableObject
{
    public string characterId;
    public string displayName;
    public Sprite portrait;

    [Header("Coin rule")]
    public CoinFace faceForFirst = CoinFace.Q; // ピコ=Q, ナノ=I

    [Header("Distributed Deck")]
    public DeckDefinition distributedDeck;
}
