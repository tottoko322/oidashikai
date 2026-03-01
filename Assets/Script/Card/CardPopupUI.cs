using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardPopupUI : MonoBehaviour
{
    public GameObject root;
    public Image art;
    public TMP_Text title;
    public TMP_Text stats;

    private CardView current;

    public bool IsOpen => root != null && root.activeSelf;

    public void Toggle(CardView v)
    {
        if (IsOpen && current == v) Close();
        else Open(v);
    }

    public void Open(CardView v)
    {
        current = v;
        if (root) root.SetActive(true);
        if (art) art.sprite = v.Data.artwork;
        if (title) title.text = v.Data.displayName;
        if (stats) stats.text = $"Cost {v.Data.cost}  ATK {v.Data.attack}  DEF {v.Data.defense}";
    }

    public void Close()
    {
        current = null;
        if (root) root.SetActive(false);
    }
}
