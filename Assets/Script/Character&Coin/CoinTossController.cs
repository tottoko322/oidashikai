using System.Collections;
using UnityEngine;

public class CoinTossController : MonoBehaviour
{
    public CoinUI ui;
    public FirstTurnDecider decider;

    public float tossDuration = 1.2f;
    public float resultHold = 0.8f;

    public CoinFace ResultFace { get; private set; }
    public bool PlayerFirst { get; private set; }

    public IEnumerator Play()
    {
        ui.SetTossing();
        yield return new WaitForSeconds(tossDuration);

        ResultFace = (Random.value < 0.5f) ? CoinFace.Q : CoinFace.I;

        var ch = SelectedCharacterContext.I.selected;
        PlayerFirst = decider.DecidePlayerFirst(ch, ResultFace);

        ui.SetResult(ResultFace, PlayerFirst);
        yield return new WaitForSeconds(resultHold);
    }
}
