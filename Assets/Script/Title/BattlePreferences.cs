using UnityEngine;

public static class BattlePreferences
{
    private const string DifficultyKey = "qpic_cpu_difficulty";
    public static bool HasDifficulty => PlayerPrefs.HasKey(DifficultyKey);

    public static CpuDifficulty GetDifficulty(CpuDifficulty fallback = CpuDifficulty.Normal)
    {
        int value = PlayerPrefs.GetInt(DifficultyKey, (int)fallback);
        return value >= (int)CpuDifficulty.Easy && value <= (int)CpuDifficulty.Hard
            ? (CpuDifficulty)value : fallback;
    }

    public static void SetDifficulty(CpuDifficulty difficulty)
    {
        if (difficulty < CpuDifficulty.Easy || difficulty > CpuDifficulty.Hard)
            difficulty = CpuDifficulty.Normal;
        PlayerPrefs.SetInt(DifficultyKey, (int)difficulty);
        PlayerPrefs.Save();
    }
}
