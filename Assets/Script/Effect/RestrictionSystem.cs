public class RestrictionSystem
{
    // 進捗確認カードなど「効果を使えない」制限に対応する枠
    public bool IsEffectBlocked { get; private set; }

    public void SetEffectBlocked(bool blocked) => IsEffectBlocked = blocked;
}
