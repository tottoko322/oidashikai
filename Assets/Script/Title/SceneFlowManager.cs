using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFlowManager : MonoBehaviour
{
    private static string pendingScene;

    private void Start() => BrandIntroUI.EnsureForScene(gameObject.scene);

    public void GoTitle() => Navigate("title");
    public void GoRules() => Navigate("Rule");
    // The existing Start button calls this method directly in the title scene.
    public void GoCharacterSelect()
    {
        if (pendingScene == null) DifficultySelectUI.Open(this);
    }
    public void GoDifficultySelect() => GoCharacterSelect();
    public void ContinueToCharacterSelect() => Navigate("CharacterSelect");
    public void GoDeckBuild() => Navigate("DeckBuild");
    public void GoBattle()
    {
        if (SelectedCharacterContext.I == null || SelectedCharacterContext.I.selected == null) return;
        Navigate("Battle");
    }
    public void GoDic() => Navigate("Dic");

    private void Navigate(string sceneName)
    {
        // CharacterSelect's confirm button has two scene-loading listeners.
        // Share the guard across managers so only one transition is accepted.
        if (pendingScene != null) return;
        pendingScene = sceneName;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        try { LoadScene(sceneName); }
        catch { pendingScene = null; throw; }
    }

    protected virtual void LoadScene(string sceneName) => SceneManager.LoadScene(sceneName);

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (string.Equals(scene.name, pendingScene, System.StringComparison.OrdinalIgnoreCase))
            pendingScene = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetTransition()
    {
        pendingScene = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}

