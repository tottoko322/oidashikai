using UnityEngine;

public partial class BattleStateMachine
{
    public BattleHistory History { get; } = new BattleHistory();
    public BattleHistoryUI HistoryUI { get; private set; }
    public bool IsHistoryOpen => HistoryUI != null && HistoryUI.IsOpen;

    private void SetupHistoryUI()
    {
        var canvas = playerHpText != null ? playerHpText.canvas : null;
        if (canvas == null) return;
        HistoryUI = gameObject.AddComponent<BattleHistoryUI>();
        HistoryUI.Initialize(this, canvas);
    }

    public void RecordPlayerDraw() => History.Draw(BattleSide.Player, 1);

    private bool DrawCpuCard()
    {
        int before = enemyState.hand.Count;
        bool success = enemyState.TryDraw();
        History.Draw(BattleSide.Enemy, enemyState.hand.Count - before);
        return success;
    }

    private void RecordUsedCard(bool enemy, CardData card, DiscardReason reason, string detail = "")
        => History.MoveToDiscard(enemy ? BattleSide.Enemy : BattleSide.Player, card, reason, detail);

    private void RecordHP(bool enemy, int before, int after, string action)
        => History.Record(enemy ? BattleSide.Enemy : BattleSide.Player, $"{action} / HP {before} → {after}");
}
