using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TurnEndButtonController : MonoBehaviour
{
    public Button button;
    public TMP_Text label;

    private bool idleOnly;

    private void Awake()
    {
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }
    }

    private void Update()
    {
        Refresh();
    }

    public void SetIdleOnly(bool value)
    {
        idleOnly = value;
    }

    private void Refresh()
    {
        var battle = BattleStateMachine.I;
        if (battle == null)
        {
            button.interactable = false;
            return;
        }

        // ★ 防御中は常に押せる
        if (battle.IsWaitingForDefense)
        {
            button.interactable = true;
            label.text = "Skip";
            return;
        }

        bool canPress =
            battle.BattleReady &&
            battle.turnSystem.Current == TurnOwner.Player &&
            !(InputLockManager.I != null && InputLockManager.I.IsLocked);

        button.interactable = canPress;
        label.text = "Turn End";
    }

    private void OnClick()
    {
        BattleStateMachine.I?.OnTurnEndButtonPressed();
    }
}