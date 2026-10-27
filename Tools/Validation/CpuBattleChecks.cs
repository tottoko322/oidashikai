using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class CpuBattleChecks
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static int checks;
    static CpuBattleChecks()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("CpuChecks", false))
            {
                SessionState.SetBool("CpuChecks", false);
                RunChecks();
            }
        };
    }
    public static void Run() { SessionState.SetBool("CpuChecks", true); EditorApplication.EnterPlaymode(); }
    static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
        checks++; Debug.Log("CPU PASS: " + message);
    }
    static void Set(object obj, string name, object value) => obj.GetType().GetField(name, Private).SetValue(obj, value);
    static object Get(object obj, string name) => obj.GetType().GetField(name, Private).GetValue(obj);
    static object Invoke(object obj, string name, params object[] args) => obj.GetType().GetMethod(name, Private).Invoke(obj, args);
    static void Drain(IEnumerator routine, BattleStateMachine battle = null)
    {
        int iterations = 0;
        while (routine.MoveNext())
        {
            if (++iterations > 1000) throw new Exception("Coroutine stuck waiting");
            if (battle != null && battle.IsWaitingForDiscardSelect && battle.handManager.handViews.Count > 0)
                battle.SelectDiscardCard(battle.handManager.handViews[0]);
            if (battle != null && battle.IsWaitingForDefense && battle.handManager.handViews.Count > 0)
                battle.SelectDefenseCard(battle.handManager.handViews[0]);
            if (routine.Current is IEnumerator nested) Drain(nested, battle);
        }
    }
    static T Component<T>(string name) where T : Component
    {
        var root = new GameObject(name); root.SetActive(false); return root.AddComponent<T>();
    }
    static CardData Card(int attack = 1, int defense = 1, int cost = 0, EffectType type = EffectType.None, int amount = 0, float multiplier = 1f)
    {
        var c = ScriptableObject.CreateInstance<CardData>();
        c.displayName = "TestCard"; c.attack = attack; c.defense = defense; c.cost = cost;
        c.effect = ScriptableObject.CreateInstance<EffectData>();
        c.effect.type = type; c.effect.valueA = amount; c.effect.valueF = multiplier;
        return c;
    }
    static GameConfig Config()
    {
        var config = ScriptableObject.CreateInstance<GameConfig>();
        config.deckLimit = 20; config.sameCardLimit = 2;
        return config;
    }
    static DeckDefinition Deck(int count = 20)
    {
        var deck = ScriptableObject.CreateInstance<DeckDefinition>();
        for (int i = 0; i < count; i += 2) deck.entries.Add(new DeckEntry { card = Card(), count = Math.Min(2, count - i) });
        return deck;
    }
    static BattleStateMachine Battle()
    {
        var b = Component<BattleStateMachine>("FixtureBattle");
        b.config = Config(); b.handManager = Component<HandManager>("FixtureHand");
        b.handManager.config = b.config;
        b.handManager.handRoot = new GameObject("FixtureHandRoot", typeof(RectTransform)).GetComponent<RectTransform>();
        b.handManager.handRoot.gameObject.SetActive(false);
        b.handManager.cardPrefab = new GameObject("FixtureCard", typeof(RectTransform)).AddComponent<CardView>();
        b.handManager.cardPrefab.gameObject.SetActive(false);
        b.handLayout = Component<HandLayoutController>("FixtureLayout");
        b.handManager.handLayout = b.handLayout;
        b.deckManager = Component<DeckManager>("FixtureDeck");
        b.costManager = Component<CostManager>("FixtureCost"); b.costManager.Init(0, 5);
        b.cardDealAnimator = Component<CardDealAnimator>("FixtureDeal");
        b.cardDealAnimator.deck = b.deckManager; b.cardDealAnimator.hand = b.handManager; b.cardDealAnimator.layout = b.handLayout;
        Set(b, "<PlayerHP>k__BackingField", 5); Set(b, "<EnemyHP>k__BackingField", 5);
        Set(b, "<BattleReady>k__BackingField", true);
        Set(b, "initialPlayerMaxHP", 10); Set(b, "initialEnemyMaxHP", 10);
        ((EnemyBattleState)Get(b, "enemyState")).Setup(Deck(), b.config);
        typeof(BattleStateMachine).GetField("<I>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, b);
        return b;
    }
    static CpuDecisionContext Context(List<CardData> hand) => new CpuDecisionContext
    {
        hand = hand, hp = 5, maxHP = 10, opponentHP = 10, opponentMaxHP = 10,
        cost = 0, maxCost = 5, deckRemaining = 12, maxHand = 8
    };
    static void RunChecks()
    {
        try
        {
            UnityEngine.Random.InitState(173);
            var loadedConfig = Resources.Load<CpuBattleConfig>("CPU/DefaultCpuConfig");
            Check(loadedConfig != null && loadedConfig.defaultDeck != null, "Bundled CPU config resolves its deck asset");
            var bundledState = new EnemyBattleState();
            Check(bundledState.Setup(loadedConfig.defaultDeck, Config()) && bundledState.Remaining == 20,
                "Bundled fixed deck has 20 legal cards");
            foreach (var entry in loadedConfig.defaultDeck.entries)
                Check(entry.card != null && entry.card.effect != null, "Bundled card and effect references resolve");
            var config = Config(); var state = new EnemyBattleState(); var deck = Deck();
            Check(state.Setup(deck, config), "CPU deck initializes within shared limits");
            for (int i = 0; i < config.startHandCount; i++) Check(state.TryDraw(), "CPU initial draw " + i);
            Check(state.hand.Count == 4 && state.Remaining == 16, "Initial hand and deck counts");
            for (int i = 0; i < 12; i++) state.StartTurn();
            Check(state.Cost == 5, "CPU cost caps at the player maximum");
            Check(!state.TryPay(6) && state.Cost == 5, "Unaffordable actions cannot spend cost");
            Check(state.TryPay(3) && state.Cost == 2, "CPU card cost is spent once");
            while (state.hand.Count < 8) state.TryDraw();
            int remaining = state.Remaining;
            Check(state.TryDraw() && state.hand.Count == 8 && state.Remaining == remaining, "Full hand skips drawing on CPU");
            state.hand.Clear();
            while (state.Remaining > 0) { state.TryDraw(); state.hand.Clear(); }
            Check(!state.TryDraw(), "Empty CPU deck fails on draw");
            var invalid = Deck(2); invalid.entries[0].count = 3;
            Check(!state.Setup(invalid, config), "CPU rejects too many copies");
            Check(!state.Setup(Deck(22), config), "CPU rejects oversized decks");

            var cheap = Card(2); var expensive = Card(9, cost: 4);
            foreach (CpuDifficulty level in Enum.GetValues(typeof(CpuDifficulty)))
            {
                for (int i = 0; i < 10; i++)
                    Check(EnemyAI.Decide(Context(new List<CardData> { cheap, expensive }), level).card == cheap,
                        level + " only chooses affordable cards " + i);
                Check(EnemyAI.Decide(Context(new List<CardData> { expensive }), level).card == null, level + " passes if no legal card");
            }
            var lethal = Card(0, type: EffectType.DealDamage, amount: 5);
            var ctx = Context(new List<CardData> { cheap, lethal }); ctx.opponentHP = 5;
            var decision = EnemyAI.Decide(ctx, CpuDifficulty.Hard);
            Check(decision.card == lethal && decision.useEffect, "Hard prioritizes lethal effect damage");
            ctx.effectsBlocked = true;
            Check(!EnemyAI.Decide(ctx, CpuDifficulty.Hard).useEffect, "Hand restriction prevents effect choices");
            var weak = Card(3, 1); var sufficient = Card(1, 2); var excessive = Card(5, 6);
            Check(EnemyAI.ChooseDefense(new List<CardData> { weak, sufficient, excessive }, 2, 1f, 5, CpuDifficulty.Hard) == sufficient,
                "Hard saves excess defence and high attack cards");
            Check(EnemyAI.ChooseDefense(new List<CardData> { sufficient }, 0, 1f, 5, CpuDifficulty.Hard) == null, "No defence spent against zero attack");
            var buff = Card(0, 0, type: EffectType.MultiplyNextDamage, multiplier: 3f);
            var next = Card(3, 0);
            ctx = Context(new List<CardData> { buff, next });
            decision = EnemyAI.Decide(ctx, CpuDifficulty.Hard);
            Check(decision.card == buff && decision.useEffect, "Hard values a next-turn buff combination");
            Check(EnemyAI.ChooseDiscard(new List<CardData> { next, next }, next, EffectType.DiscardAttackMinusOneDamage, CpuDifficulty.Hard) == next,
                "Discard selection keeps another copy of the same card eligible");
            Check(!EnemyAI.CanUseEffect(Card(type: EffectType.DiscardAttackMinusOneDamage), 0), "Discard effect requires another card");

            foreach (EffectType type in new[] { EffectType.Heal, EffectType.DealDamage, EffectType.AddCost,
                EffectType.MultiplyNextDamage, EffectType.PierceDefense, EffectType.ReduceEnemyMaxHP,
                EffectType.HandCountMinusTwoDamage, EffectType.HealAndSelfDamageNextTurnEnd })
            {
                var player = Battle(); var cpu = Battle();
                var effect = Card(type: type, amount: 2, multiplier: 2f);
                effect.effect.timing = EffectTiming.NextPlayerTurnEnd;
                Drain((IEnumerator)Invoke(player, "CoApplyCardEffect", effect, false), player);
                Drain((IEnumerator)Invoke(cpu, "CoApplyCardEffect", effect, true), cpu);
                Check(player.PlayerHP == cpu.EnemyHP && player.EnemyHP == cpu.PlayerHP, type + " has mirrored HP results");
                var enemy = (EnemyBattleState)Get(cpu, "enemyState");
                Check((float)Get(player, "pendingNextAttackMultiplier") == enemy.attackMultiplier &&
                    (float)Get(player, "pendingNextDefenseMultiplier") == enemy.defenseMultiplier, type + " has mirrored buffs");
                Check((int)Get(player, "pendingSelfDamageNextPlayerTurnEnd") == enemy.selfDamageNextTurnEnd, type + " has mirrored reservations");
                Check(player.costManager.Current == enemy.Cost, type + " has mirrored resource changes");
            }
            var a = Battle(); var s = (EnemyBattleState)Get(a, "enemyState");
            s.selfDamageNextTurnEnd = 2;
            Drain((IEnumerator)Invoke(a, "CoEndSideEffects", true));
            Check(a.EnemyHP == 5 && s.selfDamageThisTurnEnd == 2 && s.selfDamageNextTurnEnd == 0, "Next-own-turn damage is moved without applying early");
            Drain((IEnumerator)Invoke(a, "CoEndSideEffects", true));
            Check(a.EnemyHP == 3 && s.selfDamageThisTurnEnd == 0, "Reserved damage fires once on the next own turn");
            var restriction = Card(type: EffectType.BlockEffectsAndSelfDamageWhileInHand);
            s.hand.Add(restriction);
            Check(a.GetEnemyActiveEffectText().Contains("しんちょくかくにん"), "CPU restriction appears in character details");
            Drain((IEnumerator)Invoke(a, "CoEndSideEffects", true));
            Check(a.EnemyHP == 2, "CPU hand restriction deals end-turn damage");
            s.hand.Clear();
            Check(!a.GetEnemyActiveEffectText().Contains("しんちょくかくにん"), "CPU details remove expired restrictions");
            a = Battle(); s = (EnemyBattleState)Get(a, "enemyState");
            var draw = Card(type: EffectType.Draw, amount: 2);
            a.deckManager.SetupFromDefinition(Deck());
            Drain((IEnumerator)Invoke(a, "CoApplyCardEffect", draw, false), a);
            Drain((IEnumerator)Invoke(a, "CoApplyCardEffect", draw, true), a);
            Check(a.handManager.handViews.Count == 2 && s.hand.Count == 2, "Draw effect gives both sides equal cards");
            a.handManager.handViews.Clear(); a.deckManager.runtimeDeck.Clear();
            Drain(a.cardDealAnimator.Draw(1));
            Check(a.cardDealAnimator.LastDrawFailed && !a.BattleReady, "Player deck exhaustion ends battle");
            a = Battle(); a.NotifyDeckEmpty(true);
            Check(!a.BattleReady, "CPU deck exhaustion ends battle");

            foreach (bool cpuFirst in new[] { true, false })
            {
                a = Battle(); a.cpuConfig = ScriptableObject.CreateInstance<CpuBattleConfig>();
                a.cpuConfig.defaultDeck = Deck(); a.deckManager.SetupFromDefinition(Deck());
                a.turnSystem.SetFirst(cpuFirst ? TurnOwner.Enemy : TurnOwner.Player);
                Check((bool)Invoke(a, "SetupCpu"), "CPU setup with fixed deck " + cpuFirst);
                a.turnSystem.SetFirst(TurnOwner.Enemy);
                Set(a, "<PlayerHP>k__BackingField", 20);
                a.playerMaxHP = 20;
                Drain((IEnumerator)Invoke(a, "CoEnemyAction"), a);
                Check(a.EnemyDeckRemaining == (cpuFirst ? 16 : 15), "CPU first-turn draw rule " + cpuFirst);
                Check(a.EnemyHandCount == (cpuFirst ? 3 : 4), "CPU consumes exactly one action card " + cpuFirst);
                Check(a.EnemyCost == 1, "CPU gets one cost at turn start " + cpuFirst);
                Check(a.turnSystem.Current == TurnOwner.Player && a.handManager.handViews.Count == 1,
                    "CPU turn ends and player draws " + cpuFirst);
            }
            a = Battle(); s = (EnemyBattleState)Get(a, "enemyState");
            s.hand.Add(Card(1, 3)); s.defenseMultiplier = 2f;
            Drain((IEnumerator)Invoke(a, "CoDefendCpu", 5), a);
            Check(a.EnemyHP == 5 && s.hand.Count == 0 && s.defenseMultiplier == 1f, "CPU defence consumes a card and its buff");
            s.defenseMultiplier = 2f;
            Drain((IEnumerator)Invoke(a, "CoDefendCpu", 1), a);
            Check(a.EnemyHP == 4 && s.defenseMultiplier == 2f, "Skipping defence keeps next-defence buff");
            a = Battle(); a.handManager.AddCard(Card(1, 3));
            Set(a, "pendingNextDefenseMultiplier", 2f);
            Drain((IEnumerator)Invoke(a, "CoDefendPlayer", 5), a);
            Check(a.PlayerHP == 5 && a.handManager.handViews.Count == 0 && (float)Get(a, "pendingNextDefenseMultiplier") == 1f,
                "Player defence mirrors CPU defence and consumes its buff");
            foreach (EffectType discardType in new[] { EffectType.DiscardAttackMinusOneDamage, EffectType.DiscardDefenseMinusOneHeal })
            {
                var p = Battle(); var c = Battle(); var cs = (EnemyBattleState)Get(c, "enemyState");
                p.handManager.AddCard(Card(3, 3)); cs.hand.Add(Card(3, 3));
                var discardEffect = Card(type: discardType);
                Drain((IEnumerator)Invoke(p, "CoApplyCardEffect", discardEffect, false), p);
                Drain((IEnumerator)Invoke(c, "CoApplyCardEffect", discardEffect, true), c);
                Check(p.PlayerHP == c.EnemyHP && p.EnemyHP == c.PlayerHP, discardType + " resolves equally for both sides");
                Check(p.handManager.handViews.Count == 0 && cs.hand.Count == 0, discardType + " consumes one selected card");
            }
            foreach (EffectTiming timing in new[] { EffectTiming.Immediate, EffectTiming.TurnEndBeforeCurse, EffectTiming.NextPlayerTurnEnd })
            {
                var p = Battle(); var c = Battle(); var cs = (EnemyBattleState)Get(c, "enemyState");
                var timedEffect = Card(type: EffectType.HealAndSelfDamageNextTurnEnd, amount: 2, multiplier: 1f);
                timedEffect.effect.timing = timing;
                Drain((IEnumerator)Invoke(p, "CoApplyCardEffect", timedEffect, false), p);
                Drain((IEnumerator)Invoke(c, "CoApplyCardEffect", timedEffect, true), c);
                Drain((IEnumerator)Invoke(p, "CoEndSideEffects", false), p);
                Drain((IEnumerator)Invoke(c, "CoEndSideEffects", true), c);
                Check(p.PlayerHP == c.EnemyHP && (int)Get(p, "pendingSelfDamageThisPlayerTurnEnd") == cs.selfDamageThisTurnEnd,
                    timing + " end-turn timing is symmetric");
            }

            var canvas = new GameObject("RevealCanvas", typeof(RectTransform), typeof(Canvas));
            var reveal = canvas.AddComponent<CpuCardRevealUI>();
            var revealConfig = ScriptableObject.CreateInstance<CpuBattleConfig>();
            reveal.Initialize(canvas.transform, null, null, revealConfig);
            Drain(reveal.Show(cheap, "こうげき", "ATK 2"));
            var root = canvas.transform.Find("CpuCardReveal");
            Check(root != null && !root.gameObject.activeSelf, "Reveal returns and hides after its animation");
            Check(root.Find("Artwork").GetComponent<UnityEngine.UI.Button>() != null &&
                !root.Find("Usage").GetComponent<Image>().raycastTarget,
                "Only the compact artwork accepts inspection clicks");
            Check(root.GetComponentInChildren<TMP_Text>(true).text.Contains("こうげき"), "Reveal identifies how the CPU used the card");
            System.IO.File.WriteAllText("cpu-checks-result.txt", "PASS " + checks);
            Debug.Log("ALL CPU CHECKS PASSED: " + checks);
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            System.IO.File.WriteAllText("cpu-checks-result.txt", e.ToString());
            Debug.LogException(e); EditorApplication.Exit(1);
        }
    }
}
