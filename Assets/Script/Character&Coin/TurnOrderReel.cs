using UnityEngine;

// A continuous reel: even rows are First and odd rows are Second.
// The target is chosen once so the last visible row and the battle result agree.
public static class TurnOrderReel
{
    public static bool IsFirstRow(int index) => (index & 1) == 0;

    public static int StopIndex(bool playerFirst, int minimumRows)
    {
        int rows = Mathf.Clamp(minimumRows, 6, 60);
        if (IsFirstRow(rows) != playerFirst) rows++;
        return rows;
    }

    public static float Position(float time, int stopIndex)
    {
        float remaining = 1f - Mathf.Clamp01(time);
        return stopIndex * (1f - remaining * remaining * remaining);
    }
}
