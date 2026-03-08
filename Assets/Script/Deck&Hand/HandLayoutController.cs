using UnityEngine;

public class HandLayoutController : MonoBehaviour
{
    public HandManager handManager;

    [Header("Layout")]
    public float baseSpacing = 140f;
    public float minSpacing = 70f;
    public float baseY = 0f;

    [Header("Scale by count (1..8)")]
    public float[] scaleByCount = new float[9]
    { 0f, 1.00f, 1.00f, 1.00f, 1.00f, 0.92f, 0.86f, 0.80f, 0.75f };

    [Header("Hover")]
    public float hoverGap = 32f;
    public float hoverScale = 1.18f;
    public float hoverRaiseY = 12f;

    private int hoveredIndex = -1;
    private bool hoverEnabled = true;

    public void SetHoverEnabled(bool enabled)
    {
        hoverEnabled = enabled;
        if (!enabled) hoveredIndex = -1;
        Rebuild();
    }

    public void ClearHovered()
    {
        hoveredIndex = -1;
        Rebuild();
    }

    public void NotifyHoverChanged(GameObject card, bool hover)
    {
        if (!hoverEnabled) return;

        var view = card.GetComponent<CardView>();
        if (view == null) return;

        if (hover)
        {
            hoveredIndex = handManager.handViews.IndexOf(view);
        }
        else
        {
            hoveredIndex = -1;
        }

        Rebuild();
    }

    public void Rebuild()
    {
        if (handManager == null) return;

        int n = handManager.handViews.Count;
        if (n == 0) return;

        float scale = (n <= 8) ? scaleByCount[n] : scaleByCount[8];
        float spacing = Mathf.Clamp(baseSpacing - (n - 4) * 12f, minSpacing, baseSpacing);

        float totalWidth = (n - 1) * spacing;
        float startX = -totalWidth * 0.5f;

        for (int i = 0; i < n; i++)
        {
            var view = handManager.handViews[i];
            if (view == null) continue;

            RectTransform rt = view.transform as RectTransform;

            float x = startX + i * spacing;
            float y = baseY;
            float s = scale;

            if (hoverEnabled && hoveredIndex == i)
            {
                y += hoverRaiseY;
                s *= hoverScale;
            }

            rt.anchoredPosition = new Vector2(x, y);
            rt.localScale = Vector3.one * s;

            // 重要：毎回順序を整理
            rt.SetSiblingIndex(i);
        }
    }
}
