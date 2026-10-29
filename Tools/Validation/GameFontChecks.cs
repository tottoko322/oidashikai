using System;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class GameFontChecks
{
    static int checks;
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static GameFontChecks()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("FontChecks", false))
            {
                SessionState.SetBool("FontChecks", false); RunChecks();
            }
        };
    }
    public static void Run() { SessionState.SetBool("FontChecks", true); EditorApplication.EnterPlaymode(); }
    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        checks++; Debug.Log("FONT PASS: " + message);
    }
    static void SameFont(TMP_Text text, string name)
    {
        string visible = System.Text.RegularExpressions.Regex.Replace(text.text ?? "", "<[^>]*>", "");
        Check(GameFonts.Default.HasCharacters(visible.Replace("\n", "").Replace("\r", "")), name + " has every glyph in the Complete atlas");
        Check(text.font == GameFonts.Default, name + " uses the shared Complete font");
        Check(text.fontSharedMaterial == GameFonts.Default.material, name + " uses the matching atlas material");
    }
    static void RunChecks()
    {
        try
        {
            var font = GameFonts.Default;
            Check(font != null && font.name == "JikiTourai SDF_Complete", "TMP default resolves the exact requested asset");
            string path = AssetDatabase.GetAssetPath(font);
            Check(AssetDatabase.AssetPathToGUID(path) == "8fcd8a3c408b6c74cad3e00b42088372", "Shared asset GUID matches JikiTourai Complete");
            Check(font.material != null && font.material.shader != null && font.material.mainTexture != null, "Complete font has a shader and atlas");
            Check(font.HasCharacters("せんこうこうこうあなたはせんこうですじゅんばんがきまりましたむずかしさをえらんでくださいやさしいふつうむずかしいキャラクターをえらぶもどる"),
                "Complete font includes Japanese menu and monitor characters");
            Check(font.HasCharacters("CPU HP ATK DEF TURN ORDER 0123456789"), "Complete font includes spaces, battle labels and numbers");
            int glyphs = font.glyphTable.Count, characters = font.characterTable.Count;
            Check(GameFonts.Default == font && font.glyphTable.Count == glyphs && font.characterTable.Count == characters,
                "Repeated font access does not duplicate the space glyph");
            var text = new GameObject("RuntimeText", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            Check(text.font == font, "New TMP objects inherit the Complete default");
            text.fontSize = 31f; text.color = Color.magenta; text.alignment = TextAlignmentOptions.TopLeft;
            var other = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            Check(other != null && other != font, "Validation has an alternate font to test replacement");
            text.font = other; text.fontSharedMaterial = other.material;
            GameFonts.Apply(text); SameFont(text, "Explicitly repaired text");
            Check(text.fontSize == 31f && text.color == Color.magenta && text.alignment == TextAlignmentOptions.TopLeft,
                "Font replacement preserves size, colour and alignment");
            GameFonts.Apply(null);

            var flowObject = new GameObject("Flow"); flowObject.SetActive(false);
            var menu = DifficultySelectUI.Open(flowObject.AddComponent<SceneFlowManager>());
            var menuLabels = menu.GetComponentsInChildren<TMP_Text>(true);
            Check(menuLabels.Length >= 6, "Difficulty dialog creates its labels");
            foreach (var label in menuLabels) SameFont(label, "Difficulty/" + label.transform.parent.name);
            menu.Cancel();

            var canvas = new GameObject("CpuCanvas", typeof(RectTransform), typeof(Canvas));
            text.transform.SetParent(canvas.transform, false);
            text.text = "CPU HP 012";
            text.ForceMeshUpdate(true, true);
            Check(text.textInfo.characterCount == text.text.Length &&
                text.textInfo.characterInfo[text.textInfo.characterCount - 1].character == '2',
                "Text after spaces reaches the generated mesh");
            var reveal = canvas.AddComponent<CpuCardRevealUI>();
            reveal.Initialize(canvas.transform, other, null, null);
            SameFont(canvas.transform.Find("CpuCardReveal").GetComponentInChildren<TMP_Text>(true), "CPU card usage");

            var battleObject = new GameObject("Battle"); battleObject.SetActive(false);
            var battle = battleObject.AddComponent<BattleStateMachine>();
            var hp = new GameObject("HP", typeof(RectTransform)); hp.transform.SetParent(canvas.transform, false);
            battle.playerHpText = hp.AddComponent<TextMeshProUGUI>(); battle.playerHpText.font = other;
            var enemyHp = new GameObject("EnemyHP", typeof(RectTransform));
            enemyHp.transform.SetParent(canvas.transform, false);
            battle.enemyHpText = enemyHp.AddComponent<TextMeshProUGUI>();
            battle.enemyHpText.rectTransform.sizeDelta = new Vector2(100f, 80f);
            battle.enemyHpText.rectTransform.anchoredPosition = new Vector2(300f, 200f);
            battle.deckCountText = text;
            text.rectTransform.sizeDelta = new Vector2(240f, 60f);
            text.text = "やまふだ 13";
            typeof(BattleStateMachine).GetMethod("SetupCpuUI", Private).Invoke(battle, null);
            SameFont(battle.enemyInfoText, "CPU resources");
            typeof(BattleStateMachine).GetMethod("RefreshCpuUI", Private).Invoke(battle, null);
            Check(battle.enemyInfoText.text == "やまふだ 0", "CPU resources contain only the deck count");
            Check(battle.enemyInfoText.fontSize == text.fontSize && battle.enemyInfoText.color == text.color &&
                battle.enemyInfoText.alignment == text.alignment && battle.enemyInfoText.rectTransform.rect.size == text.rectTransform.rect.size,
                "CPU deck label inherits the player deck label styling and dimensions");
            Check(text.text == "やまふだ 13", "CPU refresh preserves the player deck label");
            var deckCorners = new Vector3[4]; var hpCorners = new Vector3[4];
            battle.enemyInfoText.rectTransform.GetWorldCorners(deckCorners);
            battle.enemyHpText.rectTransform.GetWorldCorners(hpCorners);
            Check(deckCorners[0].x > hpCorners[3].x, "CPU deck label sits to the right of HP without overlap");
            Check(Mathf.Approximately((deckCorners[0].y + deckCorners[1].y) * 0.5f,
                (hpCorners[0].y + hpCorners[1].y) * 0.5f), "CPU deck label is vertically centred on HP");
            battle.enemyHpText.text = "9";
            GameFonts.Apply(battle.enemyHpText);
            battle.enemyHpText.rectTransform.sizeDelta = new Vector2(600f, 80f);
            typeof(BattleStateMachine).GetMethod("PositionCpuDeckCount", Private).Invoke(battle, null);
            Vector3 digitEdge = battle.enemyHpText.rectTransform.TransformPoint(new Vector3(
                battle.enemyHpText.textBounds.max.x, battle.enemyHpText.textBounds.center.y, 0f));
            Check(Mathf.Approximately(battle.enemyInfoText.rectTransform.position.x - digitEdge.x, 24f),
                "A wide HP rectangle does not push the deck label beyond the visible digits");
            battle.enemyActionText = new GameObject("LegacyAction").AddComponent<TextMeshProUGUI>();
            typeof(BattleStateMachine).GetMethod("ShowEnemyAction", Private).Invoke(battle, new object[] { "CPU action" });
            Check(battle.enemyActionText.text == "" && !battle.enemyActionText.enabled,
                "Legacy CPU action text stays hidden");
            var legacyRoot = new GameObject("LegacyEffects");
            battle.playerActiveEffectDisplay = legacyRoot.AddComponent<ActiveEffectDisplay>();
            battle.playerActiveEffectDisplay.root = legacyRoot;
            battle.playerActiveEffectDisplay.effectText = legacyRoot.AddComponent<TextMeshProUGUI>();
            typeof(BattleStateMachine).GetField("pendingNextAttackMultiplier", Private).SetValue(battle, 2f);
            typeof(BattleStateMachine).GetMethod("RefreshActiveEffectUI", Private).Invoke(battle, null);
            Check(!legacyRoot.activeSelf && battle.GetPlayerActiveEffectText().Contains("x2"),
                "Hiding the permanent effect panel preserves character effect details");
            string status = (string)typeof(BattleStateMachine).GetMethod("StatusText", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { 2f, 2f, 1, 2, true, 3, 20 });
            Check(font.HasCharacters(status.Replace("\n", "").Replace(" ", "")), "Every active-effect status glyph is present");
            Check(!System.Text.RegularExpressions.Regex.IsMatch(status, "[一-龯]"), "Status details use kana wording");
            Check(battle.enemyActiveEffectDisplay == null, "No permanent CPU effect label is generated");

            var monitor = new GameObject("Monitor").AddComponent<TurnOrderSlotUI>();
            var animation = monitor.Play(true, 1f, 1f, 14, null, other);
            Check(animation.MoveNext(), "Monitor creates its scrolling labels");
            var root = (GameObject)typeof(TurnOrderSlotUI).GetField("root", Private).GetValue(monitor);
            var monitorLabels = root.GetComponentsInChildren<TMP_Text>(true);
            Check(monitorLabels.Length >= 8, "Monitor has title, reel and result text");
            foreach (var label in monitorLabels) SameFont(label, "Monitor/" + label.name);
            ((IDisposable)animation).Dispose();
            System.IO.File.WriteAllText("font-checks-result.txt", "PASS " + checks);
            Debug.Log("ALL FONT CHECKS PASSED: " + checks);
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            System.IO.File.WriteAllText("font-checks-result.txt", e.ToString());
            Debug.LogException(e); EditorApplication.Exit(1);
        }
    }
}

