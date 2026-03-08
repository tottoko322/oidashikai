using UnityEngine;

public class WinLoseSystem : MonoBehaviour
{
    public SceneFlowManager flow;

    public void Win()
    {
        Debug.Log("WIN");
        if (flow != null)
        {
            flow.GoTitle();
        }
    }

    public void Lose()
    {
        Debug.Log("LOSE");
        if (flow != null)
        {
            flow.GoTitle();
        }
    }
}
