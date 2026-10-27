using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TurnOrderSlotUI : MonoBehaviour
{
    [Header("Monitor layout (replace the placeholder artwork later)")]
    public Vector2 monitorSize = new Vector2(860f, 500f);
    public Vector2 screenSize = new Vector2(700f, 280f);
    public Vector2 screenOffset = new Vector2(0f, 30f);
    [Min(0.01f)] public float fadeDuration = 0.15f;

    private GameObject root;
    private CanvasGroup opacity;
    private Image frame, guide, powerLight;
    private GameObject stand;
    private TMP_Text heading, resultText;
    private readonly TMP_Text[] rows = new TMP_Text[5];
    private const float RowHeight = 96f;
    private TMP_FontAsset font;

    public bool IsVisible => root != null && root.activeSelf;
    public bool Completed { get; private set; }
    public bool HasStopped { get; private set; }
    public bool StoppedOnFirst { get; private set; }
    public string CenterLabel { get; private set; }

    public IEnumerator Play(bool playerFirst, float spinDuration, float resultHold,
        int minimumRows, Sprite artwork, TMP_FontAsset preferredFont)
    {
        if (root == null) Build(preferredFont);
        Completed = HasStopped = false;
        CenterLabel = "";
        frame.sprite = artwork;
        frame.color = artwork != null ? Color.white : new Color(0.16f, 0.19f, 0.23f);
        stand.SetActive(artwork == null);
        heading.text = "先攻・後攻を決めています";
        resultText.text = "抽選中…";
        resultText.color = new Color(0.65f, 0.85f, 0.83f);
        guide.color = new Color(0.3f, 0.85f, 0.74f, 0.13f);
        opacity.alpha = 0f;
        root.SetActive(true);
        int target = TurnOrderReel.StopIndex(playerFirst, minimumRows);
        RenderReel(0f);
        try
        {
            yield return Fade(0f, 1f);
            float duration = Mathf.Max(0.01f, spinDuration);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (!isActiveAndEnabled || !IsVisible) yield break;
                RenderReel(TurnOrderReel.Position(elapsed / duration, target));
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            if (!isActiveAndEnabled || !IsVisible) yield break;
            RenderReel(target);
            StoppedOnFirst = TurnOrderReel.IsFirstRow(target);
            CenterLabel = StoppedOnFirst ? "先攻" : "後攻";
            HasStopped = true;
            heading.text = "順番が決まりました";
            resultText.text = playerFirst ? "あなたは先攻です" : "あなたは後攻です";
            resultText.color = playerFirst ? new Color(0.5f, 1f, 0.8f) : new Color(1f, 0.8f, 0.45f);
            float hold = Mathf.Max(0f, resultHold);
            elapsed = 0f;
            while (elapsed < hold)
            {
                if (!isActiveAndEnabled || !IsVisible) yield break;
                // Blink the highlight rather than hiding the result text.
                float pulse = 0.2f + 0.18f * (0.5f + 0.5f * Mathf.Sin(elapsed * 12f));
                guide.color = new Color(resultText.color.r, resultText.color.g, resultText.color.b, pulse);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            yield return Fade(1f, 0f);
            if (isActiveAndEnabled && IsVisible) Completed = true;
        }
        finally { Hide(); }
    }

    private void RenderReel(float position)
    {
        int center = Mathf.FloorToInt(position);
        for (int i = 0; i < rows.Length; i++)
        {
            int index = center + i - 2;
            float y = (index - position) * RowHeight;
            rows[i].rectTransform.anchoredPosition = new Vector2(0f, y);
            rows[i].text = TurnOrderReel.IsFirstRow(index) ? "先攻" : "後攻";
            Color color = TurnOrderReel.IsFirstRow(index)
                ? new Color(0.5f, 1f, 0.8f) : new Color(1f, 0.8f, 0.45f);
            color.a = Mathf.Lerp(1f, 0.2f, Mathf.Clamp01(Mathf.Abs(y) / (RowHeight * 1.5f)));
            rows[i].color = color;
        }
    }

    private IEnumerator Fade(float from, float to)
    {
        float duration = Mathf.Max(0.01f, fadeDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (!isActiveAndEnabled || !IsVisible) yield break;
            opacity.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        if (opacity != null) opacity.alpha = to;
    }

    private void Build(TMP_FontAsset preferredFont)
    {
        font = preferredFont;
        foreach (var text in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include))
        {
            if (text.gameObject.scene != gameObject.scene || text.font == null) continue;
            if (font == null) font = text.font;
            if (text.font.name.Contains("Complete")) { font = text.font; break; }
        }
        if (font == null && TMP_Settings.instance != null) font = TMP_Settings.defaultFontAsset;
        root = new GameObject("TurnOrderMonitor", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, gameObject.scene);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 29000;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        opacity = root.GetComponent<CanvasGroup>();
        var backdrop = Box("Backdrop", root.transform, Vector2.zero, Vector2.zero, new Color(0.01f, 0.02f, 0.04f, 0.86f));
        backdrop.rectTransform.anchorMin = Vector2.zero;
        backdrop.rectTransform.anchorMax = Vector2.one;
        backdrop.rectTransform.sizeDelta = Vector2.zero;
        backdrop.raycastTarget = true;

        frame = Box("MonitorFrame", root.transform, new Vector2(0f, 24f), monitorSize, new Color(0.16f, 0.19f, 0.23f));
        Box("ScreenRim", frame.transform, screenOffset, screenSize + new Vector2(20f, 20f), new Color(0.04f, 0.05f, 0.07f));
        var screen = Box("Screen", frame.transform, screenOffset, screenSize, new Color(0.015f, 0.055f, 0.06f));
        screen.gameObject.AddComponent<RectMask2D>();
        guide = Box("CenterGuide", screen.transform, Vector2.zero, new Vector2(screenSize.x, RowHeight), new Color(0.3f, 0.85f, 0.74f, 0.13f));
        Box("TopGuide", screen.transform, new Vector2(0f, RowHeight * 0.5f), new Vector2(screenSize.x, 2f), new Color(0.4f, 1f, 0.8f, 0.55f));
        Box("BottomGuide", screen.transform, new Vector2(0f, -RowHeight * 0.5f), new Vector2(screenSize.x, 2f), new Color(0.4f, 1f, 0.8f, 0.55f));
        for (int i = 0; i < rows.Length; i++)
            rows[i] = Label("ReelRow" + i, screen.transform, "", Vector2.zero, new Vector2(screenSize.x, 88f), 64f);
        for (int i = 0; i < 18; i++)
            Box("Scanline" + i, screen.transform, new Vector2(0f, -screenSize.y * 0.5f + i * screenSize.y / 17f),
                new Vector2(screenSize.x, 1f), new Color(0.5f, 1f, 0.9f, 0.035f));
        Label("LeftMarker", frame.transform, ">", screenOffset + new Vector2(-screenSize.x * 0.5f - 28f, 0f), new Vector2(40f, 70f), 40f);
        Label("RightMarker", frame.transform, "<", screenOffset + new Vector2(screenSize.x * 0.5f + 28f, 0f), new Vector2(40f, 70f), 40f);
        heading = Label("Heading", frame.transform, "", new Vector2(0f, 210f), new Vector2(780f, 48f), 32f);
        resultText = Label("Result", frame.transform, "", new Vector2(0f, -174f), new Vector2(780f, 58f), 38f);
        Label("Brand", frame.transform, "TURN ORDER", new Vector2(-280f, -220f), new Vector2(230f, 24f), 16f);
        powerLight = Box("PowerLight", frame.transform, new Vector2(370f, -218f), new Vector2(12f, 12f), new Color(0.4f, 1f, 0.65f));
        stand = new GameObject("PlaceholderStand", typeof(RectTransform));
        stand.transform.SetParent(frame.transform, false);
        Box("Neck", stand.transform, new Vector2(0f, -275f), new Vector2(90f, 55f), new Color(0.11f, 0.13f, 0.16f));
        Box("Foot", stand.transform, new Vector2(0f, -310f), new Vector2(280f, 18f), new Color(0.19f, 0.22f, 0.26f));
        root.SetActive(false);
    }

    private static Image Box(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.rectTransform.anchoredPosition = position;
        image.rectTransform.sizeDelta = size;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private TMP_Text Label(string name, Transform parent, string text, Vector2 position, Vector2 size, float fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var label = go.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.7f, 0.95f, 0.9f);
        label.raycastTarget = false;
        label.rectTransform.anchoredPosition = position;
        label.rectTransform.sizeDelta = size;
        return label;
    }

    public void Hide() { if (root != null) root.SetActive(false); }
    private void OnDisable() => Hide();
    private void OnDestroy() { if (root != null) Destroy(root); }
}
