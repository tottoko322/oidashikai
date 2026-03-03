using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DefenseSelectUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject root;
    public TMP_Text message;
    public Button skipButton;

    [Header("Refs")]
    public HandManager hand;
    public HandLayoutController layout;

    private CardView selected;
    private bool decided;

    public void Open()
    {
        selected = null;
        decided = false;
        if (root) root.SetActive(true);
        if (message) message.text = "防御カードを選ぶか、スキップしてください";
    }

    public void Close()
    {
        if (root) root.SetActive(false);
    }

    public void SelectDefense(CardView v)
    {
        selected = v;
        decided = true;
    }

    public void OnSkip()
    {
        selected = null;
        decided = true;
    }

    public IEnumerator WaitDecision()
    {
        Open();
        while (!decided) yield return null;
        Close();
    }

    public CardView GetSelected() => selected;
}
