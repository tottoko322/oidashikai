using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CharacterEffectPanelUI : MonoBehaviour
{
    [Header("References")]
    public GameObject panelRoot;
    public TMP_Text titleText;
    public TMP_Text effectListText;

    private BattleStateMachine battle;
    private Button playerButton;
    private Button enemyButton;
    private Button panelButton;
    private GameObject backdrop;
    private bool showingEnemy;

    public bool IsOpen =>
        panelRoot != null && panelRoot.activeSelf;

    private void Awake()
    {
        if (battle == null) Close();
    }

    public void Initialize(BattleStateMachine owner)
    {
        UnbindPortraits();
        battle = owner;
        if (battle == null || panelRoot == null || effectListText == null)
            return;

        playerButton = BindPortrait(battle.playerHitVfx, ShowPlayerEffects);
        enemyButton = BindPortrait(battle.enemyHitVfx, ShowEnemyEffects);

        // Use the existing scene panel and font, without changing scene assets.
        if (backdrop == null)
        {
            backdrop = new GameObject("CharacterEffectBackdrop",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            backdrop.transform.SetParent(panelRoot.transform.parent, false);
            var rect = (RectTransform)backdrop.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = backdrop.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.45f);
            var button = backdrop.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(Close);

            panelButton = panelRoot.GetComponent<Button>();
            if (panelButton == null)
            {
                panelButton = panelRoot.AddComponent<Button>();
                panelButton.transition = Selectable.Transition.None;
                panelButton.navigation = new Navigation { mode = Navigation.Mode.None };
            }
            panelButton.targetGraphic = panelRoot.GetComponent<Graphic>();
            if (panelButton.targetGraphic != null)
                panelButton.targetGraphic.color = new Color(0.16f, 0.08f, 0.18f, 1f);
            panelButton.onClick.AddListener(Close);

            var panelRect = panelRoot.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(680f, 440f);
            ConfigureText(titleText, new Vector2(24f, -20f), new Vector2(-24f, -100f), 32f, true);
            ConfigureText(effectListText, new Vector2(24f, -112f), new Vector2(-24f, 24f), 28f);
        }

        Close();
    }

    private static Button BindPortrait(CharacterHitVfx vfx, UnityAction action)
    {
        if (vfx == null || vfx.targetImage == null)
            return null;

        var image = vfx.targetImage;
        image.raycastTarget = true;
        var button = image.GetComponent<Button>();
        if (button == null)
        {
            button = image.gameObject.AddComponent<Button>();
            // Preserve the portrait's damage-flash colours.
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
        }
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        return button;
    }

    private static void ConfigureText(TMP_Text text, Vector2 min, Vector2 max, float size, bool heading = false)
    {
        if (text == null) return;
        text.rectTransform.anchorMin = heading ? new Vector2(0f, 1f) : Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = new Vector2(min.x, max.y);
        text.rectTransform.offsetMax = new Vector2(max.x, min.y);
        text.fontSize = size;
        text.enableAutoSizing = true;
        text.fontSizeMin = 18f;
        text.fontSizeMax = size;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.raycastTarget = false;
    }

    public void ShowPlayerEffects() => ShowCharacterEffects(false);
    public void ShowEnemyEffects() => ShowCharacterEffects(true);

    private void ShowCharacterEffects(bool enemy)
    {
        if (battle == null || !battle.BattleReady) return;
        if (IsOpen && showingEnemy == enemy)
        {
            Close();
            return;
        }

        showingEnemy = enemy;
        foreach (var popup in FindObjectsByType<CardPopupUI>(
            FindObjectsInactive.Include))
        {
            if (popup.gameObject.scene == gameObject.scene && popup.IsOpen)
                popup.Close(false);
        }
        RefreshDetails();
        AudioManager.I?.PlayButton();
    }

    private void RefreshDetails()
    {
        Show(showingEnemy ? battle.EnemyCharacterName : battle.PlayerCharacterName, showingEnemy
            ? battle.GetEnemyActiveEffectText()
            : battle.GetPlayerActiveEffectText());
    }

    private void Update()
    {
        if (!IsOpen || battle == null) return;
        if (!battle.BattleReady || Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
            return;
        }
        RefreshDetails();
    }

    public void Show(string characterName, string effects)
    {
        if (panelRoot == null || effectListText == null)
            return;

        if (titleText != null)
            titleText.text = characterName + "の効果  <size=18>クリックで閉じる</size>";

        effectListText.text =
            string.IsNullOrWhiteSpace(effects)
                ? "現在、有効な効果はありません。"
                : effects;

        if (backdrop != null)
        {
            backdrop.SetActive(true);
            backdrop.transform.SetAsLastSibling();
        }
        panelRoot.transform.SetAsLastSibling();
        panelRoot.SetActive(true);
    }

    public void Close()
    {
        if (backdrop != null)
            backdrop.SetActive(false);
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void OnDisable()
    {
        if (backdrop != null) backdrop.SetActive(false);
    }

    private void UnbindPortraits()
    {
        if (playerButton != null) playerButton.onClick.RemoveListener(ShowPlayerEffects);
        if (enemyButton != null) enemyButton.onClick.RemoveListener(ShowEnemyEffects);
    }

    private void OnDestroy()
    {
        UnbindPortraits();
        if (panelButton != null) panelButton.onClick.RemoveListener(Close);
        if (backdrop != null) Destroy(backdrop);
    }
}
