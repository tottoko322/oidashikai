using UnityEngine;

public class TitleMenuController : MonoBehaviour
{
    public SceneFlowManager flow;

    public void OnDistributedDeckBattle()
    {
        ModeContext.I.mode = GameMode.DistributedDeckBattle;
        flow.GoCharacterSelect();
    }

    public void OnDeckBuildBattle()
    {
        ModeContext.I.mode = GameMode.DeckBuildBattle;
        flow.GoCharacterSelect();
    }

    public void OnRules()
    {
        flow.GoRules();
    }
}
