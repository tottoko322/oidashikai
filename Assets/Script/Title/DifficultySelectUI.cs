using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DifficultySelectUI : MonoBehaviour
{
    private SceneFlowManager flow;
    private CpuDifficulty selected;
    private readonly Button[] options = new Button[3];
    private Button continueButton;
    private GameObject previousSelection;
    public CpuDifficulty Selected => selected;

    public static DifficultySelectUI Open(SceneFlowManager owner)
    {
        foreach (var panel in FindObjectsByType<DifficultySelectUI>(FindObjectsInactive.Include))
        {
            if (panel.gameObject.scene == owner.gameObject.scene)
            {
                panel.Show(owner);
                return panel;
            }
        }
        var root = new GameObject("DifficultySelection", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        // Place the panel in the caller's scene, including additive editor previews.
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, owner.gameObject.scene);
        var ui = root.AddComponent<DifficultySelectUI>();
        ui.Build(owner);
        ui.Show(owner);
        return ui;
    }

    private void Build(SceneFlowManager owner)
    {
        var canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30000;
        var scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        var backdrop = Image("Backdrop", transform, new Color(0.03f, 0.02f, 0.06f, 0.9f));
        backdrop.rectTransform.anchorMin = Vector2.zero;
        backdrop.rectTransform.anchorMax = Vector2.one;
        backdrop.rectTransform.sizeDelta = Vector2.zero;
        var panel = Image("Panel", transform, new Color(0.13f, 0.09f, 0.19f, 1f));
        panel.rectTransform.sizeDelta = new Vector2(760f, 620f);
        Label("Heading", panel.transform, "むずかしさをえらんでください", new Vector2(0f, 246f), new Vector2(700f, 60f), 36f);

        string[] names = { "やさしい", "ふつう", "むずかしい" };
        string[] descriptions = { "きがるにあそびたいひとむけ", "コウゲキ・カイフク・コウカをかんがえます", "カードのくみあわせや、のこしかたもかんがえます" };
        for (int i = 0; i < 3; i++)
        {
            int index = i;
            var button = MakeButton("Difficulty" + i, panel.transform,
                $"{names[i]}\n<size=19>{descriptions[i]}</size>",
                new Vector2(0f, 138f - i * 112f), new Vector2(650f, 94f));
            button.onClick.AddListener(() => Select((CpuDifficulty)index));
            options[i] = button;
        }
        var back = MakeButton("Back", panel.transform, "もどる", new Vector2(-235f, -242f), new Vector2(170f, 64f));
        back.onClick.AddListener(Cancel);
        continueButton = MakeButton("Continue", panel.transform, "キャラクターをえらぶ",
            new Vector2(110f, -242f), new Vector2(420f, 64f));
        continueButton.onClick.AddListener(Confirm);
        for (int i = 0; i < options.Length; i++)
            options[i].navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = i > 0 ? options[i - 1] : continueButton,
                selectOnDown = i < options.Length - 1 ? options[i + 1] : continueButton
            };
        back.navigation = new Navigation { mode = Navigation.Mode.Explicit,
            selectOnUp = options[2], selectOnRight = continueButton, selectOnDown = options[0] };
        continueButton.navigation = new Navigation { mode = Navigation.Mode.Explicit,
            selectOnUp = options[2], selectOnLeft = back, selectOnDown = options[0] };
    }

    private void Show(SceneFlowManager owner)
    {
        flow = owner;
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null &&
            !EventSystem.current.currentSelectedGameObject.transform.IsChildOf(transform))
            previousSelection = EventSystem.current.currentSelectedGameObject;
        Select(BattlePreferences.GetDifficulty());
    }

    public void Select(CpuDifficulty difficulty)
    {
        selected = difficulty >= CpuDifficulty.Easy && difficulty <= CpuDifficulty.Hard
            ? difficulty : CpuDifficulty.Normal;
        for (int i = 0; i < options.Length; i++)
        {
            if (options[i] == null) continue;
            options[i].GetComponent<Image>().color = i == (int)selected
                ? new Color(0.5f, 0.23f, 0.65f, 1f) : new Color(0.23f, 0.19f, 0.31f, 1f);
        }
        if (EventSystem.current != null && options[(int)selected] != null)
            EventSystem.current.SetSelectedGameObject(options[(int)selected].gameObject);
    }

    public void Confirm()
    {
        if (!gameObject.activeSelf) return;
        BattlePreferences.SetDifficulty(selected);
        AudioManager.I?.PlayButton();
        gameObject.SetActive(false);
        flow?.ContinueToCharacterSelect();
    }

    public void Cancel()
    {
        AudioManager.I?.PlayButton();
        gameObject.SetActive(false);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(previousSelection);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Cancel();
    }

    private static Image Image(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private TMP_Text Label(string name, Transform parent, string value, Vector2 position, Vector2 size, float fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<TextMeshProUGUI>();
        GameFonts.Apply(text); text.text = value;
        text.fontSize = fontSize; text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.rectTransform.sizeDelta = size;
        text.rectTransform.anchoredPosition = position;
        return text;
    }

    private Button MakeButton(string name, Transform parent, string text, Vector2 position, Vector2 size)
    {
        var background = Image(name, parent, new Color(0.25f, 0.2f, 0.34f, 1f));
        background.rectTransform.anchoredPosition = position;
        background.rectTransform.sizeDelta = size;
        var button = background.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        Label("Label", background.transform, text, Vector2.zero, size - new Vector2(20f, 8f), 28f);
        return button;
    }
}
