using UnityEngine;

public enum GameMode
{
    DistributedDeckBattle,
    DeckBuildBattle
}

public class ModeContext : MonoBehaviour
{
    public static ModeContext I { get; private set; }
    public GameMode mode = GameMode.DistributedDeckBattle;

    private void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }
}
