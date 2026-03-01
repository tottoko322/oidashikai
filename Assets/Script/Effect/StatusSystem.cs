using System.Collections.Generic;

public class StatusSystem
{
    // 超最小：ターン終了前の自傷/呪いなどを積む枠
    private readonly List<int> pendingSelfDamage = new();

    public void AddSelfDamageAtTurnEnd(int amount)
    {
        if (amount > 0) pendingSelfDamage.Add(amount);
    }

    public int ConsumeTurnEndSelfDamage()
    {
        int sum = 0;
        for (int i = 0; i < pendingSelfDamage.Count; i++) sum += pendingSelfDamage[i];
        pendingSelfDamage.Clear();
        return sum;
    }
}
