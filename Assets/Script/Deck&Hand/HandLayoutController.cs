using UnityEngine;

public class HandLayoutController : MonoBehaviour
{
    [Header("References")]
    public HandManager handManager;
    public RectTransform handRoot;

    [Header("Card Layout")]
    [Tooltip("1～4枚のとき、横幅を何枚分として使うか")]
    public int normalCapacity = 4;

    [Tooltip("カード同士の隙間。0ならほぼ隙間なし")]
    public float gap = 2f;

    [Tooltip("手札領域の左右の余白")]
    public float horizontalPadding = 4f;

    [Tooltip("手札領域の上下の余白")]
    public float verticalPadding = 4f;

    [Header("Hover")]
    public float hoverScale = 1.08f;
    public float hoverRaiseY = 12f;

    private int hoveredIndex = -1;
    private bool hoverEnabled = true;

    private void Awake()
    {
        if (handRoot == null && handManager != null)
        {
            handRoot = handManager.handRoot;
        }
    }

    public void SetHoverEnabled(bool enabled)
    {
        hoverEnabled = enabled;

        if (!enabled)
        {
            hoveredIndex = -1;
        }

        Rebuild();
    }

    public void ClearHovered()
    {
        hoveredIndex = -1;
        Rebuild();
    }

    public void NotifyHoverChanged(GameObject card, bool hover)
    {
        if (!hoverEnabled)
            return;

        if (handManager == null)
            return;

        CardView view = card.GetComponent<CardView>();

        if (view == null)
            return;

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
        if (handManager == null)
            return;

        if (handRoot == null)
            handRoot = handManager.handRoot;

        if (handRoot == null)
            return;

        int count = handManager.handViews.Count;

        if (count == 0)
            return;

        // ============================
        // 手札として使用できる領域
        // ============================

        float areaWidth =
            handRoot.rect.width - horizontalPadding * 2f;

        float areaHeight =
            handRoot.rect.height - verticalPadding * 2f;

        if (areaWidth <= 0f || areaHeight <= 0f)
            return;

        // ============================
        // カード元サイズ取得
        // ============================

        RectTransform sampleCard = null;

        for (int i = 0; i < count; i++)
        {
            if (handManager.handViews[i] == null)
                continue;

            sampleCard =
                handManager.handViews[i].transform as RectTransform;

            if (sampleCard != null)
                break;
        }

        if (sampleCard == null)
            return;

        float originalWidth = sampleCard.rect.width;
        float originalHeight = sampleCard.rect.height;

        if (originalWidth <= 0f || originalHeight <= 0f)
            return;

        // ============================
        // 1～4枚で使うカードサイズ
        // ============================

        // 横幅4枚分でちょうど収まるサイズ
        float targetWidth =
            (areaWidth - gap * (normalCapacity - 1))
            / normalCapacity;

        float widthScale =
            targetWidth / originalWidth;

        // 高さ方向でもはみ出さないようにする
        float heightScale =
            areaHeight / originalHeight;

        // 縦横比維持
        float cardScale =
            Mathf.Min(widthScale, heightScale);

        float cardWidth =
            originalWidth * cardScale;

        float cardHeight =
            originalHeight * cardScale;

        // ============================
        // カード間隔
        // ============================

        float spacing;

        if (count <= normalCapacity)
        {
            // 1～4枚
            // サイズ固定、左詰め
            spacing = cardWidth + gap;
        }
        else
        {
            // 5～8枚
            // カードサイズを維持しつつ、
            // 横方向だけ詰める
            spacing =
                (areaWidth - cardWidth)
                / (count - 1);

            // 極端に重なりすぎるのを防止
            spacing =
                Mathf.Max(spacing, cardWidth * 0.25f);
        }

        // ============================
        // HandRoot基準位置
        // ============================

        float left =
            -handRoot.rect.width * handRoot.pivot.x
            + horizontalPadding;

        float bottom =
            -handRoot.rect.height * handRoot.pivot.y
            + verticalPadding;

        // カード下端をHandRoot下端に合わせる
        float centerY =
            bottom + cardHeight * 0.5f;

        // ============================
        // カード配置
        // ============================

        for (int i = 0; i < count; i++)
        {
            CardView view = handManager.handViews[i];

            if (view == null)
                continue;

            RectTransform rt =
                view.transform as RectTransform;

            if (rt == null)
                continue;

            // 左詰め
            float x =
                left
                + cardWidth * 0.5f
                + i * spacing;

            float y = centerY;

            float scale = cardScale;

            // Hover中だけ少し拡大＋上に出す
            if (hoverEnabled && hoveredIndex == i)
            {
                y += hoverRaiseY;
                scale *= hoverScale;
            }

            rt.anchoredPosition =
                new Vector2(x, y);

            rt.localScale =
                Vector3.one * scale;
        }

        // ============================
        // 描画順
        // ============================

        // 通常時は右のカードほど手前
        for (int i = 0; i < count; i++)
        {
            CardView view = handManager.handViews[i];

            if (view != null)
            {
                view.transform.SetSiblingIndex(i);
            }
        }

        // Hover中のカードだけ最前面
        if (hoverEnabled &&
            hoveredIndex >= 0 &&
            hoveredIndex < handManager.handViews.Count)
        {
            CardView hovered =
                handManager.handViews[hoveredIndex];

            if (hovered != null)
            {
                hovered.transform.SetAsLastSibling();
            }
        }
    }
}