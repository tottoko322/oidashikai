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
    private int openedFrame = -1;

    public bool IsOpen => root != null && root.activeSelf;

    public void Toggle(CardView v)
    {
        if (IsOpen && current == v)
        {
            Close();
        }
        else
        {
            Open(v);
        }
    }

    public void Open(CardView v)
    {
        if (v == null || v.Data == null) return;

        current = v;

        if (root)
            root.SetActive(true);

        if (art)
            art.sprite = v.Data.artwork;

        if (title)
            title.text = v.Data.displayName;

        if (stats)
            stats.text = $"Cost {v.Data.cost}  ATK {v.Data.attack}  DEF {v.Data.defense}";

        openedFrame = Time.frameCount;
        AudioManager.I?.PlayButton();
    }

    public void Close(bool playSound = true)
    {
        current = null;

        if (root)
            root.SetActive(false);

        if (playSound)
            AudioManager.I?.PlayButton();
    }

    private void Update()
    {
        if (!IsOpen)
            return;

        if (Time.frameCount == openedFrame)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            Close();
        }
    }
}