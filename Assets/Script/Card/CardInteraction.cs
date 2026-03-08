using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CardView))]
[RequireComponent(typeof(CanvasGroup))]
public class CardInteraction : MonoBehaviour,
    IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Refs")]
    public HandManager handManager;
    public HandLayoutController handLayout;
    public HandRootMover handRootMover;
    public RectTransform handAreaRect;     // 手札領域（透明Panel）
    public CardPopupUI popupUI;
    public ConfirmController confirmController;
    public AudioManager audioManager;

    [Header("Drag Visual")]
    public Transform dragTopLayer;         // ドラッグ中に一時的に親をここへ
    public float loweredCheckPadding = 0f; // 手札領域判定の余白

    private CardView view;
    private RectTransform rt;
    private CanvasGroup cg;

    private bool dragging;
    private Vector2 originalAnchoredPos;
    private Transform originalParent;
    private int originalSiblingIndex;

    private void Awake()
    {
        view = GetComponent<CardView>();
        rt = (RectTransform)transform;
        cg = GetComponent<CanvasGroup>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        audioManager?.PlayClick();

        if (dragging) return;

        // 防御選択中
        if (BattleStateMachine.I != null && BattleStateMachine.I.IsWaitingForDefense)
        {
            BattleStateMachine.I.SelectDefenseCard(view);
            return;
        }

        // 捨てるカード選択中
        if (BattleStateMachine.I != null && BattleStateMachine.I.IsWaitingForDiscardSelect)
        {
            BattleStateMachine.I.SelectDiscardCard(view);
            return;
        }

        if (popupUI == null) return;
        popupUI.Toggle(view);
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (InputLockManager.I != null && InputLockManager.I.IsLocked)
        {
            // ロック中はドラッグ開始しない（音だけ）
            audioManager?.PlayClick();
            return;
        }

        if (popupUI != null && popupUI.IsOpen) popupUI.Close();

        dragging = true;

        handLayout?.ClearHovered();
        handLayout?.SetHoverEnabled(false);

        originalAnchoredPos = rt.anchoredPosition;
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();

        // 手札から一時除外（詰まってスペース広く）
        if (handManager != null)
        {
            // 手札リストから抜くのは HandManager に任せる
            handManager.BeginDrag(view);
        }
        handLayout?.Rebuild();

        if (dragTopLayer != null) transform.SetParent(dragTopLayer, true);

        // DropZoneへのRaycastを通す
        cg.blocksRaycasts = false;
        cg.alpha = 1f;

        // ここで両方ハイライト開始
        DragDropController.I?.StartHighlight();

        audioManager?.PlayDragStart();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging) return;

        // 追従（Screen→UI座標）
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)rt.parent, eventData.position, eventData.pressEventCamera, out var local))
        {
            rt.anchoredPosition = local;
        }

        // 手札領域の外なら手札全体を下げる
        bool insideHand = IsPointerInsideHandArea(eventData);
        handRootMover?.SetLowered(!insideHand);

        // ハイライト更新
        DropZone zone = DragDropController.I != null ? DragDropController.I.RaycastDropZone(eventData) : null;
        DragDropController.I?.UpdateHighlight(zone);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragging) return;
        dragging = false;

        handRootMover?.SetLowered(false);

        // ハイライト停止
        DragDropController.I?.StopHighlight();

        cg.blocksRaycasts = true;

        DropZone zone = DragDropController.I != null ? DragDropController.I.RaycastDropZone(eventData) : null;

        bool useConfirm = confirmController != null && confirmController.UseConfirm;

        if (zone == null)
        {
            ReturnToHand();
            return;
        }

        if (!useConfirm)
        {
            CommitDrop(zone);
        }
        else
        {
            PlaceAsPending(zone);
        }
    }

    private void CommitDrop(DropZone zone)
    {
        if (zone == null)
        {
            ReturnToHand();
            return;
        }

        bool accepted = BattleStateMachine.I != null && BattleStateMachine.I.TryPlayCard(view, zone.zoneType);
        if (!accepted)
        {
            ReturnToHand();
            return;
        }

        audioManager?.PlayDrop();

        // 手札から除外状態を確定
        handManager?.ConfirmRemoveDragged();

        CardVanishVfx vanish = GetComponent<CardVanishVfx>();
        if (vanish != null)
        {
            vanish.Play(() => Destroy(gameObject));
        }
        else
        {
            Destroy(gameObject);
        }

        handLayout?.SetHoverEnabled(true);
        handLayout?.Rebuild();
    }

    private void PlaceAsPending(DropZone zone)
    {
        // 仮置きは、今は最小として「元の親に戻す＆位置固定」。
        transform.SetParent(originalParent, true);
        transform.SetSiblingIndex(originalSiblingIndex);

        // ひとまず手札には戻さない（手札から抜いたまま）
        handManager?.ConfirmRemoveDragged();
        handLayout?.SetHoverEnabled(true);
        handLayout?.Rebuild();

        Debug.Log($"Pending drop: {zone.zoneType} (Confirm ON)");
    }

    private void ReturnToHand()
    {
        audioManager?.PlayCancel();

        // 元の親へ戻す
        transform.SetParent(originalParent, true);
        transform.SetSiblingIndex(originalSiblingIndex);
        rt.anchoredPosition = originalAnchoredPos;

        // 手札に戻す
        handManager?.CancelDrag(view);
        handLayout?.SetHoverEnabled(true);
        handLayout?.Rebuild();
    }

    private bool IsPointerInsideHandArea(PointerEventData eventData)
    {
        if (handAreaRect == null) return true;

        return RectTransformUtility.RectangleContainsScreenPoint(
            handAreaRect,
            eventData.position,
            eventData.pressEventCamera
        );
    }
}
