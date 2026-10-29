using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RulePanelController : MonoBehaviour
{
    public CarouselController carousel;
    public Image descImage;
    public Sprite image1;
    public Sprite image2;
    public Sprite image3;
    public Sprite image4;
    public Sprite[] additionalPages;
    public TMP_Text counter;
    private Sprite[] imageSprites;

    private void Start()
    {
        var pages = new List<Sprite>();
        foreach (var page in new[] { image1, image2, image3, image4 })
            if (page != null) pages.Add(page);
        if (additionalPages != null)
            foreach (var page in additionalPages) if (page != null) pages.Add(page);
        imageSprites = pages.ToArray();
        if (imageSprites.Length == 0 || descImage == null || carousel == null) return;
        BuildPageSurface();
        carousel.Init(imageSprites.Length);
        carousel.Select(0);
        Apply();
    }

    private void BuildPageSurface()
    {
        var canvas = descImage.canvas;
        if (canvas == null) return;
        var buttons = canvas.GetComponentsInChildren<Button>(true);
        descImage.gameObject.SetActive(false);
        if (counter != null) counter.gameObject.SetActive(false);
        var surface = new GameObject("RulePageSurface", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image), typeof(AspectRatioFitter));
        surface.transform.SetParent(canvas.transform, false);
        var fit = surface.GetComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = 1920f / 1200f;
        descImage = surface.GetComponent<Image>();
        descImage.preserveAspect = true;
        descImage.raycastTarget = false;
        foreach (var button in buttons)
        {
            Vector2 min, max;
            switch (button.name)
            {
                case "Left": min = new Vector2(.029f, .396f); max = new Vector2(.096f, .604f); break;
                case "Right": min = new Vector2(.904f, .396f); max = new Vector2(.971f, .604f); break;
                case "Dismiss": min = new Vector2(.825f, .847f); max = new Vector2(.878f, .929f); break;
                default: continue;
            }
            button.transform.SetParent(surface.transform, false);
            var rect = (RectTransform)button.transform;
            rect.localScale = Vector3.one;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            foreach (var graphic in button.GetComponentsInChildren<Graphic>(true))
                graphic.color = Color.clear;
            button.transition = Selectable.Transition.None;
        }
    }

    public void OnPrev() { carousel.Prev(); Apply(); }
    public void OnNext() { carousel.Next(); Apply(); }

    private void Apply()
    {
        if (imageSprites == null || imageSprites.Length == 0) return;
        descImage.sprite = imageSprites[carousel.Index];
        if (counter != null) counter.text = (carousel.Index + 1) + "/" + imageSprites.Length;
    }
}
