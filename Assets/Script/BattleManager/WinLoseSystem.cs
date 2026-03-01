using UnityEngine;

public class WinLoseSystem : MonoBehaviour
{
    public SceneFlowManager flow;

    public void Win()
    {
        Debug.Log("WIN");
        flow.GoTitle();
    }

    public void Lose()
    {
        Debug.Log("LOSE");
        flow.GoTitle();
    }
}
