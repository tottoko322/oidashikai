using System.Collections.Generic;
using UnityEngine;

public class HandManager : MonoBehaviour
{
    public int MaxHand => config != null ? config.maxHand : 8;

    [Header("Refs")]
    public GameConfig config;
    public RectTransform handRoot;
    public CardView cardPrefab;

    public readonly List<CardView> handViews = new();

    private CardView draggingView;

    public void ClearHand()
    {
        foreach (var v in handViews) if (v) Destroy(v.gameObject);
        handViews.Clear();
        draggingView = null;
    }

    public bool CanAdd() => handViews.Count < MaxHand;

    public CardView AddCard(CardData data)
    {
        var v = Instantiate(cardPrefab, handRoot);
        v.Bind(data);
        handViews.Add(v);
        return v;
    }

    // === Drag support ===
    public void BeginDrag(CardView v)
    {
        draggingView = v;
        handViews.Remove(v); // 抜く→詰まる
    }

    public void CancelDrag(CardView v)
    {
        draggingView = null;
        if (!handViews.Contains(v)) handViews.Add(v); // 戻す
    }

    public void ConfirmRemoveDragged()
    {
        draggingView = null; // 既に手札から抜けたまま
    }

    public bool Contains(CardView v) => handViews.Contains(v);
}
