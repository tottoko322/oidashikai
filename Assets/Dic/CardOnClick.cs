
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
            Image img = btn.GetComponent<CardView>().art;
            DicManager.ShowCards(img);
        }
    }
}
