using UnityEngine;

public class RulePanelController : MonoBehaviour
{
    public SceneFlowManager flow;
    public void OnBack() => flow.GoTitle();
}
