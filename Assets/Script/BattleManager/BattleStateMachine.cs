using System.Collections;
using UnityEngine;
using TMPro;

public partial class BattleStateMachine : MonoBehaviour
{
    public static BattleStateMachine I { get; private set; }

    [Header("Config")]
    public GameConfig config;

    [Header("Systems")]
    public WinLoseSystem winLose;
    public CoinTossController coinToss;
    public CostManager costManager;
    public EffectSystem effectSystem;

    [Header("Deck / Hand")]
    public DeckDefinition deckDefinition;
    public DeckManager deckManager;
    public HandManager handManager;
    public CardDealAnimator cardDealAnimator;

    [Header("UI")]
    public HandLayoutController handLayout;
    public TurnBannerController turnBanner;
    public TurnEndButtonController turnEndButton;
    public DefenseSelectUI defenseSelectUI;
    public ActiveEffectDisplay playerActiveEffectDisplay;
    public TMP_Text playerHpText;
    public TMP_Text enemyHpText;
    public BattleEffectPopupUI effectPopupUI;
    public CharacterEffectPanelUI characterEffectPanelUI;

    [Header("Extra UI")]
    public TMP_Text gameStartText;
    public TMP_Text deckCountText;
    public TMP_Text enemyActionText;

    [Header("VFX")]
    public CharacterHitVfx playerHitVfx;
    public CharacterHitVfx enemyHitVfx;

    [Header("Turn")]
    public TurnSystem turnSystem = new TurnSystem();

    [Header("Battle Values")]
    public int playerStartHP = 10;
    public int enemyStartHP = 10;
    public int playerMaxHP = 10;
    public int enemyMaxHP = 10;

    [Header("CPU Timing")]
    public float enemyThinkTime = 0.8f;

    [Header("Center Message")]
    public TMP_Text centerMessageText;
    public float centerMessageDuration = 1.0f;
    public bool playBgmAfterBattleStart = true;

    private bool firstTurnIntroSkipped = false;

    public int PlayerHP { get; private set; }
    public int EnemyHP { get; private set; }
    public int TurnCount { get; private set; } = 1;
    public bool BattleReady { get; private set; }
    public bool PlayerActionUsed { get; private set; }
    public bool IsWaitingForDefense => waitingForDefense;
    public bool IsWaitingForDiscardSelect => waitingForDiscardSelect;

    private bool resolvingAction;
    private bool waitingForDefense;
    private bool waitingForDiscardSelect;
    private bool defenseSkipped;
    private bool turnTransitioning;

    private CardView selectedDiscardCard;

    // 配信などの自傷予約
    // this  : 今回の自分ターン終了時に発動
    // next  : 次の自分ターン終了時に発動
    private int pendingSelfDamageThisPlayerTurnEnd = 0;
    private int pendingSelfDamageNextPlayerTurnEnd = 0;

    // 次の攻撃 / 次の防御
    private float pendingNextAttackMultiplier = 1f;
    private float pendingNextDefenseMultiplier = 1f;

