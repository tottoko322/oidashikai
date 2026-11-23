using System;
using System.Collections.Generic;

public enum BattleSide { Player, Enemy, System }
public enum DiscardReason { Attack, Effect, Defense, Discard }

public sealed class BattleLogEntry
{
    public int Turn { get; }
    public BattleSide Side { get; }
    public string Message { get; }
    public BattleLogEntry(int turn, BattleSide side, string message)
    { Turn = turn; Side = side; Message = message; }
}

public sealed class DiscardEntry
{
    public int Turn { get; }
    public BattleSide Side { get; }
    public CardData Card { get; }
    public DiscardReason Reason { get; }
    public DiscardEntry(int turn, BattleSide side, CardData card, DiscardReason reason)
    { Turn = turn; Side = side; Card = card; Reason = reason; }
}

// Match-owned public information only. Drawing never records card identities.
// A new match owns a fresh instance, independent of presentation or networking.
public sealed class BattleHistory
{
    private readonly List<BattleLogEntry> logs = new List<BattleLogEntry>();
    private readonly List<DiscardEntry> discards = new List<DiscardEntry>();
    public IReadOnlyList<BattleLogEntry> Logs => logs.AsReadOnly();
    public IReadOnlyList<DiscardEntry> Discards => discards.AsReadOnly();
    public int Turn { get; private set; }
    public bool Finished { get; private set; }
    public event Action Changed;

    public void BeginTurn(BattleSide side)
    {
        if (Finished) return;
        Turn++;
        Record(side, "ターン開始");
    }

    public void Record(BattleSide side, string message)
    {
        if (Finished || string.IsNullOrEmpty(message)) return;
        logs.Add(new BattleLogEntry(Turn, side, message));
        Changed?.Invoke();
    }

    // Each call represents one physical card, even if copies share CardData.
    public void MoveToDiscard(BattleSide side, CardData card, DiscardReason reason, string detail = "")
    {
        if (Finished || card == null || side == BattleSide.System) return;
        discards.Add(new DiscardEntry(Turn, side, card, reason));
        Record(side, $"「{CardName(card)}」: {ReasonName(reason)}" +
            (string.IsNullOrEmpty(detail) ? "" : " / " + detail));
    }

    public void Draw(BattleSide side, int count)
    { if (count > 0) Record(side, $"{count}枚ドロー"); }

    public void Finish(bool playerWon, string reason)
    {
        if (Finished) return;
        Record(BattleSide.System, (playerWon ? "WIN" : "LOSE") + " / " + reason);
        Finished = true;
    }

    public int DiscardCount(BattleSide side)
    {
        int count = 0;
        foreach (var entry in discards) if (entry.Side == side) count++;
        return count;
    }

    public static string SideName(BattleSide side) => side == BattleSide.Player ? "じぶん"
        : side == BattleSide.Enemy ? "あいて" : "システム";
    public static string ReasonName(DiscardReason reason) => reason == DiscardReason.Attack ? "コウゲキ"
        : reason == DiscardReason.Defense ? "ボウギョ" : reason == DiscardReason.Effect ? "こうか" : "すてたカード";
    public static string CardName(CardData card)
    {
        if (card == null) return "カード";
        if (!string.IsNullOrWhiteSpace(card.encyclopediaDescription))
            return card.encyclopediaDescription.Split('\n')[0].Trim();
        return string.IsNullOrWhiteSpace(card.displayName) ? "カード" : card.displayName;
    }
}
