using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public partial class BattleStateMachine
{
    [Header("CPU")]
    public CpuDifficulty cpuDifficulty = CpuDifficulty.Normal;
    public CpuBattleConfig cpuConfig;
    public DeckDefinition enemyDeckDefinition;
    public ActiveEffectDisplay enemyActiveEffectDisplay;
    public TMP_Text enemyInfoText;
    public CpuCardRevealUI cpuCardRevealUI;

    private readonly EnemyBattleState enemyState = new EnemyBattleState();
    private bool firstEnemyTurnStarted, cpuWasFirst;
    private int initialPlayerMaxHP;
    public int EnemyHandCount => enemyState.hand.Count;
    public int EnemyDeckRemaining => enemyState.Remaining;
    public int EnemyCost => enemyState.Cost;

    private bool SetupCpu()
    {
        if (cpuConfig == null) cpuConfig = Resources.Load<CpuBattleConfig>("CPU/DefaultCpuConfig");
        var definition = enemyDeckDefinition != null ? enemyDeckDefinition
            : cpuConfig != null ? cpuConfig.DeckFor(cpuDifficulty) : null;
        if (!enemyState.Setup(definition, config))
        {
            Debug.LogError("[CPU] Assign a CPU deck within the shared deck and copy limits.");
            return false;
        }
        for (int i = 0; i < config.startHandCount; i++)
            if (!enemyState.TryDraw()) { NotifyDeckEmpty(true); return false; }
        firstEnemyTurnStarted = false;
        cpuWasFirst = turnSystem.Current == TurnOwner.Enemy;
        SetupCpuUI();
        RefreshCpuUI();
        return true;
    }

    private void SetupCpuUI()
    {
        var canvas = playerHpText != null ? playerHpText.canvas : null;
        if (canvas == null) return;
        TMP_FontAsset font = characterEffectPanelUI != null && characterEffectPanelUI.effectListText != null
            ? characterEffectPanelUI.effectListText.font : playerHpText.font;
        if (cpuCardRevealUI == null)
        {
            cpuCardRevealUI = gameObject.AddComponent<CpuCardRevealUI>();
            cpuCardRevealUI.Initialize(canvas.transform, font,
                enemyHitVfx != null ? enemyHitVfx.targetRect : null, cpuConfig);
        }
        if (enemyInfoText == null)
            enemyInfoText = CreateCpuText(canvas.transform, "CpuResources", font, new Vector2(420f, 420f), new Vector2(500f, 100f));
        if (enemyActiveEffectDisplay == null)
        {
            var text = CreateCpuText(canvas.transform, "CpuActiveEffects", font, new Vector2(-350f, 250f), new Vector2(400f, 130f));
            enemyActiveEffectDisplay = text.gameObject.AddComponent<ActiveEffectDisplay>();
            enemyActiveEffectDisplay.root = text.gameObject;
            enemyActiveEffectDisplay.effectText = text;
        }
    }

    private static TMP_Text CreateCpuText(Transform parent, string name, TMP_FontAsset font, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<TextMeshProUGUI>();
        text.font = font; text.fontSize = 26f; text.enableAutoSizing = true;
        text.fontSizeMin = 16f; text.fontSizeMax = 26f;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.rectTransform.sizeDelta = size;
        text.rectTransform.anchoredPosition = position;
        return text;
    }

    private void RefreshCpuUI()
    {
        if (enemyInfoText != null)
        {
            string difficulty = cpuDifficulty == CpuDifficulty.Easy ? "やさしい"
                : cpuDifficulty == CpuDifficulty.Hard ? "むずかしい" : "ふつう";
            enemyInfoText.text = $"CPU（{difficulty}）\n山札 {EnemyDeckRemaining}  手札 {EnemyHandCount}  コスト {EnemyCost}/{enemyState.MaxCost}";
        }
        enemyActiveEffectDisplay?.SetEffects(GetEnemyActiveEffectText(false));
    }

    private CpuDecisionContext CpuContext() => new CpuDecisionContext
    {
        hand = enemyState.hand, cost = enemyState.Cost, maxCost = enemyState.MaxCost,
        hp = EnemyHP, maxHP = enemyMaxHP, opponentHP = PlayerHP, opponentMaxHP = playerMaxHP,
        deckRemaining = enemyState.Remaining, maxHand = enemyState.MaxHand,
        attackMultiplier = enemyState.attackMultiplier, defenseMultiplier = enemyState.defenseMultiplier,
        effectsBlocked = enemyState.HasHandRestriction()
    };

    private IEnumerator RevealCpuCard(CardData card, string usage, string detail = "")
    {
        characterEffectPanelUI?.Close();
        if (handManager != null && handManager.popupUI != null && handManager.popupUI.IsOpen)
            handManager.popupUI.Close(false);
        ShowEnemyAction($"CPUの{usage}：{card.displayName} {detail}");
        if (cpuCardRevealUI != null) yield return cpuCardRevealUI.Show(card, usage, detail);
    }

    private IEnumerator CoEnemyAction()
    {
        turnTransitioning = true;
        enemyState.StartTurn();
        bool skipDraw = !firstEnemyTurnStarted && cpuWasFirst;
        firstEnemyTurnStarted = true;
        if (!skipDraw && !enemyState.TryDraw()) { NotifyDeckEmpty(true); yield break; }
        RefreshCpuUI();
        var action = EnemyAI.Decide(CpuContext(), cpuDifficulty);
        if (action.card != null && enemyState.TryPay(action.card.cost))
        {
            enemyState.hand.Remove(action.card);
            RefreshCpuUI();
            if (action.useEffect)
            {
                yield return RevealCpuCard(action.card, "効果", GetEffectPopupText(action.card.effect));
                yield return CoApplyCardEffect(action.card, true);
            }
            else
            {
                int attack = Mathf.FloorToInt(Mathf.Max(0, action.card.attack) * enemyState.attackMultiplier);
                enemyState.attackMultiplier = 1f;
                RefreshCpuUI();
                yield return RevealCpuCard(action.card, "攻撃", $"ATK {attack}");
                yield return CoDefendPlayer(attack);
            }
        }
        else ShowEnemyAction("CPUは何もせずターン終了");
        if (CheckBattleEnded()) yield break;
        yield return CoEndSideEffects(true);
        if (!BattleReady || CheckBattleEnded()) yield break;
        yield return ShowCenterMessage("Turn End");
        turnSystem.NextTurn(); TurnCount++;
        turnTransitioning = false;
        yield return CoStartCurrentTurn();
    }

    private IEnumerator CoDefendPlayer(int attack)
    {
        pendingDefenseCard = null; defenseSkipped = false;
        if (attack > 0 && handManager.handViews.Count > 0)
        {
            waitingForDefense = true;
            defenseSelectUI?.BeginSelection($"CPUの攻撃 {attack}：防御かスキップ");
            turnBanner?.SetText($"CPUの攻撃 {attack}");
            InputLockManager.I?.Unlock();
            while (pendingDefenseCard == null && !defenseSkipped && BattleReady) yield return null;
            InputLockManager.I?.Lock();
            waitingForDefense = false;
            defenseSelectUI?.EndSelection();
        }
        if (!BattleReady) yield break;
        int defense = 0;
        if (pendingDefenseCard != null && pendingDefenseCard.Data != null)
        {
            defense = Mathf.FloorToInt(Mathf.Max(0, pendingDefenseCard.Data.defense) * pendingNextDefenseMultiplier);
            pendingNextDefenseMultiplier = 1f;
            RefreshActiveEffectUI();
            yield return CoConsumeHandCard(pendingDefenseCard);
        }
        ApplyDamage(false, DamageCalculator.Calc(attack, defense));
        yield return new WaitForSeconds(0.2f);
    }

    private IEnumerator CoDefendCpu(int attack)
    {
        var card = EnemyAI.ChooseDefense(enemyState.hand, attack, enemyState.defenseMultiplier, EnemyHP, cpuDifficulty);
        int defense = 0;
        if (card != null)
        {
            enemyState.hand.Remove(card);
            defense = Mathf.FloorToInt(Mathf.Max(0, card.defense) * enemyState.defenseMultiplier);
            enemyState.defenseMultiplier = 1f;
            RefreshCpuUI();
            yield return RevealCpuCard(card, "防御", $"DEF {defense}");
        }
        ApplyDamage(true, DamageCalculator.Calc(attack, defense));
        yield return new WaitForSeconds(0.2f);
    }

    public void NotifyDeckEmpty(bool enemy)
    {
        BattleReady = false;
        waitingForDefense = waitingForDiscardSelect = false;
        InputLockManager.I?.Lock();
        if (enemy) winLose?.Win(); else winLose?.Lose();
    }

    private bool CheckBattleEnded()
    {
        if (!BattleReady) return true;
        if (EnemyHP <= 0) { BattleReady = false; winLose?.Win(); }
        else if (PlayerHP <= 0) { BattleReady = false; winLose?.Lose(); }
        if (!BattleReady)
        {
            characterEffectPanelUI?.Close();
            waitingForDefense = waitingForDiscardSelect = false;
            InputLockManager.I?.Lock();
        }
        return !BattleReady;
    }

    private void ApplyDamage(bool enemy, int amount)
    {
        amount = Mathf.Max(0, amount);
        if (enemy) EnemyHP = Mathf.Max(0, EnemyHP - amount);
        else PlayerHP = Mathf.Max(0, PlayerHP - amount);
        RefreshHpUI();
        if (amount <= 0) return;
        if (enemy) enemyHitVfx?.Play(); else playerHitVfx?.Play();
        AudioManager.I?.PlayDamage();
    }

    private void HealSide(bool enemy, int amount)
    {
        if (enemy) EnemyHP = Mathf.Min(enemyMaxHP, EnemyHP + Mathf.Max(0, amount));
        else PlayerHP = Mathf.Min(playerMaxHP, PlayerHP + Mathf.Max(0, amount));
        RefreshHpUI(); AudioManager.I?.PlayHeal();
    }

    private void ReserveSideDamage(bool enemy, EffectData effect, int amount)
    {
        amount = Mathf.Max(0, amount);
        if (effect.timing == EffectTiming.Immediate) ApplyDamage(enemy, amount);
        else if (effect.timing == EffectTiming.TurnEndBeforeCurse)
        {
            if (enemy) enemyState.selfDamageThisTurnEnd += amount;
            else pendingSelfDamageThisPlayerTurnEnd += amount;
        }
        else
        {
            if (enemy) enemyState.selfDamageNextTurnEnd += amount;
            else pendingSelfDamageNextPlayerTurnEnd += amount;
        }
    }

    private IEnumerator CoEndSideEffects(bool enemy)
    {
        if (enemy ? enemyState.HasHandRestriction() : HasProgressCheckInHand())
        {
            ApplyDamage(enemy, 1);
            if (CheckBattleEnded()) yield break;
            yield return new WaitForSeconds(0.2f);
        }
        int scheduled = enemy ? enemyState.selfDamageThisTurnEnd : pendingSelfDamageThisPlayerTurnEnd;
        if (enemy) enemyState.selfDamageThisTurnEnd = 0;
        else pendingSelfDamageThisPlayerTurnEnd = 0;
        ApplyDamage(enemy, scheduled);
        if (enemy)
        {
            enemyState.selfDamageThisTurnEnd = enemyState.selfDamageNextTurnEnd;
            enemyState.selfDamageNextTurnEnd = 0;
        }
        else
        {
            pendingSelfDamageThisPlayerTurnEnd = pendingSelfDamageNextPlayerTurnEnd;
            pendingSelfDamageNextPlayerTurnEnd = 0;
        }
        RefreshActiveEffectUI(); RefreshCpuUI();
        CheckBattleEnded();
    }

    // Shared resolver: only the acting side changes; effect values/timing stay identical.
    private IEnumerator CoApplyCardEffect(CardData card, bool enemy)
    {
        if (card == null || card.effect == null) yield break;
        var effect = card.effect;
        string text = GetEffectPopupText(effect);
        if (effectPopupUI != null && !string.IsNullOrEmpty(text))
            yield return effectPopupUI.Show((enemy ? "CPU：" : "") + text);
        int amount = Mathf.Max(0, effect.valueA);
        switch (effect.type)
        {
            case EffectType.Draw:
                if (enemy)
                {
                    for (int i = 0; i < amount; i++)
                        if (!enemyState.TryDraw()) { NotifyDeckEmpty(true); yield break; }
                }
                else yield return cardDealAnimator.Draw(amount);
                break;
            case EffectType.Heal: HealSide(enemy, amount); break;
            case EffectType.DealDamage: ApplyDamage(!enemy, amount); break;
            case EffectType.AddCost:
                if (enemy) enemyState.AddCost(amount); else costManager.Add(amount);
                AudioManager.I?.PlayBuff(); break;
            case EffectType.MultiplyNextDamage:
                if (enemy) enemyState.attackMultiplier = Mathf.Max(1f, effect.valueF);
                else pendingNextAttackMultiplier = Mathf.Max(1f, effect.valueF);
                AudioManager.I?.PlayBuff(); break;
            case EffectType.PierceDefense:
                if (enemy) enemyState.defenseMultiplier = Mathf.Max(1f, effect.valueF);
                else pendingNextDefenseMultiplier = Mathf.Max(1f, effect.valueF);
                AudioManager.I?.PlayBuff(); break;
            case EffectType.DiscardAttackMinusOneDamage:
            case EffectType.DiscardDefenseMinusOneHeal:
                CardData discarded = null;
                if (enemy)
                {
                    discarded = EnemyAI.ChooseDiscard(enemyState.hand, null, effect.type, cpuDifficulty);
                    if (discarded != null)
                    {
                        enemyState.hand.Remove(discarded);
                        yield return RevealCpuCard(discarded, "捨て札", "効果の対象");
                    }
                }
                else if (handManager.handViews.Count > 0)
                {
                    yield return CoWaitForDiscardSelection("すてるカードをクリック");
                    if (selectedDiscardCard != null)
                    {
                        discarded = selectedDiscardCard.Data;
                        yield return CoConsumeHandCard(selectedDiscardCard);
                    }
                }
                if (discarded != null)
                {
                    if (effect.type == EffectType.DiscardAttackMinusOneDamage)
                        ApplyDamage(!enemy, Mathf.Max(0, discarded.attack - 1));
                    else HealSide(enemy, Mathf.Max(0, discarded.defense - 1));
                }
                break;
            case EffectType.HandCountMinusTwoDamage:
                int count = enemy ? enemyState.hand.Count : handManager.handViews.Count;
                ApplyDamage(!enemy, Mathf.Max(0, count - 2)); break;
            case EffectType.HealAndSelfDamageNextTurnEnd:
                HealSide(enemy, amount);
                ReserveSideDamage(enemy, effect, Mathf.RoundToInt(effect.valueF)); break;
            case EffectType.ReduceEnemyMaxHP:
                if (enemy) { playerMaxHP = Mathf.Max(1, playerMaxHP - amount); PlayerHP = Mathf.Min(PlayerHP, playerMaxHP); }
                else { enemyMaxHP = Mathf.Max(1, enemyMaxHP - amount); EnemyHP = Mathf.Min(EnemyHP, enemyMaxHP); }
                RefreshHpUI(); AudioManager.I?.PlayBuff(); break;
        }
        RefreshDeckUI(); RefreshActiveEffectUI(); RefreshCpuUI();
        CheckBattleEnded();
    }

    private static string StatusText(float attack, float defense, int currentDamage, int nextDamage,
        bool blocked, int reduction, int maxHP)
    {
        string text = "";
        if (!Mathf.Approximately(attack, 1f)) text += $"・つぎのコウゲキ ×{attack}\n";
        if (!Mathf.Approximately(defense, 1f)) text += $"・つぎのボウギョ ×{defense}\n";
        if (currentDamage > 0) text += $"・自分のターン終了時 {currentDamage}ダメージ\n";
        if (nextDamage > 0) text += $"・つぎの自分のターン終了時 {nextDamage}ダメージ\n";
        if (blocked) text += "・進捗確認：手札にある間、効果カード使用不可／自分のターン終了時に1ダメージ\n";
        if (reduction > 0) text += $"・最大HP -{reduction}（現在の最大HP：{maxHP}）\n";
        return text.TrimEnd();
    }
}
