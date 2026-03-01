using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardSelectUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject root;
    public TMP_Text title;
    public Button confirmButton;
    public Button cancelButton;

    private SelectionRule rule;
    private List<CardView> candidates;
    private readonly List<CardView> selected = new();
    public bool IsFinished { get; private set; }

    public void Open(List<CardView> candidates, SelectionRule rule)
    {
        this.rule = rule;
        this.candidates = candidates;

        selected.Clear();
        IsFinished = false;

        if (root) root.SetActive(true);
        if (title) title.text = rule.isOptional ? "カードを選択（キャンセル可）" : "カードを選択（必須）";

        if (cancelButton) cancelButton.gameObject.SetActive(rule.isOptional);
        UpdateConfirmInteractable();
    }

    public void Close()
    {
        if (root) root.SetActive(false);
    }

    // 実際の候補クリックは、候補側のUIに “選択トグル” を付けてここを呼ぶ想定
    public void ToggleSelect(CardView v)
    {
        if (!candidates.Contains(v)) return;

        if (selected.Contains(v)) selected.Remove(v);
        else
        {
            if (selected.Count >= rule.maxSelect) return;
            selected.Add(v);
        }
        UpdateConfirmInteractable();
    }

    public void OnConfirm()
    {
        if (selected.Count < rule.minSelect) return;
        IsFinished = true;
    }

    public void OnCancel()
    {
        if (!rule.isOptional) return;
        selected.Clear();
        IsFinished = true;
    }

    public List<CardView> GetResult() => new List<CardView>(selected);

    private void UpdateConfirmInteractable()
    {
        if (confirmButton == null) return;
        confirmButton.interactable = selected.Count >= rule.minSelect;
    }
}
