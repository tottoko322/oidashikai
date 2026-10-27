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

    public void Initialize(Transform canvas, TMP_FontAsset font, RectTransform enemyOrigin, CpuBattleConfig config)
    {
        settings = config; origin = enemyOrigin;
        var go = new GameObject("CpuCardReveal", typeof(RectTransform), typeof(CanvasGroup));
        root = go.GetComponent<RectTransform>();
        root.SetParent(canvas, false);
        root.sizeDelta = new Vector2(300f, 420f);
        group = go.GetComponent<CanvasGroup>();
        group.blocksRaycasts = group.interactable = false;
        var imageObject = new GameObject("Artwork", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(root, false);
        var imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero; imageRect.anchorMax = Vector2.one;
        imageRect.sizeDelta = Vector2.zero;
        artwork = imageObject.GetComponent<Image>();
        artwork.preserveAspect = true; artwork.raycastTarget = false;
        var titleObject = new GameObject("Usage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        titleObject.transform.SetParent(root, false);
        var titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.sizeDelta = new Vector2(360f, 76f);
        titleRect.anchoredPosition = new Vector2(0f, 250f);
        titleObject.GetComponent<Image>().color = new Color(0.08f, 0.06f, 0.13f, 0.95f);
        titleObject.GetComponent<Image>().raycastTarget = false;
        var textObject = new GameObject("Text", typeof(RectTransform));
        textObject.transform.SetParent(titleRect, false);
        label = textObject.AddComponent<TextMeshProUGUI>();
        GameFonts.Apply(label);
        label.fontSize = 28f;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.sizeDelta = Vector2.zero;
        root.gameObject.SetActive(false);
    }

    public IEnumerator Show(CardData card, string usage, string detail = "")
    {
        if (root == null || card == null) yield break;
        artwork.sprite = card.artwork;
        label.text = $"CPUの{usage}\n{detail}";
        label.color = usage == "コウゲキ" ? new Color(1f, 0.65f, 0.55f)
            : usage == "ボウギョ" ? new Color(0.55f, 0.85f, 1f) : new Color(0.95f, 0.65f, 1f);
        var parent = (RectTransform)root.parent;
        Vector2 start = origin != null ? (Vector2)parent.InverseTransformPoint(origin.position)
            - parent.rect.center : new Vector2(0f, 420f);
        Vector2 target = settings != null ? settings.revealPosition : new Vector2(-350f, 30f);
        float scale = settings != null ? Mathf.Max(0.1f, settings.revealScale) : 1.4f;
        root.SetAsLastSibling();
        root.gameObject.SetActive(true);
        try
        {
            yield return Animate(start, target, 0.15f, scale, 0f, 1f,
                settings != null ? settings.enterDuration : 0.16f);
            yield return new WaitForSeconds(settings != null ? Mathf.Max(0f, settings.holdDuration) : 0.45f);
            yield return Animate(target, start, scale, 0.1f, 1f, 0f,
                settings != null ? settings.returnDuration : 0.18f);
        }
        finally { if (root != null) root.gameObject.SetActive(false); }
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

    private void OnDestroy() { if (root != null) Destroy(root.gameObject); }
}
