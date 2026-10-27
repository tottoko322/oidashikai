using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public partial class BattleStateMachine
{
    [Header("CPU")]
    [Tooltip("タイトルで難易度を選ぶと保存した値が優先されます。未選択時はこの値を使用します。")]
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
        if (BattlePreferences.HasDifficulty)
            cpuDifficulty = BattlePreferences.GetDifficulty(cpuDifficulty);
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
        TMP_FontAsset font = GameFonts.Default;
        if (cpuCardRevealUI == null)
        {
            cpuCardRevealUI = gameObject.AddComponent<CpuCardRevealUI>();
            cpuCardRevealUI.Initialize(canvas.transform, font,
                enemyHitVfx != null ? enemyHitVfx.targetRect : null, cpuConfig);
        }
        if (enemyInfoText == null)
        {
            enemyInfoText = deckCountText != null
                ? Instantiate(deckCountText, deckCountText.transform.parent)
                : CreateCpuText(canvas.transform, "CpuDeckCount", font, Vector2.zero, new Vector2(220f, 60f));
            enemyInfoText.name = "CpuDeckCount";
        }
        PositionCpuDeckCount();
        enemyActiveEffectDisplay?.Clear();
    }

    private void PositionCpuDeckCount()
    {
        if (enemyInfoText == null || enemyHpText == null) return;
        var rect = enemyInfoText.rectTransform;
        var hp = enemyHpText.rectTransform;
        Vector2 size = deckCountText != null ? deckCountText.rectTransform.rect.size : rect.rect.size;
        rect.SetParent(hp.parent, false);
        rect.anchorMin = hp.anchorMin;
        rect.anchorMax = hp.anchorMax;
        rect.pivot = new Vector2(0f, 0.5f);
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
        // HP's text rectangle can be much wider than its visible digits.
        enemyHpText.ForceMeshUpdate();
        var bounds = enemyHpText.textBounds;
        Vector3 edge = enemyHpText.textInfo.characterCount > 0
            ? new Vector3(bounds.max.x, bounds.center.y, 0f)
            : new Vector3(hp.rect.xMax, hp.rect.center.y, 0f);
        rect.position = hp.TransformPoint(edge + new Vector3(24f, 0f, 0f));
        enemyInfoText.raycastTarget = false;
    }

    private void LateUpdate() => PositionCpuDeckCount();

    private static TMP_Text CreateCpuText(Transform parent, string name, TMP_FontAsset font, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<TextMeshProUGUI>();
        GameFonts.Apply(text); text.fontSize = 26f; text.enableAutoSizing = true;
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
            enemyInfoText.text = $"やまふだ {EnemyDeckRemaining}";
        }
        enemyActiveEffectDisplay?.Clear();
    }

    private CpuDecisionContext CpuContext() => new CpuDecisionContext
    {
        hand = enemyState.hand, cost = enemyState.Cost, maxCost = enemyState.MaxCost,
        hp = EnemyHP, maxHP = enemyMaxHP, opponentHP = PlayerHP, opponentMaxHP = playerMaxHP,
        deckRemaining = enemyState.Remaining, maxHand = enemyState.MaxHand,
        attackMultiplier = enemyState.attackMultiplier, defenseMultiplier = enemyState.defenseMultiplier,
        effectsBlocked = enemyState.HasHandRestriction()
    };

    private IEnumerator RevealCpuCard(CardData card, string usage, string detail = "", IEnumerator whileVisible = null)
    {
        characterEffectPanelUI?.Close();
        if (handManager != null && handManager.popupUI != null && handManager.popupUI.IsOpen)
            handManager.popupUI.Close(false);
        if (cpuCardRevealUI != null) yield return cpuCardRevealUI.Show(card, usage, detail, whileVisible);
        else if (whileVisible != null) yield return whileVisible;
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
                yield return RevealCpuCard(action.card, "こうか", GetEffectPopupText(action.card.effect));
                yield return CoApplyCardEffect(action.card, true);
            }
            else
            {
                int attack = Mathf.FloorToInt(Mathf.Max(0, action.card.attack) * enemyState.attackMultiplier);
                enemyState.attackMultiplier = 1f;
                RefreshCpuUI();
                bool canChooseDefense = attack > 0 && handManager.handViews.Count > 0;
                yield return RevealCpuCard(action.card, "こうげき", $"ATK {attack}",
                    canChooseDefense ? CoDefendPlayer(attack) : null);
                if (!canChooseDefense) yield return CoDefendPlayer(attack);
            }
        }
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
            defenseSelectUI?.BeginSelection();
            InputLockManager.I?.Unlock();
            while (pendingDefenseCard == null && !defenseSkipped && BattleReady) yield return null;
            InputLockManager.I?.Lock();
            waitingForDefense = false;
            defenseSelectUI?.EndSelection();
        }
        if (cpuCardRevealUI != null && cpuCardRevealUI.IsVisible)
            yield return cpuCardRevealUI.ReturnToOrigin();
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
            yield return RevealCpuCard(card, "ぼうぎょ", $"DEF {defense}");
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
            yield return effectPopupUI.Show((enemy ? "CPU: " : "") + text);
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
                        yield return RevealCpuCard(discarded, "すてふだ", "こうかのたいしょう");
                    }
                }
                else if (handManager.handViews.Count > 0)
                {
                    yield return CoWaitForDiscardSelection("すてるかーどをくりっく");
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
        if (!Mathf.Approximately(attack, 1f)) text += $"・つぎのこうげき x{attack}\n";
        if (!Mathf.Approximately(defense, 1f)) text += $"・つぎのぼうぎょ x{defense}\n";
        if (currentDamage > 0) text += $"・じぶんのたーんおわりに {currentDamage}だめーじ\n";
        if (nextDamage > 0) text += $"・つぎのじぶんのたーんおわりに {nextDamage}だめーじ\n";
        if (blocked) text += "・しんちょくかくにん: てふだにあるあいだ、こうかかーどはつかえません/じぶんのたーんおわりに1だめーじ\n";
        if (reduction > 0) text += $"・さいだいHP -{reduction}(いまのさいだいHP: {maxHP})\n";
        return text.TrimEnd();
    }
}
