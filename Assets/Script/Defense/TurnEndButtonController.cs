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
        BattleStateMachine battle = BattleStateMachine.I;

        if (button == null)
            return;

        if (battle == null)
        {
            button.interactable = false;
            return;
        }

        bool inputLocked =
            InputLockManager.I != null &&
            InputLockManager.I.IsLocked;

        // ============================
        // 防御選択中
        // ============================

        if (battle.IsWaitingForDefense)
        {
            label.text = "Skip";

            // 防御待ちでもInputLock中なら押せない
            button.interactable =
                battle.BattleReady &&
                !inputLocked;

            return;
        }

        // ============================
        // 通常時
        // ============================

        label.text = "Turn End";

        bool canPress =
            battle.BattleReady &&
            battle.turnSystem.Current == TurnOwner.Player &&
            !inputLocked;

        button.interactable = canPress;
    }

    private void OnClick()
    {
        BattleStateMachine battle = BattleStateMachine.I;

        if (battle == null)
            return;

        // ロック中のクリックは絶対に通さない
        if (InputLockManager.I != null &&
            InputLockManager.I.IsLocked)
        {
            return;
        }

        battle.OnTurnEndButtonPressed();
    }
}