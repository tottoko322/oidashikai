using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    private bool waiting;
    private bool skipped;

    private void Awake()
    {
        if (root != null)
            root.SetActive(false);
    }

    public IEnumerator WaitDecision()
    {
        waiting = true;
        skipped = false;
        selected = null;

        if (root != null)
            root.SetActive(true);

        if (message != null)
            message.text = "defense or skip?";

        while (waiting)
            yield return null;

        if (root != null)
            root.SetActive(false);
    }

    public void SelectDefense(CardView view)
    {
        if (!waiting) return;
        if (view == null) return;

        selected = view;
        skipped = false;
        waiting = false;
    }

    public void OnSkip()
    {
        if (!waiting) return;

        selected = null;
        skipped = true;
        waiting = false;
    }

    public CardView GetSelected()
    {
        return selected;
    }

    public bool WasSkipped()
    {
        return skipped;
    }

    public bool IsWaiting()
    {
        return waiting;
    }
}