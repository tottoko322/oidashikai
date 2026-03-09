using UnityEngine;

public class WinLoseSystem : MonoBehaviour
{
    public SceneFlowManager flow;

    public void Win()
    {
        AudioManager.I?.PlayYouWin();
        Debug.Log("WIN");
        if (flow != null)
        {
            flow.GoTitle();
        }
    }

    public void Lose()
    {
        AudioManager.I?.PlayYouWin();
        Debug.Log("LOSE");
        if (flow != null)
        {
            flow.GoTitle();
        }
    }
}
