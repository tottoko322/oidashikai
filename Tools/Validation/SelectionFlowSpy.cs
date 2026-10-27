using System.Collections.Generic;

// Validation only: copy outside the Editor folder in the isolated test project.
public class SelectionFlowSpy : SceneFlowManager
{
    public readonly List<string> loaded = new List<string>();
    protected override void LoadScene(string sceneName) => loaded.Add(sceneName);
}
