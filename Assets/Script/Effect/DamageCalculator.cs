using UnityEngine;

public static class DamageCalculator
{
    public static int Calc(int attack, int defense, float multiplier = 1f, bool pierce = false)
    {
        int baseDmg = pierce ? attack : Mathf.Max(0, attack - defense);
        int dmg = Mathf.FloorToInt(baseDmg * multiplier);
        return Mathf.Max(0, dmg);
    }
}
