using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFlowManager : MonoBehaviour
{
    public void GoTitle() => SceneManager.LoadScene("Title");
    public void GoRules() => SceneManager.LoadScene("Rule");
    public void GoCharacterSelect() => SceneManager.LoadScene("CharacterSelect");
    public void GoDeckBuild() => SceneManager.LoadScene("DeckBuild");
    public void GoBattle() => SceneManager.LoadScene("Battle");
}
