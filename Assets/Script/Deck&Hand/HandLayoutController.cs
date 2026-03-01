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

    [Header("Hover (whole hand pushes)")]
    public float hoverGap = 32f;
    public float hoverScale = 1.18f;
    public float hoverRaiseY = 12f;
    public float[] falloff = new float[] { 0f, 1.0f, 0.6f, 0.35f, 0.2f, 0.15f, 0.12f, 0.10f };

    private int hoveredIndex = -1;
    private bool hoverEnabled = true;

    public void SetHoverEnabled(bool enabled)
    {
        hoverEnabled = enabled;
        if (!enabled) hoveredIndex = -1;
        Rebuild();
    }

    public void SetHovered(CardView v)
    {
        if (!hoverEnabled) return;
        hoveredIndex = handManager.handViews.IndexOf(v);
        Rebuild();
    }

    public void ClearHovered()
    {
        hoveredIndex = -1;
        Rebuild();
    }

    public void Rebuild()
    {
        int n = handManager.handViews.Count;
        if (n <= 0) return;

        float scale = (n <= 8) ? scaleByCount[n] : scaleByCount[8];
        float spacing = Mathf.Clamp(baseSpacing - (n - 4) * 12f, minSpacing, baseSpacing);

        float totalWidth = (n - 1) * spacing;
        float startX = -totalWidth * 0.5f;

        for (int i = 0; i < n; i++)
        {
            float x = startX + i * spacing;
            float y = baseY;
            float s = scale;

            if (hoverEnabled && hoveredIndex >= 0 && hoveredIndex < n)
            {
                if (i == hoveredIndex)
                {
                    y += hoverRaiseY;
                    s *= hoverScale;
                }
                else
                {
                    int d = Mathf.Abs(i - hoveredIndex);
                    float f = (d < falloff.Length) ? falloff[d] : falloff[falloff.Length - 1];
                    float sign = (i < hoveredIndex) ? -1f : 1f;
                    x += sign * hoverGap * f;
                }
            }

            var rt = (RectTransform)handManager.handViews[i].transform;
            rt.anchoredPosition = new Vector2(x, y);
            rt.localScale = Vector3.one * s;

            if (hoverEnabled && hoveredIndex == i) rt.SetAsLastSibling();
        }
    }
}
