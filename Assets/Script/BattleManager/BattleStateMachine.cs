using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleStateMachine : MonoBehaviour
{
    public GameConfig config;

    [Header("Systems")]
    public WinLoseSystem winLose;
    public CoinTossController coinToss;
    public CostManager costManager;

    [Header("Deck/Hand")]
    public DeckManager deckManager;
    public HandManager handManager;
    public CardDealAnimator cardDealAnimator;

    [Header("UI")]
    public HandLayoutController handLayout;

    [Header("Turn")]
    public TurnSystem turnSystem = new TurnSystem();

    private IEnumerator Start()
    {
        // Confirm設定反映
        if (SaveManager.I != null)
            config.useConfirm = SaveManager.I.GetConfirm(config.useConfirm);

        // コイントス（スキップ不可）
        yield return coinToss.Play();
        bool playerFirst = coinToss.PlayerFirst;
        turnSystem.SetFirst(playerFirst ? TurnOwner.Player : TurnOwner.Enemy);

        // デッキ準備
        if (!deckManager.SetupFromModeAndCharacter())
        {
            winLose.Lose();
            yield break;
        }

        // コスト初期化
        costManager.Init(config.startCost, config.maxCost);

        // 初期手札 4枚（シュッ×4）
        yield return cardDealAnimator.DealStartHand(config.startHandCount);

        // 手札レイアウト更新
        handLayout.Rebuild();

        Debug.Log("Battle Ready (next: TurnStart)");
        // ここから TurnStart→行動→防御→解決…を追加していく予定だよ
    }
}
