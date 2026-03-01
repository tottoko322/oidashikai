using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFlowManager : MonoBehaviour
{
    public void GoTitle() => SceneManager.LoadScene("TitleScene");
    public void GoRules() => SceneManager.LoadScene("RuleScene");
    public void GoCharacterSelect() => SceneManager.LoadScene("CharacterSelectScene");
    public void GoDeckBuild() => SceneManager.LoadScene("DeckBuildScene");
    public void GoBattle() => SceneManager.LoadScene("BattleScene");
}
