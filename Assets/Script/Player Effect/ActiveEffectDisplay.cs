using TMPro;
using UnityEngine;

public class ActiveEffectDisplay : MonoBehaviour
{
    [Header("References")]
    public GameObject root;
    public TMP_Text effectText;

    private void Awake()
    {
        // The compact status is informational; clicks must reach the portrait.
        foreach (var graphic in GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
            graphic.raycastTarget = false;
        Clear();
    }

    public void SetEffects(string text)
    {
        if (root == null || effectText == null)
            return;

        bool hasEffect = !string.IsNullOrWhiteSpace(text);

        root.SetActive(hasEffect);

        effectText.text = hasEffect ? text : "";
    }

    public void Clear()
    {
        if (root != null)
            root.SetActive(false);

        if (effectText != null)
            effectText.text = "";
    }
}
