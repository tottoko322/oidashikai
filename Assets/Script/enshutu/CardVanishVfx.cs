using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class CardVanishVfx : MonoBehaviour
{
    [Header("Warp VFX")]
    public GameObject warpPrefab;
    public float duration = 0.18f;
    public float endScale = 0.0f;

    private CanvasGroup cg;
    private RectTransform rt;

    private void Awake()
    {
        cg = GetComponent<CanvasGroup>();
        rt = (RectTransform)transform;
    }

    public void Play(Action onDone)
    {
        StartCoroutine(CoPlay(onDone));
    }

    private IEnumerator CoPlay(Action onDone)
    {
        GameObject warp = null;

        if (warpPrefab != null)
        {
            warp = Instantiate(warpPrefab, transform.parent);
            var wrt = warp.GetComponent<RectTransform>();
            if (wrt != null)
            {
                wrt.position = rt.position;
                wrt.localScale = Vector3.zero;
            }
        }

        Vector3 startScale = rt.localScale;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);

            // 吸い込み
            rt.localScale = Vector3.Lerp(startScale, Vector3.one * endScale, k);
            cg.alpha = 1f - k;

            // ワープ輪っか（広がって消える）
            if (warp != null)
            {
                var wrt = warp.GetComponent<RectTransform>();
                var wimg = warp.GetComponent<Image>();
                if (wrt != null) wrt.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * 1.35f, k);
                if (wimg != null) wimg.color = new Color(wimg.color.r, wimg.color.g, wimg.color.b, 1f - k);
            }

            yield return null;
        }

        if (warp != null) Destroy(warp);
        onDone?.Invoke();
    }
}
