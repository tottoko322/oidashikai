using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CharacterHitVfx : MonoBehaviour
{
    public RectTransform targetRect; // UI ImageのRectTransform
    public Image targetImage;        // UI Image（赤フラッシュ）

    [Header("Move")]
    public float distanceY = 12f;
    public float duration = 0.22f;

    [Header("Flash")]
    public Color flashColor = Color.red;
    public float flashDuration = 0.18f;

    private Vector2 basePos;
    private Color baseColor;

    private void Awake()
    {
        if (targetRect == null) targetRect = GetComponent<RectTransform>();
        if (targetImage == null) targetImage = GetComponent<Image>();

        if (targetRect != null) basePos = targetRect.anchoredPosition;
        if (targetImage != null) baseColor = targetImage.color;
    }

    public void Play()
    {
        StopAllCoroutines();
        StartCoroutine(CoPlay());
    }

    private IEnumerator CoPlay()
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float wave = Mathf.Sin(k * Mathf.PI * 2f) * (1f - k);
            if (targetRect != null)
                targetRect.anchoredPosition = basePos + new Vector2(0f, wave * distanceY);
            yield return null;
        }

        if (targetRect != null) targetRect.anchoredPosition = basePos;

        // Flash
        if (targetImage != null)
        {
            float tf = 0f;
            while (tf < flashDuration)
            {
                tf += Time.deltaTime;
                float k = Mathf.Clamp01(tf / flashDuration);
                targetImage.color = Color.Lerp(flashColor, baseColor, k);
                yield return null;
            }
            targetImage.color = baseColor;
        }
    }
}
