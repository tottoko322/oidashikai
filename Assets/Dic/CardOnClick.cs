
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class CardOnClick : MonoBehaviour
{
    public GameObject btn;
    public void OnButtonClick()
    {
        if (DicManager.allLoaded)
        {
            CardView card = btn.GetComponent<CardView>();
            DicManager.ShowCards(card.art);
            DicManager.ChangeText(card.atkText);
        }
    }
}
