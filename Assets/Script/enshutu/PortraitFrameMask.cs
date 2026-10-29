using UnityEngine;
using UnityEngine.UI;

// Stencil clipping keeps either opponent portrait inside the circular frame,
// including during the hit animation. The original Image remains clickable.
public class PortraitFrameMask : MaskableGraphic
{
    public static void Enclose(CharacterHitVfx vfx, RectTransform frame = null)
    {
        if (vfx == null || vfx.targetImage == null) return;
        var image = vfx.targetImage;
        var rect = image.rectTransform;
        if (rect.parent != null && rect.parent.GetComponent<PortraitFrameMask>() != null) return;
        var host = new GameObject("EnemyPortraitFrameMask", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(PortraitFrameMask));
        var clip = (RectTransform)host.transform;
        clip.SetParent(rect.parent, false);
        clip.SetSiblingIndex(rect.GetSiblingIndex());
        clip.anchorMin = rect.anchorMin;
        clip.anchorMax = rect.anchorMax;
        clip.pivot = rect.pivot;
        float height = rect.rect.height * Mathf.Abs(rect.localScale.y);
        clip.sizeDelta = Vector2.one * (height * 4f / 3f);
        clip.anchoredPosition = rect.anchoredPosition + Vector2.up * (height / 6f);
        if (frame != null)
        {
            // Frame_Dead_1's black circle is partially cropped at the top.
            // Match its inner ellipse in the frame's own coordinate system.
            clip.SetParent(frame, false);
            clip.anchorMin = clip.anchorMax = new Vector2(.5f, .5f);
            clip.pivot = new Vector2(.5f, .5f);
            clip.sizeDelta = Vector2.Scale(frame.rect.size, new Vector2(.776f, 1.309f));
            clip.anchoredPosition = new Vector2(0f, frame.rect.height * .334f);
        }
        host.GetComponent<PortraitFrameMask>().raycastTarget = false;
        host.AddComponent<Mask>().showMaskGraphic = false;
        rect.SetParent(clip, true);
        image.maskable = true;
        image.RecalculateMasking();
        vfx.Rebase();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        var rect = rectTransform.rect;
        vh.AddVert(rect.center, color, Vector2.one * .5f);
        const int segments = 96;
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            var point = rect.center + Vector2.Scale(direction, rect.size) * .5f;
            vh.AddVert(point, color, direction * .5f + Vector2.one * .5f);
            if (i > 0) vh.AddTriangle(0, i, i + 1);
        }
    }
}
