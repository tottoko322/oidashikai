public class RestrictionSystem
{
    public bool IsEffectBlocked { get; private set; }

    public void SetEffectBlocked(bool blocked) => IsEffectBlocked = blocked;
}
