using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Browsing public history does not pause the match or alter turn input locks.
public class BattleHistoryUI : MonoBehaviour
{
    private BattleStateMachine battle;
    private Canvas board;
    private RectTransform overlay, panel, content, detailRoot;
    private Button logButton, discardButton, playerTab, enemyTab;
    private TMP_Text heading, countText, detailCaption;
    private ScrollRect scroll;
    private Image detailImage;
    private bool showingDiscards;
    private BattleSide side = BattleSide.Player;
    private DiscardEntry inspected;
    private bool dirty;
    private GameObject previousSelection;
    public bool IsOpen => overlay != null && overlay.gameObject.activeSelf;
    public bool IsInspecting => detailRoot != null && detailRoot.gameObject.activeSelf;

    private static readonly Color Ink = new Color(.035f, .10f, .18f, .98f);
    private static readonly Color Cyan = new Color(.3f, .88f, 1f);
    private static readonly Color Blue = new Color(.10f, .26f, .40f, 1f);

    public void Initialize(BattleStateMachine owner, Canvas canvas)
    {
        if (battle != null) return;
        battle = owner; board = canvas;
        logButton = Button("BattleLogButton", board.transform, "ログをみる", OpenLog);
        discardButton = Button("DiscardPileButton", board.transform, "ごみばこをみる", OpenDiscards);
        LayoutButtons();
        BuildOverlay();
        battle.History.Changed += MarkDirty;
        MarkDirty();
    }

    private void MarkDirty() => dirty = true;

    private void LayoutButtons()
    {
        var rect = (RectTransform)board.transform;
        float width = Mathf.Clamp(rect.rect.width * .16f, 170f, 280f);
        float height = Mathf.Clamp(rect.rect.height * .055f, 44f, 68f);
        Place(logButton.transform, new Vector2(.02f, .65f), new Vector2(.02f, .65f), Vector2.zero, new Vector2(width, height), new Vector2(0f, .5f));
        Place(discardButton.transform, new Vector2(.02f, .35f), new Vector2(.02f, .35f), Vector2.zero, new Vector2(width, height), new Vector2(0f, .5f));
    }

    private void BuildOverlay()
    {
        overlay = Rect("BattleHistoryOverlay", board.transform);
        Stretch(overlay);
        var canvas = overlay.gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true; canvas.sortingOrder = 32600;
        overlay.gameObject.AddComponent<GraphicRaycaster>();
        var backdrop = overlay.gameObject.AddComponent<Image>(); backdrop.color = new Color(0f, .025f, .07f, .85f);
        overlay.gameObject.AddComponent<Button>().onClick.AddListener(Close);
        panel = Rect("HistoryPanel", overlay);
        Place(panel, new Vector2(.09f, .09f), new Vector2(.91f, .91f), Vector2.zero, Vector2.zero);
        panel.gameObject.AddComponent<Image>().color = Ink;
        panel.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
        var border = panel.gameObject.AddComponent<Outline>(); border.effectColor = Cyan; border.effectDistance = Vector2.one * 2;
        heading = Text("Heading", panel, "たいせんログ", 32);
        Place(heading.transform, new Vector2(0f, 1f), Vector2.one, new Vector2(26f, -24f), new Vector2(-250f, 52f), new Vector2(0f, 1f));
        var close = Button("Close", panel, "とじる", Close);
        Place(close.transform, Vector2.one, Vector2.one, new Vector2(-22f, -20f), new Vector2(150f, 48f), Vector2.one);
        playerTab = Button("PlayerPile", panel, "じぶん", () => SelectPile(BattleSide.Player));
        enemyTab = Button("EnemyPile", panel, "あいて", () => SelectPile(BattleSide.Enemy));
        Place(playerTab.transform, Vector2.up, Vector2.up, new Vector2(24f, -88f), new Vector2(180f, 44f), Vector2.up);
        Place(enemyTab.transform, Vector2.up, Vector2.up, new Vector2(216f, -88f), new Vector2(180f, 44f), Vector2.up);
        countText = Text("Count", panel, "", 22);
        Place(countText.transform, Vector2.up, Vector2.one, new Vector2(24f, -88f), new Vector2(-60f, 44f), Vector2.up);
        countText.alignment = TextAlignmentOptions.MidlineRight;

        var area = Rect("HistoryScroll", panel);
        area.anchorMin = Vector2.zero; area.anchorMax = Vector2.one;
        area.offsetMin = new Vector2(24f, 24f); area.offsetMax = new Vector2(-24f, -150f);
        area.gameObject.AddComponent<Image>().color = new Color(.02f, .065f, .12f);
        scroll = area.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 32f;
        var viewport = Rect("Viewport", area); Stretch(viewport);
        viewport.gameObject.AddComponent<Image>().color = Color.white;
        viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        content = Rect("Content", viewport);
        content.anchorMin = Vector2.up; content.anchorMax = Vector2.one; content.pivot = Vector2.up;
        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 12, 12); layout.spacing = 10f;
        layout.childControlWidth = true; layout.childControlHeight = true;
        layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewport; scroll.content = content;

