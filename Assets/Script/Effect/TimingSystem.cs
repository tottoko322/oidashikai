using System;

public class TimingSystem
{
    public event Action OnTurnStart;
    public event Action OnTurnEndBeforeCurse;

    public void FireTurnStart() => OnTurnStart?.Invoke();
    public void FireTurnEndBeforeCurse() => OnTurnEndBeforeCurse?.Invoke();
}
