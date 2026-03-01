using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardView : MonoBehaviour
{
    public Image art;
    public TMP_Text nameText;
    public TMP_Text costText;
    public TMP_Text atkText;
    public TMP_Text defText;

    public CardData Data { get; private set; }

    public void Bind(CardData data)
    {
        Data = data;
        if (art) art.sprite = data.artwork;
        if (nameText) nameText.text = data.displayName;
        if (costText) costText.text = data.cost.ToString();
        if (atkText) atkText.text = data.attack.ToString();
        if (defText) defText.text = data.defense.ToString();
    }
}
