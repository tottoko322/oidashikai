using UnityEngine;

public class CostManager : MonoBehaviour
{
    public int Current { get; private set; }
    public int Max { get; private set; }

    public void Init(int start, int max)
    {
        Max = max;
        Current = Mathf.Clamp(start, 0, Max);
    }

    public void OnTurnStartGainOne()
    {
        Current = Mathf.Min(Max, Current + 1);
    }

    public bool TryPay(int cost)
    {
        if (Current < cost) return false;
        Current -= cost;
        return true;
    }

    public void Add(int amount)
    {
        Current = Mathf.Clamp(Current + amount, 0, Max);
    }
}
