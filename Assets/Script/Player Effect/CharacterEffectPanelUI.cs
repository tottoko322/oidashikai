using TMPro;
using UnityEngine;

public class CharacterEffectPanelUI : MonoBehaviour
{
    [Header("References")]
    public GameObject panelRoot;
    public TMP_Text titleText;
    public TMP_Text effectListText;

    public bool IsOpen =>
        panelRoot != null && panelRoot.activeSelf;

    private void Awake()
    {
        Close();
    }

    public void Show(string characterName, string effects)
    {
        if (panelRoot == null || effectListText == null)
            return;

        if (titleText != null)
            titleText.text = characterName + "の効果";

        effectListText.text =
            string.IsNullOrWhiteSpace(effects)
                ? "効果はなにもおきていない"
                : effects;

        panelRoot.SetActive(true);
    }

    public void Close()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }
}