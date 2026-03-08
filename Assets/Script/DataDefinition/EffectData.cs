using UnityEngine;

public enum EffectType
{
    None,
    Draw,
    Heal,
    DealDamage,
    AddCost,
    MultiplyNextDamage,
    PierceDefense,

    DiscardAttackMinusOneDamage,
    DiscardDefenseMinusOneHeal,
    HandCountMinusTwoDamage,

    HealAndSelfDamageNextTurnEnd,
    ReduceEnemyMaxHP,
    BlockEffectsAndSelfDamageWhileInHand

}


public enum EffectTiming
{
    Immediate,
    TurnStart,
    TurnEndBeforeCurse
}

[CreateAssetMenu(menuName = "Qpic/Data/EffectData")]
public class EffectData : ScriptableObject
{
    public string effectId;

    public EffectType type = EffectType.None;
    public EffectTiming timing = EffectTiming.Immediate;

    [Header("Values")]
    public int valueA = 0;
    public float valueF = 1f;

    [Header("Reference (effect decides)")]
    public RefMode refMode = RefMode.None;
    public RefSource refSource = RefSource.None;
    public SelectionRule selectionRule;
}
