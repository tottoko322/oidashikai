using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DefenseSelectUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject root;
    public TMP_Text message;
    public Button skipButton;

    private bool waiting;
    private GameObject promptPanel;

    private void SetupPrompt()
    {
        if (message == null || promptPanel != null) return;
        var canvas = message.GetComponentInParent<Canvas>();
        if (canvas == null) return;
        promptPanel = new GameObject("DefensePrompt", typeof(RectTransform), typeof(Image));
        var rect = promptPanel.GetComponent<RectTransform>(); rect.SetParent(canvas.transform, false);
        float width = ((RectTransform)canvas.transform).rect.width;
        rect.sizeDelta = new Vector2(Mathf.Min(660f, width * 0.65f), 76f);
        rect.anchoredPosition = Vector2.zero;
        var image = promptPanel.GetComponent<Image>(); image.color = new Color(0.05f, 0.22f, 0.35f, 0.97f); image.raycastTarget = false;
        message.rectTransform.SetParent(rect, false);
        message.rectTransform.anchorMin = Vector2.zero; message.rectTransform.anchorMax = Vector2.one;
        message.rectTransform.offsetMin = new Vector2(12f, 6f); message.rectTransform.offsetMax = new Vector2(-12f, -6f);
        GameFonts.Apply(message); message.color = Color.white; message.fontSize = 26f;
        message.enableAutoSizing = true; message.fontSizeMin = 18f; message.fontSizeMax = 26f;
        message.alignment = TextAlignmentOptions.Center; message.raycastTarget = false;
    }

    private void Awake()
    {
        if (root != null)
            root.SetActive(false);

        if (skipButton != null)
        {
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(OnSkip);
        }
    }

    public void BeginSelection(string msg = "ぼうぎょするか、すきっぷをえらんでください")
    {
        waiting = true;
        SetupPrompt();
        if (promptPanel != null) promptPanel.SetActive(true);

        if (root != null)
            root.SetActive(true);

        if (message != null)
        {
            message.enabled = true;
            message.text = string.IsNullOrWhiteSpace(msg)
                ? "ぼうぎょするか、すきっぷをえらんでください" : msg;
        }
    }

    public void EndSelection()
    {
        waiting = false;
        if (promptPanel != null) promptPanel.SetActive(false);

        if (root != null)
            root.SetActive(false);
    }

    private void OnDestroy() { if (promptPanel != null) Destroy(promptPanel); }

    public void SelectDefense(CardView view)
    {
        if (!waiting) return;

        BattleStateMachine.I?.TrySelectDefenseByDrop(view);
    }

    public void OnSkip()
    {
        if (!waiting) return;

        Debug.Log("[DefenseUI] Skip button pressed");

        // ここから1回だけ呼ぶ
        BattleStateMachine.I?.SkipDefense();
    }
}
