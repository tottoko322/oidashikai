using TMPro;
using UnityEngine;

public class ActiveEffectDisplay : MonoBehaviour
{
    [Header("References")]
    public GameObject root;
    public TMP_Text effectText;

    public void SetEffects(string text)
    {
        if (root == null || effectText == null)
            return;

        bool hasEffect = !string.IsNullOrWhiteSpace(text);

        root.SetActive(hasEffect);

        if (hasEffect)
            effectText.text = text;
    }

    public void Clear()
    {
        if (root != null)
            root.SetActive(false);

        if (effectText != null)
            effectText.text = "";
    }
}