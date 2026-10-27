using System.Collections.Generic;
using UnityEngine;

// Hidden cards stay as data rather than interactive board objects.
public sealed class EnemyBattleState
{
    private readonly List<CardData> deck = new List<CardData>();
    private int drawIndex;
    public readonly List<CardData> hand = new List<CardData>();
    public int Cost { get; private set; }
    public int MaxCost { get; private set; }
    public int MaxHand { get; private set; }
    public int Remaining => deck.Count - drawIndex;
    public float attackMultiplier = 1f, defenseMultiplier = 1f;
    public int selfDamageThisTurnEnd, selfDamageNextTurnEnd;
    public bool Setup(DeckDefinition definition, GameConfig config)
    {
        deck.Clear(); hand.Clear(); drawIndex = 0;
        attackMultiplier = defenseMultiplier = 1f;
        selfDamageThisTurnEnd = selfDamageNextTurnEnd = 0;
        if (definition == null || definition.entries == null || config == null) return false;
        var counts = new Dictionary<CardData, int>();
        foreach (var entry in definition.entries)
        {
            if (entry == null || entry.card == null || entry.count <= 0) continue;
            counts.TryGetValue(entry.card, out int count);
            count += entry.count;
            if (count > config.sameCardLimit) return false;
            counts[entry.card] = count;
            for (int i = 0; i < entry.count; i++) deck.Add(entry.card);
        }
        if (deck.Count == 0 || deck.Count > config.deckLimit) return false;
        MaxCost = Mathf.Max(0, config.maxCost);
        Cost = Mathf.Clamp(config.startCost, 0, MaxCost);
        MaxHand = Mathf.Max(0, config.maxHand);
        DeckManager.Shuffle(deck);
        return true;
    }
    // Full hands skip drawing, matching the player's CardDealAnimator.
    public bool TryDraw()
    {
        if (hand.Count >= MaxHand) return true;
        if (Remaining == 0) return false;
        hand.Add(deck[drawIndex++]);
        return true;
    }
    public void StartTurn() => AddCost(1);
    public void AddCost(int amount) => Cost = Mathf.Clamp(Cost + amount, 0, MaxCost);
    public bool TryPay(int amount)
    {
        amount = Mathf.Max(0, amount);
        if (Cost < amount) return false;
        Cost -= amount; return true;
    }
    public bool HasHandRestriction()
    {
        foreach (var card in hand)
            if (card != null && card.effect != null &&
                card.effect.type == EffectType.BlockEffectsAndSelfDamageWhileInHand) return true;
        return false;
    }
}
