using System.Collections;
using UnityEngine;
using TMPro;


public class BattleStateMachine : MonoBehaviour
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
    public TMP_Text playerHpText;
    public TMP_Text enemyHpText;

    

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

    [Header("Temporary Enemy AI")]
    public int enemyFixedDamage = 1;
    public float enemyThinkTime = 0.8f;

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

    private CardView selectedDiscardCard;

    private int pendingSelfDamageAtNextPlayerTurnEnd = 0;


    // 次の攻撃 / 次の防御
    private float pendingNextAttackMultiplier = 1f;
    private float pendingNextDefenseMultiplier = 1f;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;
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
        InputLockManager.I?.Lock();
        BattleReady = false;
        PlayerActionUsed = false;
        resolvingAction = false;
        waitingForDefense = false;
        waitingForDiscardSelect = false;

        if (config == null || winLose == null || costManager == null || deckManager == null || handManager == null || cardDealAnimator == null)
        {
            Debug.LogError("[BattleStateMachine] Required reference is missing.");
            yield break;
        }

        if (SaveManager.I != null)
            config.useConfirm = SaveManager.I.GetConfirm(config.useConfirm);

        PlayerHP = Mathf.Clamp(playerStartHP, 0, playerMaxHP);
        RefreshHpUI();
        EnemyHP = Mathf.Clamp(enemyStartHP, 0, enemyMaxHP);
        RefreshHpUI();
        TurnCount = 1;

        bool playerFirst = true;
        if (coinToss != null)
        {
            yield return coinToss.Play();
            playerFirst = coinToss.PlayerFirst;
        }
        turnSystem.SetFirst(playerFirst ? TurnOwner.Player : TurnOwner.Enemy);

        handManager.ClearHand();
        handLayout?.Rebuild();

        bool deckOk = deckManager.SetupFromDefinition(deckDefinition);
        if (!deckOk)
        {
            Debug.LogError("[BattleStateMachine] Deck setup failed. Check deckDefinition entries.");
            BattleReady = false;
            winLose.Lose();
            yield break;
        }

        costManager.Init(config.startCost, config.maxCost);

        yield return cardDealAnimator.DealStartHand(config.startHandCount);
        handLayout?.Rebuild();

        BattleReady = true;

        yield return StartCoroutine(CoStartCurrentTurn());
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

        if (turnSystem.Current == TurnOwner.Player)
        {
            InputLockManager.I?.Lock();

            turnBanner?.SetText("プレイヤーターン");

            costManager.OnTurnStartGainOne();

            if (cardDealAnimator != null)
            {
                yield return cardDealAnimator.Draw(1);
            }

            handLayout?.Rebuild();
            InputLockManager.I?.Unlock();
        }
        else
        {
            InputLockManager.I?.Lock();

            turnBanner?.SetText("敵ターン");

            yield return new WaitForSeconds(enemyThinkTime);
            yield return StartCoroutine(CoEnemyTurn());
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

        StartCoroutine(CoResolvePlayerCard(view.Data, zoneType));
        return true;
    }

    private IEnumerator CoResolvePlayerCard(CardData card, DropZoneType zoneType)
    {
        resolvingAction = true;
        PlayerActionUsed = true;
        InputLockManager.I?.Lock();
        turnEndButton?.SetIdleOnly(true);
        CardView usedCard = null;

    for (int i = 0; i < handManager.handViews.Count; i++)
    {
        if (handManager.handViews[i].Data == card)
        {
            usedCard = handManager.handViews[i];
            break;
        }
    }

    if (usedCard != null)
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


        if (zoneType == DropZoneType.Enemy)
        {
            int rawAttack = Mathf.Max(0, card.attack);
            int buffedAttack = Mathf.FloorToInt(rawAttack * pendingNextAttackMultiplier);
            pendingNextAttackMultiplier = 1f;

            int damage = DamageCalculator.Calc(buffedAttack, 0);
            EnemyHP = Mathf.Max(0, EnemyHP - damage);
            RefreshHpUI();
            enemyHitVfx?.Play();

            Debug.Log($"[Battle] Player attack: {card.displayName} / ATK {rawAttack} -> {buffedAttack} / Damage {damage} / EnemyHP {EnemyHP}");
            yield return new WaitForSeconds(0.35f);
        }
        else if (zoneType == DropZoneType.Effect)
        {
            yield return StartCoroutine(CoApplyEffect(card));
        }

        if (EnemyHP <= 0)
        {
            BattleReady = false;
            winLose.Win();
            yield break;
        }

        resolvingAction = false;
        InputLockManager.I?.Unlock();
    }

    private IEnumerator CoApplyEffect(CardData card)
    {
        if (card == null || card.effect == null) yield break;

        var e = card.effect;

        switch (e.type)
        {
            case EffectType.None:
                yield break;

            case EffectType.Draw:
                if (effectSystem != null)
                    yield return effectSystem.ApplyEffect(e);
                handLayout?.Rebuild();
                yield break;

            case EffectType.Heal:
                PlayerHP = Mathf.Min(playerMaxHP, PlayerHP + Mathf.Max(0, e.valueA));
                RefreshHpUI();
                Debug.Log($"[Battle] Heal {e.valueA} / PlayerHP {PlayerHP}");
                yield return new WaitForSeconds(0.2f);
                yield break;

            case EffectType.DealDamage:
                EnemyHP = Mathf.Max(0, EnemyHP - Mathf.Max(0, e.valueA));
                RefreshHpUI();
                enemyHitVfx?.Play();
                Debug.Log($"[Battle] Effect damage {e.valueA} / EnemyHP {EnemyHP}");
                yield return new WaitForSeconds(0.35f);
                yield break;

            case EffectType.AddCost:
                costManager.Add(Mathf.Max(0, e.valueA));
                Debug.Log($"[Battle] Add cost {e.valueA} / CurrentCost {costManager.Current}");
                yield return new WaitForSeconds(0.15f);
                yield break;

            case EffectType.MultiplyNextDamage:
                pendingNextAttackMultiplier = Mathf.Max(1f, e.valueF);
                Debug.Log($"[Battle] Next attack multiplier = x{pendingNextAttackMultiplier}");
                yield return new WaitForSeconds(0.15f);
                yield break;

            case EffectType.PierceDefense:
                pendingNextDefenseMultiplier = Mathf.Max(1f, e.valueF);
                Debug.Log($"[Battle] Next defense multiplier = x{pendingNextDefenseMultiplier}");
                yield return new WaitForSeconds(0.15f);
                yield break;

            case EffectType.DiscardAttackMinusOneDamage:
                yield return StartCoroutine(CoDiscardAttackMinusOneDamage());
                yield break;

            case EffectType.DiscardDefenseMinusOneHeal:
                yield return StartCoroutine(CoDiscardDefenseMinusOneHeal());
                yield break;

            case EffectType.HandCountMinusTwoDamage:
                yield return StartCoroutine(CoHandCountMinusTwoDamage());
                yield break;

            case EffectType.HealAndSelfDamageNextTurnEnd:
                {
                    int heal = Mathf.Max(0, e.valueA);
                    int selfDamage = Mathf.Max(0, Mathf.RoundToInt(e.valueF));

                    PlayerHP = Mathf.Min(playerMaxHP, PlayerHP + heal);
                    RefreshHpUI();
                    pendingSelfDamageAtNextPlayerTurnEnd += selfDamage;

                    Debug.Log($"[Battle] Heal {heal} / PlayerHP {PlayerHP} / SelfDamageReserved {pendingSelfDamageAtNextPlayerTurnEnd}");
                    yield return new WaitForSeconds(0.25f);
                    yield break;
                }

            case EffectType.ReduceEnemyMaxHP:
                {
                    int reduce = Mathf.Max(0, e.valueA);

                    enemyMaxHP = Mathf.Max(1, enemyMaxHP - reduce);
                    EnemyHP = Mathf.Min(EnemyHP, enemyMaxHP);
                    RefreshHpUI();

                    Debug.Log($"[Battle] Enemy MaxHP -{reduce} / EnemyMaxHP {enemyMaxHP} / EnemyHP {EnemyHP}");
                    yield return new WaitForSeconds(0.25f);

                    if (EnemyHP <= 0)
                    {
                        BattleReady = false;
                        winLose.Win();
                    }
                    yield break;
                }


            default:
                yield break;
        }
    }

    private IEnumerator CoDiscardAttackMinusOneDamage()
    {
        if (handManager == null || handManager.handViews.Count <= 0)
        {
            Debug.Log("[Battle] No card to discard.");
            yield break;
        }

        yield return StartCoroutine(CoWaitForDiscardSelection("捨てるカードを選択してください"));

        if (selectedDiscardCard == null || selectedDiscardCard.Data == null)
            yield break;

        int damage = Mathf.Max(0, selectedDiscardCard.Data.attack - 1);

        yield return StartCoroutine(CoConsumeHandCard(selectedDiscardCard));

        EnemyHP = Mathf.Max(0, EnemyHP - damage);
        RefreshHpUI();
        enemyHitVfx?.Play();

        Debug.Log($"[Battle] QGJ effect / Damage {damage} / EnemyHP {EnemyHP}");
        yield return new WaitForSeconds(0.35f);

        if (EnemyHP <= 0)
        {
            BattleReady = false;
            winLose.Win();
        }
    }

    private IEnumerator CoDiscardDefenseMinusOneHeal()
    {
        if (handManager == null || handManager.handViews.Count <= 0)
        {
            Debug.Log("[Battle] No card to discard.");
            yield break;
        }

        yield return StartCoroutine(CoWaitForDiscardSelection("捨てるカードを選択してください"));

        if (selectedDiscardCard == null || selectedDiscardCard.Data == null)
            yield break;

        int heal = Mathf.Max(0, selectedDiscardCard.Data.defense - 1);

        yield return StartCoroutine(CoConsumeHandCard(selectedDiscardCard));

        PlayerHP = Mathf.Min(playerMaxHP, PlayerHP + heal);
        RefreshHpUI();

        Debug.Log($"[Battle] 部室 effect / Heal {heal} / PlayerHP {PlayerHP}");
        yield return new WaitForSeconds(0.25f);
    }

    private IEnumerator CoHandCountMinusTwoDamage()
    {
        int handCount = handManager != null ? handManager.handViews.Count : 0;
        int damage = Mathf.Max(0, handCount - 2);

        EnemyHP = Mathf.Max(0, EnemyHP - damage);
        RefreshHpUI();
        enemyHitVfx?.Play();

        Debug.Log($"[Battle] 講座 effect / Hand {handCount} / Damage {damage} / EnemyHP {EnemyHP}");
        yield return new WaitForSeconds(0.35f);

        if (EnemyHP <= 0)
        {
            BattleReady = false;
            winLose.Win();
        }
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
        if (!BattleReady) return;
        if (turnSystem.Current != TurnOwner.Player) return;
        if (resolvingAction) return;
        if (waitingForDefense) return;
        if (waitingForDiscardSelect) return;
        if (InputLockManager.I != null && InputLockManager.I.IsLocked) return;

        StartCoroutine(CoEndPlayerTurn());
    }

    private IEnumerator CoEndPlayerTurn()
    {
        InputLockManager.I?.Lock();
        yield return new WaitForSeconds(0.1f);

                // 進捗確認：手札にある間、自分ターン終了時に1ダメージ
        if (HasProgressCheckInHand())
        {
            PlayerHP = Mathf.Max(0, PlayerHP - 1);
            RefreshHpUI();
            playerHitVfx?.Play();

            Debug.Log($"[Battle] 進捗確認 self damage 1 / PlayerHP {PlayerHP}");
            yield return new WaitForSeconds(0.35f);

            if (PlayerHP <= 0)
            {
                BattleReady = false;
                winLose.Lose();
                yield break;
            }
        }


        // 配信などの「次の自分ターン終了時に自分へダメージ」
        if (pendingSelfDamageAtNextPlayerTurnEnd > 0)
        {
            int selfDamage = pendingSelfDamageAtNextPlayerTurnEnd;
            pendingSelfDamageAtNextPlayerTurnEnd = 0;

            PlayerHP = Mathf.Max(0, PlayerHP - selfDamage);
            RefreshHpUI();
            playerHitVfx?.Play();

            Debug.Log($"[Battle] Self damage at player turn end: {selfDamage} / PlayerHP {PlayerHP}");
            yield return new WaitForSeconds(0.35f);

            if (PlayerHP <= 0)
            {
                BattleReady = false;
                winLose.Lose();
                yield break;
            }
        }

        turnSystem.NextTurn();
        yield return StartCoroutine(CoStartCurrentTurn());
    }

    private IEnumerator CoEnemyTurn()
    {
        int attack = Mathf.Max(0, enemyFixedDamage);

        CardView defenseCard = null;
        int defenseValue = 0;

        if (defenseSelectUI != null && handManager.handViews.Count > 0)
        {
            waitingForDefense = true;
            InputLockManager.I?.Unlock();

            yield return defenseSelectUI.WaitDecision();

            InputLockManager.I?.Lock();
            waitingForDefense = false;

            defenseCard = defenseSelectUI.GetSelected();
            if (defenseCard != null && defenseCard.Data != null)
            {
                int rawDefense = Mathf.Max(0, defenseCard.Data.defense);
                defenseValue = Mathf.FloorToInt(rawDefense * pendingNextDefenseMultiplier);
                pendingNextDefenseMultiplier = 1f;
            }
        }

        int damage = DamageCalculator.Calc(attack, defenseValue);

        if (defenseCard != null)
        {
            Debug.Log($"[Battle] Defense card used: {defenseCard.Data.displayName} / DEF {defenseValue}");
            handManager.RemoveCard(defenseCard);
            handLayout?.Rebuild();

            CardVanishVfx vanish = defenseCard.GetComponent<CardVanishVfx>();
            if (vanish != null)
            {
                bool done = false;
                vanish.Play(() =>
                {
                    Destroy(defenseCard.gameObject);
                    done = true;
                });
                while (!done) yield return null;
            }
            else
            {
                Destroy(defenseCard.gameObject);
            }
        }

        if (damage > 0)
        {
            PlayerHP = Mathf.Max(0, PlayerHP - damage);
            RefreshHpUI();
            playerHitVfx?.Play();
        }

        Debug.Log($"[Battle] Enemy attack {attack} / Defense {defenseValue} / Damage {damage} / PlayerHP {PlayerHP}");
        yield return new WaitForSeconds(0.35f);

        if (PlayerHP <= 0)
        {
            BattleReady = false;
            winLose.Lose();
            yield break;
        }

        turnSystem.NextTurn();
        TurnCount++;
        yield return StartCoroutine(CoStartCurrentTurn());
    }

    public void SelectDefenseCard(CardView view)
    {
        if (!waitingForDefense) return;
        if (view == null) return;
        if (defenseSelectUI == null) return;
        if (!handManager.Contains(view)) return;

        defenseSelectUI.SelectDefense(view);
    }

    public void SelectDiscardCard(CardView view)
    {
        if (!waitingForDiscardSelect) return;
        if (view == null) return;
        if (!handManager.Contains(view)) return;

        selectedDiscardCard = view;
    }

    private void RefreshHpUI()
    {
        if (playerHpText != null)
            playerHpText.text = $"HP {PlayerHP} / {playerMaxHP}";

        if (enemyHpText != null)
            enemyHpText.text = $"HP {EnemyHP} / {enemyMaxHP}";
    }

}
