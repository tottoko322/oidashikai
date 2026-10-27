using System;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class BattleSelectionChecks
{
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    const string PreferenceKey = "qpic_cpu_difficulty";
    static int checks;
    static BattleSelectionChecks()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("SelectionChecks", false))
            {
                SessionState.SetBool("SelectionChecks", false);
                RunChecks();
            }
        };
    }
    public static void Run() { SessionState.SetBool("SelectionChecks", true); EditorApplication.EnterPlaymode(); }
    static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
        checks++; Debug.Log("SELECTION PASS: " + message);
    }
    static void ResetTransition() => typeof(SceneFlowManager).GetMethod("ResetTransition", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
    static T Component<T>(string name) where T : Component
    {
        var root = new GameObject(name); root.SetActive(false); return root.AddComponent<T>();
    }
    static CharacterData Character(string name, CoinFace face)
    {
        var character = ScriptableObject.CreateInstance<CharacterData>();
        character.characterId = character.displayName = name;
        character.faceForFirst = face;
        var texture = new Texture2D(2, 2);
        character.portrait = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), Vector2.one * 0.5f);
        character.distributedDeck = ScriptableObject.CreateInstance<DeckDefinition>();
        var card = ScriptableObject.CreateInstance<CardData>(); card.displayName = name;
        character.distributedDeck.entries.Add(new DeckEntry { card = card, count = 2 });
        return character;
    }
    static CharacterHitVfx Portrait(string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.SetActive(false);
        var vfx = go.AddComponent<CharacterHitVfx>(); vfx.targetImage = go.GetComponent<Image>();
        return vfx;
    }
    static void RunChecks()
    {
        bool hadPreference = PlayerPrefs.HasKey(PreferenceKey);
        int previousPreference = PlayerPrefs.GetInt(PreferenceKey, 1);
        try
        {
            PlayerPrefs.DeleteKey(PreferenceKey); ResetTransition();
            Check(!BattlePreferences.HasDifficulty && BattlePreferences.GetDifficulty() == CpuDifficulty.Normal,
                "Difficulty defaults to Normal before a choice is saved");
            var pico = Character("pico", CoinFace.Q); var nano = Character("nano", CoinFace.I);
            var flow = Component<SelectionFlowSpy>("Flow1");
            var secondFlow = Component<SelectionFlowSpy>("Flow2");
            var manager = Component<CharacterSelectManager>("CharacterManager");
            manager.characters = new[] { pico, nano };
            manager.carousel = Component<CarouselController>("Carousel");
            manager.carousel.Init(2); manager.carousel.Select(1); manager.flow = flow;
            Check(manager.ConfirmSelection(), "Confirm accepts the selected Nano character");
            Check(SelectedCharacterContext.I != null && SelectedCharacterContext.I.selected == nano,
                "Character choice persists independently of the scene manager");
            Check(SelectedCharacterContext.I.opponent == pico, "CPU uses the other character");
            var battle = Component<BattleStateMachine>("BattleFixture");
            battle.playerHitVfx = Portrait("PlayerPortrait"); battle.enemyHitVfx = Portrait("EnemyPortrait");
            battle.ApplyCharacterSelection();
            Check(battle.playerHitVfx.targetImage.sprite == nano.portrait, "Battle player image uses selected Nano portrait");
            Check(battle.enemyHitVfx.targetImage.sprite == pico.portrait, "Battle CPU image uses Pico portrait");
            Check(battle.PlayerCharacterName == "nano" && battle.EnemyCharacterName == "pico", "Effect detail titles use chosen character names");
            var decider = Component<FirstTurnDecider>("Decider");
            Check(decider.DecidePlayerFirst(battle.PlayerCharacter, CoinFace.I) &&
                !decider.DecidePlayerFirst(battle.PlayerCharacter, CoinFace.Q), "Selected Nano coin rule is respected");
            var deck = Component<DeckManager>("PlayerDeck");
            deck.forcedDeckDefinition = pico.distributedDeck;
            Check(deck.SetupForBattle(pico.distributedDeck) && deck.runtimeDeck[0].displayName == "nano",
                "Selected distributed deck takes priority over the fixed scene deck");
            Check(deck.TryDraw(out var first) && first.displayName == "nano", "Selected deck supplies the actual drawn cards");

            manager.OnConfirm(); secondFlow.GoBattle();
            Check(flow.loaded.Count == 1 && flow.loaded[0] == "Battle" && secondFlow.loaded.Count == 0,
                "Duplicate confirm listeners produce only one battle transition");
            ResetTransition();
            manager.carousel.Select(0); manager.ConfirmSelection(); battle.ApplyCharacterSelection();
            Check(battle.playerHitVfx.targetImage.sprite == pico.portrait && battle.enemyHitVfx.targetImage.sprite == nano.portrait,
                "Switching to Pico swaps both battle portraits");
            Check(decider.DecidePlayerFirst(battle.PlayerCharacter, CoinFace.Q), "Pico retains its own coin rule");
            manager.carousel.Select(1);
            typeof(CharacterSelectManager).GetMethod("Start", Private).Invoke(manager, null);
            Check(manager.carousel.Index == 0, "Returning to selection restores the previous character");
            manager.OnNext(); Check(manager.carousel.Index == 1, "Next changes selected character");
            manager.OnNext(); Check(manager.carousel.Index == 0, "Character selection wraps at the end");
            manager.OnPrev(); Check(manager.carousel.Index == 1, "Character selection wraps backwards");
            manager.characters = new CharacterData[0];
            Check(!manager.ConfirmSelection(), "Empty character lists cannot confirm");
            manager.characters = new[] { pico, nano };

            var mode = new GameObject("Mode").AddComponent<ModeContext>();
            mode.mode = GameMode.DeckBuildBattle;
            var constructed = new GameObject("ConstructedDeck").AddComponent<DeckRuntimeContext>();
            var custom = ScriptableObject.CreateInstance<CardData>(); custom.displayName = "constructed";
            constructed.builtDeck.Add(custom);
            Check(deck.SetupForBattle(pico.distributedDeck) && deck.runtimeDeck[0] == custom,
                "A constructed deck is preserved when changing portrait");
            mode.mode = GameMode.DistributedDeckBattle;
            SelectedCharacterContext.I.Select(null, null);
            Check(deck.SetupForBattle(pico.distributedDeck) && deck.runtimeDeck[0].displayName == "pico",
                "Opening Battle directly keeps its fallback deck");
            SelectedCharacterContext.I.Select(nano, pico);

            if (EventSystem.current == null) new GameObject("EventSystem", typeof(EventSystem));
            var startButton = new GameObject("Start", typeof(RectTransform), typeof(Button)).GetComponent<Button>();
            EventSystem.current.SetSelectedGameObject(startButton.gameObject);
            flow.loaded.Clear(); flow.GoCharacterSelect();
            var ui = UnityEngine.Object.FindAnyObjectByType<DifficultySelectUI>();
            Check(ui != null && ui.gameObject.activeSelf && flow.loaded.Count == 0,
                "Start opens difficulty selection before loading CharacterSelect");
            Check(ui.Selected == CpuDifficulty.Normal, "Difficulty dialog selects the Normal default");
            var root = ui.transform;
            Check(root.Find("Backdrop").GetComponent<Image>().raycastTarget, "Difficulty backdrop blocks underlying buttons");
            var option = root.Find("Panel/Difficulty0").GetComponent<Button>();
            Check(option.navigation.mode == Navigation.Mode.Explicit, "Keyboard navigation stays within the difficulty dialog");
            ui.Select(CpuDifficulty.Hard); ui.Cancel();
            Check(!BattlePreferences.HasDifficulty && !ui.gameObject.activeSelf, "Cancel does not save a pending difficulty choice");
            Check(EventSystem.current.currentSelectedGameObject == startButton.gameObject, "Cancel restores focus to Start");
            flow.GoCharacterSelect();
            Check(ui.gameObject.activeSelf && ui.Selected == CpuDifficulty.Normal, "Reopening a cancelled dialog restores saved/default difficulty");
            flow.GoCharacterSelect();
            Check(UnityEngine.Object.FindObjectsByType<DifficultySelectUI>(FindObjectsInactive.Include).Length == 1,
                "Repeated Start reuses a single difficulty dialog");
            root.Find("Panel/Difficulty2").GetComponent<Button>().onClick.Invoke();
            Check(ui.Selected == CpuDifficulty.Hard, "Hard button changes the chosen difficulty");
            root.Find("Panel/Continue").GetComponent<Button>().onClick.Invoke();
            Check(BattlePreferences.HasDifficulty && BattlePreferences.GetDifficulty() == CpuDifficulty.Hard,
                "Continue persists the difficulty before changing scenes");
            Check(!ui.gameObject.activeSelf && flow.loaded.Count == 1 && flow.loaded[0] == "CharacterSelect",
                "Continue loads CharacterSelect once");
            ui.Confirm(); Check(flow.loaded.Count == 1, "Repeated confirmation cannot load another scene");

            battle.config = ScriptableObject.CreateInstance<GameConfig>(); battle.config.deckLimit = 20;
            battle.cpuDifficulty = CpuDifficulty.Easy;
            Check((bool)typeof(BattleStateMachine).GetMethod("SetupCpu", Private).Invoke(battle, null) &&
                battle.cpuDifficulty == CpuDifficulty.Hard, "Battle uses saved difficulty instead of its Inspector default");
            ResetTransition(); flow.GoCharacterSelect();
            Check(ui.Selected == CpuDifficulty.Hard, "Next title visit restores saved Hard difficulty");
            ui.Select(CpuDifficulty.Easy); ui.Confirm();
            Check(BattlePreferences.GetDifficulty() == CpuDifficulty.Easy, "Saved difficulty can be changed to Easy");
            PlayerPrefs.SetInt(PreferenceKey, 99);
            Check(BattlePreferences.GetDifficulty() == CpuDifficulty.Normal, "Invalid saved difficulty falls back safely");
            BattlePreferences.SetDifficulty((CpuDifficulty)(-1));
            Check(BattlePreferences.GetDifficulty() == CpuDifficulty.Normal, "Invalid difficulty selections are normalized");
            ResetTransition(); flow.loaded.Clear(); flow.GoTitle();
            Check(flow.loaded.Count == 1 && flow.loaded[0] == "title", "Return uses the actual title scene name");
            ResetTransition();
            System.IO.File.WriteAllText("selection-checks-result.txt", "PASS " + checks);
            Debug.Log("ALL SELECTION CHECKS PASSED: " + checks);
        }
        catch (Exception e)
        {
            System.IO.File.WriteAllText("selection-checks-result.txt", e.ToString());
            Debug.LogException(e);
            Restore(hadPreference, previousPreference);
            EditorApplication.Exit(1); return;
        }
        Restore(hadPreference, previousPreference);
        EditorApplication.Exit(0);
    }
    static void Restore(bool existed, int value)
    {
        if (existed) PlayerPrefs.SetInt(PreferenceKey, value); else PlayerPrefs.DeleteKey(PreferenceKey);
        PlayerPrefs.Save();
    }
}
