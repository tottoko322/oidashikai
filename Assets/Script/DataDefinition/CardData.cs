using UnityEngine;

[CreateAssetMenu(menuName = "Qpic/Data/CardData")]
public class CardData : ScriptableObject
{
    public string cardId;
    public string displayName;

    [Header("Stats")]
    public int cost;
    public int attack;
    public int defense;

    [Header("Effect")]
    public EffectData effect;

    [Header("Art")]
    public Sprite artwork;
}
