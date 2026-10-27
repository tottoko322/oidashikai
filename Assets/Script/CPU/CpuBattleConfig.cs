using UnityEngine;

public enum CpuDifficulty { Easy, Normal, Hard }

[CreateAssetMenu(menuName = "Qpic/CPU/Battle Config")]
public class CpuBattleConfig : ScriptableObject
{
    public DeckDefinition defaultDeck;
    public DeckDefinition easyDeck;
    public DeckDefinition normalDeck;
    public DeckDefinition hardDeck;
    [Header("Card reveal (seconds)")]
    [Min(0.01f)] public float enterDuration = 0.16f;
    [Min(0f)] public float holdDuration = 1.5f;
    [Min(0.01f)] public float returnDuration = 0.18f;
    public Vector2 revealPosition = new Vector2(-500f, 300f);
    public float revealScale = 0.7f;
    public DeckDefinition DeckFor(CpuDifficulty difficulty)
    {
        var selected = difficulty == CpuDifficulty.Easy ? easyDeck
            : difficulty == CpuDifficulty.Hard ? hardDeck : normalDeck;
        return selected != null ? selected : defaultDeck;
    }
}