    private bool firstPlayerTurnStarted = false;
    private int initialEnemyMaxHP;
    private CardView pendingDefenseCard;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;
    }

    private void RefreshDeckUI()
    {
        if (deckCountText != null && deckManager != null)
        {
            deckCountText.text = $"やまふだ {deckManager.Remaining}";
        }
    }

    private void ShowEnemyAction(string message)
    {
        if (enemyActionText != null)
        {
            enemyActionText.text = "";
            enemyActionText.enabled = false;
        }
    }

    private IEnumerator ShowGameStart()
    {
        if (gameStartText == null) yield break;

        gameStartText.gameObject.SetActive(true);
        gameStartText.text = "GAME START";
        yield return new WaitForSeconds(1.2f);
        gameStartText.gameObject.SetActive(false);
    }

    private bool HasProgressCheckInHand()
    {
        if (handManager == null) return false;

        for (int i = 0; i < handManager.handViews.Count; i++)
        {
            var v = handManager.handViews[i];
            if (v == null || v.Data == null || v.Data.effect == null) continue;

            if (v.Data.effect.type == EffectType.BlockEffectsAndSelfDamageWhileInHand)
                return true;
        }

        return false;
    }

    private bool IsEffectCardBlocked()
    {
        return HasProgressCheckInHand();
    }

    private IEnumerator Start()
    {
        ApplyCharacterSelection();
        initialEnemyMaxHP = enemyMaxHP;
        initialPlayerMaxHP = playerMaxHP;
        // Include the panel even when its Awake has already hidden its root.
        if (characterEffectPanelUI == null)
        {
            foreach (var panel in FindObjectsByType<CharacterEffectPanelUI>(
                FindObjectsInactive.Include))
            {
                if (panel.gameObject.scene == gameObject.scene)
                {
                    characterEffectPanelUI = panel;
                    break;
                }
            }
        }
        characterEffectPanelUI?.Initialize(this);
        RefreshActiveEffectUI();

        BattleReady = false;
        PlayerActionUsed = false;
        resolvingAction = false;
        waitingForDefense = false;
        waitingForDiscardSelect = false;
        firstPlayerTurnStarted = false;
        pendingDefenseCard = null;
        defenseSkipped = false;
        turnTransitioning = false;

        if (config == null || winLose == null || costManager == null || deckManager == null || handManager == null || cardDealAnimator == null)
        {
            Debug.LogError("[BattleStateMachine] Required reference is missing.");
            yield break;
        }

        if (SaveManager.I != null)
            config.useConfirm = SaveManager.I.GetConfirm(config.useConfirm);

        handManager.config = config;

        PlayerHP = Mathf.Clamp(playerStartHP, 0, playerMaxHP);
        EnemyHP = Mathf.Clamp(enemyStartHP, 0, enemyMaxHP);
        RefreshHpUI();

        TurnCount = 1;
        ShowEnemyAction("");

        bool playerFirst = true;
        InputLockManager.I?.Lock();
        if (coinToss != null)
        {
            yield return coinToss.Play();
            if (!coinToss.ResultReady) yield break;
            playerFirst = coinToss.PlayerFirst;
        }
        turnSystem.SetFirst(playerFirst ? TurnOwner.Player : TurnOwner.Enemy);

        handManager.ClearHand();
        handLayout?.Rebuild();

        bool deckOk = deckManager.SetupForBattle(deckDefinition);
        if (!deckOk)
        {
            Debug.LogError("[BattleStateMachine] Deck setup failed. Check deckDefinition entries.");
            BattleReady = false;
            winLose.Lose();
            yield break;
        }

        RefreshDeckUI();

        costManager.Init(config.startCost, config.maxCost);

        yield return cardDealAnimator.DealStartHand(config.startHandCount);
        if (cardDealAnimator.LastDrawFailed) yield break;
        if (!SetupCpu()) yield break;
        handLayout?.Rebuild();
        RefreshDeckUI();

        BattleReady = true;

        yield return ShowCenterMessage("Battle Start!");

        if (playBgmAfterBattleStart)
        {
            // AudioManager側にBGM開始関数がある場合だけ使ってください
            // AudioManager.I?.PlayBgm();
        }

        firstTurnIntroSkipped = true;
        yield return CoStartCurrentTurn();    
    }

    private IEnumerator CoStartCurrentTurn()
    {
        if (!BattleReady) yield break;

        PlayerActionUsed = false;
        resolvingAction = false;
        waitingForDefense = false;
        waitingForDiscardSelect = false;
        selectedDiscardCard = null;
        turnEndButton?.SetIdleOnly(false);
        pendingDefenseCard = null;
        defenseSkipped = false;

        if (defenseSelectUI != null)
            defenseSelectUI.EndSelection();

        if (turnSystem.Current == TurnOwner.Player)
        {
            // ターン開始時点で即ロック
            InputLockManager.I?.Lock();

            if (!firstTurnIntroSkipped)
            {
                yield return ShowCenterMessage("Your Turn");
            }
            else
            {
                firstTurnIntroSkipped = false;
            }
                
            AudioManager.I?.PlayTurnStart();

            turnBanner?.SetText("My Turn");

            costManager.OnTurnStartGainOne();

            bool shouldDraw = true;

            // 最初のプレイヤーターンだけ特別処理
            if (!firstPlayerTurnStarted)
            {
                firstPlayerTurnStarted = true;

                // 先攻プレイヤーの最初のターンはドローしない
                if (TurnCount == 1)
                {
                    shouldDraw = false;
                }
            }

            if (shouldDraw && cardDealAnimator != null)
            {
                yield return cardDealAnimator.Draw(1);
                if (!BattleReady) yield break;
            }

            RefreshDeckUI();
            handLayout?.Rebuild();
            InputLockManager.I?.Unlock();
        }
        else
        {
            // Enemy Turn開始時点で即ロック
            InputLockManager.I?.Lock();

            if (!firstTurnIntroSkipped)
            {
                yield return ShowCenterMessage("Enemy Turn");
            }
            else
            {
                firstTurnIntroSkipped = false;
            }
    
            AudioManager.I?.PlayTurnStart();

            turnBanner?.SetText("Enemy Turn");

            yield return new WaitForSeconds(enemyThinkTime);
            yield return CoEnemyTurn();
        }
    }

    public bool TryPlayCard(CardView view, DropZoneType zoneType)
    {
        if (!BattleReady) return false;
        if (resolvingAction) return false;
        if (waitingForDefense) return false;
        if (waitingForDiscardSelect) return false;
        if (view == null || view.Data == null) return false;
        if (turnSystem.Current != TurnOwner.Player) return false;
        if (InputLockManager.I != null && InputLockManager.I.IsLocked) return false;
        if (PlayerActionUsed) return false;

        if (zoneType != DropZoneType.Enemy && zoneType != DropZoneType.Effect) return false;
        if (zoneType == DropZoneType.Effect && !EnemyAI.CanUseEffect(view.Data, handManager.handViews.Count - (handManager.Contains(view) ? 1 : 0))) return false;

        if (zoneType == DropZoneType.Effect && IsEffectCardBlocked())
        {
            Debug.Log("[Battle] Effect cards are blocked by 進捗確認.");
            return false;
        }

        if (!costManager.TryPay(view.Data.cost))
        {
            Debug.Log("[BattleStateMachine] Cost is not enough.");
            return false;
        }

        StartCoroutine(CoResolvePlayerCard(view, zoneType));
        return true;    
    }

    private IEnumerator CoResolvePlayerCard(CardView usedCard, DropZoneType zoneType)
    {
        if (usedCard == null || usedCard.Data == null)
            yield break;

        CardData card = usedCard.Data;

        resolvingAction = true;
        PlayerActionUsed = true;
        InputLockManager.I?.Lock();
        turnEndButton?.SetIdleOnly(true);

        // 使用したカード1枚だけを手札から消す
        if (handManager.Contains(usedCard))
        {
            handManager.RemoveCard(usedCard);
            handLayout?.Rebuild();

            CardVanishVfx vanish = usedCard.GetComponent<CardVanishVfx>();

            if (vanish != null)
            {
                bool done = false;

                vanish.Play(() =>
                {
                    Destroy(usedCard.gameObject);
                    done = true;
                });

                while (!done) yield return null;
            }
            else
            {
                Destroy(usedCard.gameObject);
            }
        }

        // 攻撃
        if (zoneType == DropZoneType.Enemy)
        {
            int rawAttack = Mathf.Max(0, card.attack);
            int buffedAttack = Mathf.FloorToInt(rawAttack * pendingNextAttackMultiplier);

            // 攻撃倍率を消費
            pendingNextAttackMultiplier = 1f;

            // 継続効果の表示を更新
            RefreshActiveEffectUI();


            int hpBefore = EnemyHP;
            yield return CoDefendCpu(buffedAttack);
            Debug.Log($"[Battle] Player attack: {card.displayName} / ATK {rawAttack} -> {buffedAttack} / Damage {hpBefore - EnemyHP} / EnemyHP {EnemyHP}");
            yield return new WaitForSeconds(0.35f);
        }
        // 効果
        else if (zoneType == DropZoneType.Effect)
        {
            yield return CoApplyEffect(card);
        }

        // 敵HPが0ならターンエンドせず勝利
        if (EnemyHP <= 0)
        {
            BattleReady = false;
            winLose.Win();
            yield break;
        }

        // 自分HPが0ならターンエンドせず敗北
        if (PlayerHP <= 0)
        {
            BattleReady = false;
            winLose.Lose();
            yield break;
        }

        resolvingAction = false;

        // カード処理が終わった瞬間、自動でターンエンド
        yield return CoEndPlayerTurn();
    }

    private string GetEffectPopupText(EffectData effect)
    {
        if (effect == null)
            return "";

        switch (effect.type)
        {
            case EffectType.Draw:
                return $"{effect.valueA} draw";

            case EffectType.Heal:
                return $"HP {effect.valueA} heal";

            case EffectType.DealDamage:
                return $"{effect.valueA} damage";

            case EffectType.AddCost:
                return $"cost {effect.valueA} heal";

            case EffectType.MultiplyNextDamage:
                return $"next attack x{effect.valueF}";

            case EffectType.PierceDefense:
                return $"next defense x{effect.valueF}";

            case EffectType.DiscardAttackMinusOneDamage:
                return "one discard and damage";

            case EffectType.DiscardDefenseMinusOneHeal:
                return "one discard and heal";

            case EffectType.HandCountMinusTwoDamage:
                return "hand count -2 and damage";

            case EffectType.HealAndSelfDamageNextTurnEnd:
                return $"HP {effect.valueA} heal";

            case EffectType.ReduceEnemyMaxHP:
                return $"enemy max HP -{effect.valueA}";

            case EffectType.BlockEffectsAndSelfDamageWhileInHand:
                return "block effects card";

            case EffectType.None:
            default:
                return "";
        }
    }

    private IEnumerator CoApplyEffect(CardData card)
    {
        yield return CoApplyCardEffect(card, false);
    }

    private IEnumerator CoWaitForDiscardSelection(string message)
    {
        waitingForDiscardSelect = true;
        selectedDiscardCard = null;

        turnBanner?.SetText(message);

        InputLockManager.I?.Unlock();

        while (selectedDiscardCard == null)
            yield return null;

        InputLockManager.I?.Lock();
        waitingForDiscardSelect = false;
    }

    private IEnumerator CoConsumeHandCard(CardView card)
    {
        if (card == null) yield break;

        handManager.RemoveCard(card);
        handLayout?.Rebuild();

        CardVanishVfx vanish = card.GetComponent<CardVanishVfx>();
        if (vanish != null)
        {
            bool done = false;
            vanish.Play(() =>
            {
                Destroy(card.gameObject);
                done = true;
            });

            while (!done) yield return null;
        }
        else
        {
            Destroy(card.gameObject);
        }
    }

    public void OnTurnEndButtonPressed()
    {
        Debug.Log("[TurnEnd] Button pressed");

        if (!BattleReady)
            return;

        if (waitingForDiscardSelect)
            return;

        // ターン切り替え処理がすでに始まっていたら絶対に通さない
        if (turnTransitioning)
            return;

        // ロック中も通さない
        if (InputLockManager.I != null &&
            InputLockManager.I.IsLocked)
            return;

        // =========================
        // 防御待ち → Skip
        // =========================
        if (waitingForDefense)
        {
            // 最初の1回で即座に防御受付を終了
            waitingForDefense = false;
            defenseSkipped = true;
            pendingDefenseCard = null;

            InputLockManager.I?.Lock();

            AudioManager.I?.PlayTurnEnd();

            Debug.Log("[Battle] Defense skipped");

            return;
        }

        // =========================
        // 通常のTurn End
        // =========================
        if (resolvingAction)
            return;

        if (turnSystem.Current != TurnOwner.Player)
            return;

        // ここで即座に多重起動を禁止
        turnTransitioning = true;

        InputLockManager.I?.Lock();

        AudioManager.I?.PlayTurnEnd();

        StartCoroutine(CoEndPlayerTurn());
    }
    private IEnumerator CoEndPlayerTurn()
    {
        if (!BattleReady) yield break;
        InputLockManager.I?.Lock();
        turnTransitioning = true;
        yield return CoEndSideEffects(false);
        if (!BattleReady || CheckBattleEnded()) yield break;
        yield return ShowCenterMessage("Turn End");
        turnSystem.NextTurn();
        turnTransitioning = false;
        yield return CoStartCurrentTurn();
    }

    private IEnumerator CoEnemyTurn()
    {
        yield return CoEnemyAction();
    }

    public void SelectDefenseCard(CardView view)
    {
        if (!waitingForDefense) return;
        if (view == null) return;
        if (!handManager.Contains(view)) return;

        // 選んだ瞬間に入力禁止
        InputLockManager.I?.Lock();

        pendingDefenseCard = view;
        defenseSkipped = false;

        if (defenseSelectUI != null)
            defenseSelectUI.SelectDefense(view);
    }

    public bool TrySelectDefenseByDrop(CardView view)
    {
        Debug.Log("ドラッグ防御検出");

        if (!waitingForDefense)
            return false;

        if (pendingDefenseCard != null || defenseSkipped || !BattleReady ||
            (InputLockManager.I != null && InputLockManager.I.IsLocked)) return false;

        if (view == null)
            return false;

        // ドロップ成立した瞬間にロック
        InputLockManager.I?.Lock();

        pendingDefenseCard = view;
        defenseSkipped = false;

        return true;
    }

    public void SkipDefense()
    {
        Debug.Log("SkipDefense 呼ばれた");

        if (!waitingForDefense)
            return;

        // 最初の1回を受け取った瞬間に受付終了
        waitingForDefense = false;

        InputLockManager.I?.Lock();

        defenseSkipped = true;
        pendingDefenseCard = null;
    }

    public void SelectDiscardCard(CardView view)
    {
        if (!waitingForDiscardSelect) return;
        if (selectedDiscardCard != null || !BattleReady) return;
        if (view == null) return;
        if (!handManager.Contains(view)) return;

        // 捨て札を選んだ瞬間に入力禁止
        InputLockManager.I?.Lock();

        selectedDiscardCard = view;
    }

    private void RefreshHpUI()
    {
        if (playerHpText != null)
            playerHpText.text = $"{PlayerHP}";

        if (enemyHpText != null)
            enemyHpText.text = $"{EnemyHP}";
    }
    private void RefreshActiveEffectUI()
    {
        playerActiveEffectDisplay?.Clear();
    }

    public string GetPlayerActiveEffectText(bool includeHandEffects = true)
    {
        return StatusText(pendingNextAttackMultiplier, pendingNextDefenseMultiplier,
            pendingSelfDamageThisPlayerTurnEnd, pendingSelfDamageNextPlayerTurnEnd,
            includeHandEffects && HasProgressCheckInHand(),
            Mathf.Max(0, initialPlayerMaxHP - playerMaxHP), playerMaxHP);
    }

    public string GetEnemyActiveEffectText(bool includeHandEffects = true)
    {
        return StatusText(enemyState.attackMultiplier, enemyState.defenseMultiplier,
            enemyState.selfDamageThisTurnEnd, enemyState.selfDamageNextTurnEnd,
            includeHandEffects && enemyState.HasHandRestriction(),
            Mathf.Max(0, initialEnemyMaxHP - enemyMaxHP), enemyMaxHP);
    }

    private IEnumerator ShowCenterMessage(string message)
    {
        bool wasLocked = InputLockManager.I != null && InputLockManager.I.IsLocked;

        if (!wasLocked)
            InputLockManager.I?.Lock();

        if (centerMessageText != null)
        {
            centerMessageText.gameObject.SetActive(true);
            centerMessageText.text = message;
        }

        yield return new WaitForSeconds(centerMessageDuration);

        if (centerMessageText != null)
            centerMessageText.gameObject.SetActive(false);

        if (!wasLocked)
            InputLockManager.I?.Unlock();
    }
}