        detailRoot = Rect("DiscardCardDetails", overlay); Stretch(detailRoot);
        detailRoot.gameObject.AddComponent<Image>().color = new Color(0f, .02f, .05f, .96f);
        detailRoot.gameObject.AddComponent<Button>().onClick.AddListener(CloseDetails);
        detailImage = Rect("Card", detailRoot).gameObject.AddComponent<Image>();
        detailImage.preserveAspect = true; detailImage.raycastTarget = false;
        detailCaption = Text("DetailCaption", detailRoot, "", 24);
        Place(detailCaption.transform, new Vector2(.1f, .015f), new Vector2(.9f, .1f), Vector2.zero, Vector2.zero);
        detailCaption.alignment = TextAlignmentOptions.Center;
        detailRoot.gameObject.SetActive(false);
        overlay.gameObject.SetActive(false);
    }

    public void OpenLog() => Open(false);
    public void OpenDiscards() => Open(true);
    private void Open(bool discards)
    {
        if (overlay == null) return;
        var events = UnityEngine.EventSystems.EventSystem.current;
        previousSelection = events != null ? events.currentSelectedGameObject : null;
        events?.SetSelectedGameObject(null);
        battle.characterEffectPanelUI?.Close();
        battle.handManager?.popupUI?.Close(false);
        battle.cpuCardRevealUI?.CloseDetails();
        showingDiscards = discards; CloseDetails();
        overlay.gameObject.SetActive(true);
        Refresh(); scroll.verticalNormalizedPosition = 1f;
    }

    public void Close()
    {
        CloseDetails();
        if (overlay != null) overlay.gameObject.SetActive(false);
        var events = UnityEngine.EventSystems.EventSystem.current;
        if (events != null && previousSelection != null && previousSelection.activeInHierarchy)
            events.SetSelectedGameObject(previousSelection);
        previousSelection = null;
    }

    public void SelectPile(BattleSide value)
    {
        side = value == BattleSide.Enemy ? BattleSide.Enemy : BattleSide.Player;
        CloseDetails(); Refresh(); scroll.verticalNormalizedPosition = 1f;
    }

    private void Refresh()
    {
        dirty = false;
        discardButton.GetComponentInChildren<TMP_Text>().text = $"ごみばこをみる ({battle.History.Discards.Count})";
        if (!IsOpen) return;
        heading.text = showingDiscards ? "ごみばこ" : "たいせんログ";
        playerTab.gameObject.SetActive(showingDiscards); enemyTab.gameObject.SetActive(showingDiscards);
        playerTab.GetComponent<Image>().color = side == BattleSide.Player ? new Color(.15f,.45f,.62f) : Blue;
        enemyTab.GetComponent<Image>().color = side == BattleSide.Enemy ? new Color(.15f,.45f,.62f) : Blue;
        countText.text = showingDiscards ? $"{BattleHistory.SideName(side)} / {battle.History.DiscardCount(side)}枚" : "新しい記録が上 / ホイールでスクロール";
        foreach (Transform child in content) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        int count = 0;
        if (showingDiscards)
        {
            for (int i = battle.History.Discards.Count - 1; i >= 0; i--)
            {
                var entry = battle.History.Discards[i];
                if (entry.Side != side) continue;
                AddDiscardRow(entry); count++;
            }
        }
        else
        {
            for (int i = battle.History.Logs.Count - 1; i >= 0; i--)
            {
                var entry = battle.History.Logs[i];
                var text = Text("LogEntry", content, $"{TurnLabel(entry.Turn)} / {BattleHistory.SideName(entry.Side)}\n{entry.Message}", 24);
                text.margin = new Vector4(8f, 6f, 8f, 6f); count++;
            }
        }
        if (count == 0) Text("Empty", content, showingDiscards ? "ごみばこはまだありません" : "記録はまだありません", 26);
        Canvas.ForceUpdateCanvases(); LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }

    private void AddDiscardRow(DiscardEntry entry)
    {
        var row = Button("DiscardEntry", content, "", () => Inspect(entry));
        var label = row.GetComponentInChildren<TMP_Text>();
        label.text = $"{BattleHistory.CardName(entry.Card)}\n{TurnLabel(entry.Turn)} / {BattleHistory.ReasonName(entry.Reason)}\nATK {entry.Card.attack} / DEF {entry.Card.defense}\nクリックで拡大";
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = new Vector2(145f, 10f); label.rectTransform.offsetMax = new Vector2(-12f, -10f);
        var height = row.gameObject.AddComponent<LayoutElement>(); height.preferredHeight = 180f;
        var image = Rect("Artwork", row.transform).gameObject.AddComponent<Image>();
        image.sprite = entry.Card.artwork; image.preserveAspect = true; image.raycastTarget = false;
        Place(image.transform, Vector2.zero, Vector2.up, new Vector2(12f, 0f), new Vector2(120f, -12f), new Vector2(0f,.5f));
    }

    public void Inspect(DiscardEntry entry)
    {
        if (!IsOpen || entry == null) return;
        inspected = entry; detailImage.sprite = entry.Card.artwork;
        detailCaption.text = $"{BattleHistory.CardName(entry.Card)} / {BattleHistory.SideName(entry.Side)} / {BattleHistory.ReasonName(entry.Reason)}\nクリック または Esc でもどる";
        detailRoot.gameObject.SetActive(true); LayoutDetail();
    }

    public void CloseDetails() { inspected = null; if (detailRoot != null) detailRoot.gameObject.SetActive(false); }

    private void LayoutDetail()
    {
        float height = detailRoot.rect.height * .82f;
        float width = Mathf.Min(detailRoot.rect.width * .82f, height * .75f);
        Place(detailImage.transform, Vector2.one * .5f, Vector2.one * .5f, new Vector2(0f, 20f), new Vector2(width, height));
    }

    private void Update()
    {
        if (board == null) return;
        LayoutButtons();
        if (dirty) { float position = scroll != null ? scroll.verticalNormalizedPosition : 1f; Refresh(); if (IsOpen) scroll.verticalNormalizedPosition = position; }
        if (IsInspecting) LayoutDetail();
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) { if (IsInspecting) CloseDetails(); else Close(); }
    }

    private void OnDestroy()
    {
        if (battle != null) battle.History.Changed -= MarkDirty;
        if (overlay != null) Destroy(overlay.gameObject);
        if (logButton != null) Destroy(logButton.gameObject);
        if (discardButton != null) Destroy(discardButton.gameObject);
    }

    private static string TurnLabel(int turn) => turn == 0 ? "準備" : $"ターン {turn}";
    private static RectTransform Rect(string name, Transform parent)
    { var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); return (RectTransform)go.transform; }
    private static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    private static void Place(Transform t, Vector2 min, Vector2 max, Vector2 pos, Vector2 size, Vector2? pivot = null)
    { var r = (RectTransform)t; r.anchorMin = min; r.anchorMax = max; r.pivot = pivot ?? Vector2.one * .5f; r.anchoredPosition = pos; r.sizeDelta = size; }
    private static TMP_Text Text(string name, Transform parent, string message, float size)
    {
        var text = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        GameFonts.ApplyJapanese(text); if (text.font == null) GameFonts.Apply(text);
        text.text = message; text.fontSize = size; text.color = new Color(.84f,.96f,1f);
        text.textWrappingMode = TextWrappingModes.Normal; text.alignment = TextAlignmentOptions.TopLeft;
        text.raycastTarget = false; return text;
    }
    private static Button Button(string name, Transform parent, string label, UnityEngine.Events.UnityAction action)
    {
        var rect = Rect(name, parent); var image = rect.gameObject.AddComponent<Image>(); image.color = Blue;
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None }; button.onClick.AddListener(action);
        var text = Text("Label", rect, label, 25); Stretch(text.rectTransform);
        text.rectTransform.offsetMin = new Vector2(6f, 4f); text.rectTransform.offsetMax = new Vector2(-6f,-4f);
        text.alignment = TextAlignmentOptions.Center; text.enableAutoSizing = true; text.fontSizeMin = 16f; text.fontSizeMax = 25f;
        return button;
    }
}

