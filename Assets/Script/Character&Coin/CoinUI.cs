using UnityEngine;
using TMPro;

public class CoinUI : MonoBehaviour
{
    public TMP_Text label;

    public void SetTossing()
    {
        if (label) label.text = "Coin Toss...";
    }

    public void SetResult(CoinFace face, bool playerFirst)
    {
        if (label) label.text = $"{face} / {(playerFirst ? "You First" : "Enemy First")}";
    }
}
