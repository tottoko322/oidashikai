using System.Collections.Generic;
using UnityEngine;

public struct CpuAction
{
    public CardData card;
    public bool useEffect;
    public CpuAction(CardData card, bool useEffect) { this.card = card; this.useEffect = useEffect; }
}

// Only own cards and public battle values. Never receives the opponent's hidden cards.
public sealed class CpuDecisionContext
{
    public IReadOnlyList<CardData> hand;
    public int cost, maxCost, hp, maxHP, opponentHP, opponentMaxHP, deckRemaining;
    public int maxHand = 8;
    public float attackMultiplier = 1f, defenseMultiplier = 1f;
    public bool effectsBlocked;
}

public partial class EnemyAI
{
    public static CpuAction Decide(CpuDecisionContext state, CpuDifficulty difficulty)
    {
        var actions = new List<CpuAction>();
        if (state == null || state.hand == null) return default;
        foreach (var card in state.hand)
        {
            if (card == null || Mathf.Max(0, card.cost) > state.cost) continue;
            actions.Add(new CpuAction(card, false));
            if (!state.effectsBlocked && CanUseEffect(card, state.hand.Count - 1))
                actions.Add(new CpuAction(card, true));
        }
        if (actions.Count == 0) return default;
        if (difficulty == CpuDifficulty.Easy) return actions[Random.Range(0, actions.Count)];
        CpuAction best = default;
        float bestScore = 0f;
        foreach (var action in actions)
        {
            float score = Score(action, state, difficulty);
            if (score > bestScore) { bestScore = score; best = action; }
        }
        return best;
    }

    public static bool CanUseEffect(CardData card, int remainingHand)
    {
        if (card == null || card.effect == null || card.effect.type == EffectType.None ||
            card.effect.type == EffectType.BlockEffectsAndSelfDamageWhileInHand) return false;
        var type = card.effect.type;
        return remainingHand > 0 || (type != EffectType.DiscardAttackMinusOneDamage &&
            type != EffectType.DiscardDefenseMinusOneHeal);
    }

    private static float Score(CpuAction action, CpuDecisionContext s, CpuDifficulty difficulty)
    {
        var c = action.card;
        float damage = 0f, value = 0f;
        if (!action.useEffect) damage = Mathf.FloorToInt(Mathf.Max(0, c.attack) * s.attackMultiplier);
        else
        {
            var e = c.effect;
            int amount = Mathf.Max(0, e.valueA);
            switch (e.type)
            {
                case EffectType.Draw:
                    int draws = Mathf.Min(amount, Mathf.Max(0, s.maxHand - s.hand.Count + 1));
                    if (draws > s.deckRemaining) return -10000f;
                    value = draws * 1.25f; break;
                case EffectType.Heal:
                    value = Mathf.Min(amount, s.maxHP - s.hp) * (s.hp <= 3 ? 2f : 1.1f); break;
                case EffectType.DealDamage: damage = amount; break;
                case EffectType.AddCost:
                    value = Mathf.Min(amount, s.maxCost - s.cost + Mathf.Max(0, c.cost)) * 0.8f; break;
                case EffectType.MultiplyNextDamage:
                    value = Mathf.Max(0f, Mathf.Max(1f, e.valueF) - s.attackMultiplier) * 1.6f;
                    if (difficulty == CpuDifficulty.Hard)
                    {
                        int nextAttack = 0;
                        bool skipped = false;
                        foreach (var next in s.hand)
                        {
                            if (!skipped && next == c) { skipped = true; continue; }
                            if (next != null && next.cost <= Mathf.Min(s.maxCost, s.cost - Mathf.Max(0, c.cost) + 1))
                                nextAttack = Mathf.Max(nextAttack, next.attack);
                        }
                        value = Mathf.Max(0f, Mathf.Max(1f, e.valueF) - s.attackMultiplier) * nextAttack * 0.9f;
                    }
                    break;
                case EffectType.PierceDefense:
                    value = Mathf.Max(0f, Mathf.Max(1f, e.valueF) - s.defenseMultiplier) * 1.2f; break;
                case EffectType.DiscardAttackMinusOneDamage:
                case EffectType.DiscardDefenseMinusOneHeal:
                    var discarded = ChooseDiscard(s.hand, c, e.type, difficulty);
                    if (discarded == null) return -1000f;
                    if (e.type == EffectType.DiscardAttackMinusOneDamage) damage = Mathf.Max(0, discarded.attack - 1);
                    else value = Mathf.Min(Mathf.Max(0, discarded.defense - 1), s.maxHP - s.hp) * 1.2f;
                    value -= 0.7f; break;
                case EffectType.HandCountMinusTwoDamage: damage = Mathf.Max(0, s.hand.Count - 3); break;
                case EffectType.HealAndSelfDamageNextTurnEnd:
                    int self = Mathf.Max(0, Mathf.RoundToInt(e.valueF));
                    int healedHP = Mathf.Min(s.maxHP, s.hp + amount);
                    if (self >= healedHP) return -10000f;
                    value = (healedHP - s.hp) * 1.2f - self * 0.8f; break;
                case EffectType.ReduceEnemyMaxHP:
                    int reducedMax = Mathf.Max(1, s.opponentMaxHP - amount);
                    value = Mathf.Max(0, s.opponentHP - reducedMax) + (s.opponentMaxHP - reducedMax) * 0.35f; break;
            }
        }
        if (damage >= s.opponentHP && damage > 0) return 10000f + damage;
        value += damage;
        value -= Mathf.Max(0, c.cost) * 0.12f;
        if (difficulty == CpuDifficulty.Hard) value -= Mathf.Max(0, c.defense) * 0.08f;
        return value;
    }

