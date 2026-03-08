using System.Collections.Generic;
using UnityEngine;

public class HandManager : MonoBehaviour
{
    public int MaxHand => config != null ? config.maxHand : 8;

    [Header("Refs")]
    public GameConfig config;
    public RectTransform handRoot;
    public CardView cardPrefab;

    [Header("Auto Wire For Spawned Cards")]
    public HandLayoutController handLayout;
    public HandRootMover handRootMover;
    public RectTransform handAreaRect;
    public CardPopupUI popupUI;
    public ConfirmController confirmController;
    public AudioManager audioManager;
    public Transform dragTopLayer;

    public readonly List<CardView> handViews = new();

    private CardView draggingView;

    public void ClearHand()
    {
        foreach (var v in handViews)
        {
            if (v) Destroy(v.gameObject);
        }

        handViews.Clear();
        draggingView = null;
    }

    public bool CanAdd() => handViews.Count < MaxHand;

    public CardView AddCard(CardData data)
    {
        if (cardPrefab == null)
        {
            Debug.LogError("[HandManager] cardPrefab is not set.");
            return null;
        }

        if (handRoot == null)
        {
            Debug.LogError("[HandManager] handRoot is not set.");
            return null;
        }

        var v = Instantiate(cardPrefab, handRoot);
        v.Bind(data);

        AutoWireSpawnedCard(v);

        handViews.Add(v);
        return v;
    }

    private void AutoWireSpawnedCard(CardView v)
    {
        if (v == null) return;

        // CardInteraction
        var interaction = v.GetComponent<CardInteraction>();
        if (interaction != null)
        {
            interaction.handManager = this;
            interaction.handLayout = handLayout;
            interaction.handRootMover = handRootMover;
            interaction.handAreaRect = handAreaRect;
            interaction.popupUI = popupUI;
            interaction.confirmController = confirmController;
            interaction.audioManager = audioManager;
            interaction.dragTopLayer = dragTopLayer;
        }

        // CardHoverNotifier
        var hover = v.GetComponent<CardHoverNotifier>();
        if (hover == null)
        {
            hover = v.gameObject.AddComponent<CardHoverNotifier>();
        }
        hover.layout = handLayout;
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

    public void RemoveCard(CardView v)
    {
        if (v == null) return;
        handViews.Remove(v);
    }

}
