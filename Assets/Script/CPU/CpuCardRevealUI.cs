using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CpuCardRevealUI : MonoBehaviour
{
    private RectTransform root;
    private CanvasGroup group;
    private Image artwork;
    private TMP_Text label;
    private CpuBattleConfig settings;
    private RectTransform origin;
    private Vector2 returnPosition;
    private float shownScale;
    private bool returning;
    private bool positioned;
    private GameObject details;
    private RectTransform detailCanvas, detailCard, detailHint;
    public bool IsDetailOpen => details != null && details.activeSelf;
    public bool IsVisible => root != null && root.gameObject.activeSelf;

    public void Initialize(Transform canvas, TMP_FontAsset font, RectTransform enemyOrigin, CpuBattleConfig config)
    {
        settings = config; origin = enemyOrigin;
        var go = new GameObject("CpuCardReveal", typeof(RectTransform), typeof(CanvasGroup));
        root = go.GetComponent<RectTransform>();
        root.SetParent(canvas, false);
        root.sizeDelta = new Vector2(300f, 420f);
        group = go.GetComponent<CanvasGroup>();
        group.blocksRaycasts = group.interactable = true;
        var imageObject = new GameObject("Artwork", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(root, false);
        var imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero; imageRect.anchorMax = Vector2.one;
        imageRect.sizeDelta = Vector2.zero;
        artwork = imageObject.GetComponent<Image>();
        artwork.preserveAspect = true; artwork.raycastTarget = true;
        var inspect = imageObject.AddComponent<Button>();
        inspect.transition = Selectable.Transition.None;
        inspect.navigation = new Navigation { mode = Navigation.Mode.None };
        inspect.onClick.AddListener(OpenDetails);
        var titleObject = new GameObject("Usage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        titleObject.transform.SetParent(root, false);
        var titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.sizeDelta = new Vector2(360f, 76f);
        titleRect.anchoredPosition = new Vector2(0f, 250f);
        titleObject.GetComponent<Image>().color = new Color(0.06f, 0.22f, 0.34f, 0.97f);
        titleObject.GetComponent<Image>().raycastTarget = false;
        var textObject = new GameObject("Text", typeof(RectTransform));
        textObject.transform.SetParent(titleRect, false);
        label = textObject.AddComponent<TextMeshProUGUI>();
        GameFonts.Apply(label);
        label.fontSize = 26f;
        label.enableAutoSizing = true; label.fontSizeMin = 18f; label.fontSizeMax = 26f;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.sizeDelta = Vector2.zero;
        root.gameObject.SetActive(false);
    }

    public IEnumerator Show(CardData card, string usage, string detail = "", IEnumerator whileVisible = null)
    {
        if (root == null || card == null) yield break;
        returning = false;
        positioned = false;
        artwork.sprite = card.artwork;
        label.text = $"CPUの{usage}\n{detail}\n<size=18>クリックでくわしく</size>";
        label.color = usage == "コウゲキ" ? new Color(1f, 0.65f, 0.55f)
            : usage == "ボウギョ" ? new Color(0.55f, 0.85f, 1f) : new Color(0.95f, 0.65f, 1f);
        var parent = (RectTransform)root.parent;
        Vector2 start = origin != null ? (Vector2)parent.InverseTransformPoint(origin.position)
            - parent.rect.center : new Vector2(0f, 420f);
        float scale = settings != null ? Mathf.Max(0.1f, settings.revealScale) : 0.7f;
        Vector2 target = RevealPosition(parent, scale);
        returnPosition = start;
        shownScale = scale;
        root.SetAsLastSibling();
        root.gameObject.SetActive(true);
        try
        {
            yield return Animate(start, target, 0.15f, scale, 0f, 1f,
                settings != null ? settings.enterDuration : 0.16f);
            positioned = true;
            if (whileVisible != null) yield return whileVisible;
            else
            {
                float remaining = settings != null ? Mathf.Max(0f, settings.holdDuration) : 1.5f;
                while (remaining > 0f || IsDetailOpen)
                {
                    if (!IsDetailOpen) remaining -= Time.unscaledDeltaTime;
                    yield return null;
                }
            }
            yield return ReturnToOrigin();
        }
        finally { CloseDetails(); if (root != null) root.gameObject.SetActive(false); }
    }

    private Vector2 RevealPosition(RectTransform parent, float scale)
    {
        Vector2 position = settings != null ? settings.revealPosition : new Vector2(-500f, 300f);
        if (origin != null)
        {
            var corners = new Vector3[4]; origin.GetWorldCorners(corners);
            float left = float.MaxValue, bottom = float.MaxValue, top = float.MinValue;
            foreach (var corner in corners)
            {
                var local = parent.InverseTransformPoint(corner);
                left = Mathf.Min(left, local.x); bottom = Mathf.Min(bottom, local.y); top = Mathf.Max(top, local.y);
            }
            position = new Vector2(left - root.rect.width * scale * 0.5f - 24f, (bottom + top) * 0.5f);
        }
        // Keep both the card and its usage header inside the canvas.
        float halfWidth = 180f * scale;
        position.x = Mathf.Clamp(position.x, parent.rect.xMin + halfWidth + 20f, parent.rect.xMax - halfWidth - 20f);
        position.y = Mathf.Clamp(position.y, parent.rect.yMin + 210f * scale + 20f,
            parent.rect.yMax - 288f * scale - 20f);
        return position;
    }

    public IEnumerator ReturnToOrigin()
    {
        if (!IsVisible) yield break;
        returning = true;
        CloseDetails();
        yield return Animate(root.anchoredPosition, returnPosition, shownScale, 0.1f, 1f, 0f,
            settings != null ? settings.returnDuration : 0.18f);
        if (root != null) root.gameObject.SetActive(false);
    }

    public void OpenDetails()
    {
        if (!IsVisible || IsDetailOpen || returning) return;
        var canvas = root.GetComponentInParent<Canvas>().rootCanvas;
        detailCanvas = (RectTransform)canvas.transform;
        details = new GameObject("CpuCardDetails", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        var rect = details.GetComponent<RectTransform>();
        rect.SetParent(canvas.transform, false);
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        var overlay = details.GetComponent<Canvas>(); overlay.overrideSorting = true; overlay.sortingOrder = 32000;
        var backdrop = new GameObject("CloseBackdrop", typeof(RectTransform), typeof(Image), typeof(Button));
        var backRect = backdrop.GetComponent<RectTransform>(); backRect.SetParent(rect, false);
        backRect.anchorMin = Vector2.zero; backRect.anchorMax = Vector2.one; backRect.offsetMin = backRect.offsetMax = Vector2.zero;
        backdrop.GetComponent<Image>().color = new Color(0.015f, 0.045f, 0.09f, 0.88f);
        backdrop.GetComponent<Button>().onClick.AddListener(CloseDetails);
        var card = new GameObject("Card", typeof(RectTransform), typeof(Image), typeof(Button));
        var cardRect = card.GetComponent<RectTransform>(); cardRect.SetParent(rect, false);
        detailCard = cardRect;
        float height = Mathf.Min(((RectTransform)canvas.transform).rect.height - 140f, 760f);
        height = Mathf.Max(180f, Mathf.Min(height, (((RectTransform)canvas.transform).rect.width - 80f) * 1.4f));
        cardRect.sizeDelta = new Vector2(height / 1.4f, height);
        var image = card.GetComponent<Image>(); image.sprite = artwork.sprite; image.preserveAspect = true;
        card.GetComponent<Button>().onClick.AddListener(CloseDetails);
        var hint = new GameObject("CloseHint", typeof(RectTransform)); hint.transform.SetParent(rect, false);
        var text = hint.AddComponent<TextMeshProUGUI>(); GameFonts.Apply(text);
        text.text = "クリック / Esc でとじる"; text.fontSize = 24f; text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
        text.rectTransform.sizeDelta = new Vector2(600f, 44f);
        text.rectTransform.anchoredPosition = new Vector2(0f, -height * 0.5f - 30f);
        detailHint = text.rectTransform;
        FitDetails();
    }

    private void FitDetails()
    {
        if (!IsDetailOpen || detailCanvas == null || detailCard == null) return;
        float height = Mathf.Max(1f, Mathf.Min(760f, detailCanvas.rect.height - 140f,
            (detailCanvas.rect.width - 80f) * 1.4f));
        detailCard.sizeDelta = new Vector2(height / 1.4f, height);
        detailHint.sizeDelta = new Vector2(Mathf.Min(600f, detailCanvas.rect.width - 24f), 44f);
        detailHint.anchoredPosition = new Vector2(0f, -height * 0.5f - 30f);
    }

    public void CloseDetails()
    {
        if (details == null) return;
        details.SetActive(false); Destroy(details); details = null;
    }

    private void Update()
    {
        if (positioned && IsVisible && !returning)
            root.anchoredPosition = RevealPosition((RectTransform)root.parent, shownScale);
        if (!IsDetailOpen) return;
        FitDetails();
        if (Input.GetKeyDown(KeyCode.Escape)) CloseDetails();
    }

    private IEnumerator Animate(Vector2 from, Vector2 to, float fromScale, float toScale,
        float fromAlpha, float toAlpha, float duration)
    {
        duration = Mathf.Max(0.01f, duration);
        float t = 0f;
        while (t < duration)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / duration);
            root.anchoredPosition = Vector2.Lerp(from, to, k);
            root.localScale = Vector3.one * Mathf.Lerp(fromScale, toScale, k);
            group.alpha = Mathf.Lerp(fromAlpha, toAlpha, k);
            t += Time.deltaTime;
            yield return null;
        }
        root.anchoredPosition = to;
        root.localScale = Vector3.one * toScale;
        group.alpha = toAlpha;
    }

    private void OnDestroy() { CloseDetails(); if (root != null) Destroy(root.gameObject); }
}