    public static CardData ChooseDefense(IReadOnlyList<CardData> hand, int incoming, float multiplier,
        int hp, CpuDifficulty difficulty)
    {
        if (hand == null || incoming <= 0) return null;
        var candidates = new List<CardData>();
        foreach (var card in hand) if (card != null && card.defense > 0) candidates.Add(card);
        if (candidates.Count == 0) return null;
        if (difficulty == CpuDifficulty.Easy)
            return Random.value < 0.2f ? null : candidates[Random.Range(0, candidates.Count)];
        CardData best = null;
        float bestScore = 0;
        foreach (var c in candidates)
        {
            int blocked = Mathf.Min(incoming, Mathf.FloorToInt(c.defense * multiplier));
            float score = blocked;
            if (incoming >= hp && incoming - blocked < hp) score += 1000f;
            if (difficulty == CpuDifficulty.Hard)
                score -= c.attack * 0.12f + Mathf.Max(0, c.defense * multiplier - incoming) * 0.1f;
            else score -= c.attack * 0.01f;
            if (score > bestScore) { best = c; bestScore = score; }
        }
        return best;
    }

    public static CardData ChooseDiscard(IReadOnlyList<CardData> hand, CardData excluded,
        EffectType type, CpuDifficulty difficulty)
    {
        var candidates = new List<CardData>();
        bool skipped = false;
        foreach (var card in hand)
        {
            if (!skipped && card == excluded) { skipped = true; continue; }
            if (card != null) candidates.Add(card);
        }
        if (candidates.Count == 0) return null;
        if (difficulty == CpuDifficulty.Easy) return candidates[Random.Range(0, candidates.Count)];
        CardData best = null;
        float bestScore = float.NegativeInfinity;
        foreach (var card in candidates)
        {
            float score = type == EffectType.DiscardAttackMinusOneDamage ? card.attack : card.defense;
            if (difficulty == CpuDifficulty.Hard)
                score -= (type == EffectType.DiscardAttackMinusOneDamage ? card.defense : card.attack) * 0.15f;
            if (score > bestScore) { best = card; bestScore = score; }
        }
        return best;
    }
}
