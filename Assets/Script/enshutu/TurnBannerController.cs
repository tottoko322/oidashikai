using UnityEngine;
using TMPro;

public class TurnBannerController : MonoBehaviour
{
    public GameObject root;
    public TMP_Text text;

    private void Awake()
    {
        if (root) root.SetActive(true);
    }

    public void SetText(string message)
    {
        if (root) root.SetActive(true);
        if (text) text.text = message;
    }
}
