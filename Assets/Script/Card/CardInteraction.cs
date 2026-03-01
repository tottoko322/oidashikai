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
    public Transform dragTopLayer;         // ドラッグ中に一時的に親をここへ（Canvas配下推奨）
    public float loweredCheckPadding = 0f; // 手札領域判定の余白（必要なら）

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
        // ロック中でもクリック音はOK・処理は進めない方針
        audioManager?.PlayClick();

        if (popupUI == null) return;
        if (dragging) return;

        popupUI.Toggle(view);
        // popup開閉中はホバーを止めたい場合、UI側で制御してOK
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
            // “手札リストから抜く”は HandManager に任せる
            handManager.BeginDrag(view);
        }
        handLayout?.Rebuild();

        if (dragTopLayer != null) transform.SetParent(dragTopLayer, true);
        cg.blocksRaycasts = false; // DropZoneへのRaycastを通す
        cg.alpha = 1f;

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

        // 手札領域の外なら手札全体を下げる（B）
        bool insideHand = IsPointerInsideHandArea(eventData);
        handRootMover?.SetLowered(!insideHand);

        // ハイライト更新
        var zone = DragDropController.I != null ? DragDropController.I.RaycastDropZone(eventData) : null;
        DragDropController.I?.UpdateHighlight(zone);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragging) return;
        dragging = false;

        handRootMover?.SetLowered(false);
        DragDropController.I?.UpdateHighlight(null);

        cg.blocksRaycasts = true;

        var zone = DragDropController.I != null ? DragDropController.I.RaycastDropZone(eventData) : null;

        // Confirm ON の場合：ここでは確定せず“仮置き”扱いにする
        bool useConfirm = confirmController != null && confirmController.UseConfirm;

        if (zone == null)
        {
            ReturnToHand();
            return;
        }

        // ここで「攻撃ゾーン or 効果ゾーンへドロップ」確定（Confirm OFFなら即確定）
        if (!useConfirm)
        {
            CommitDrop(zone);
        }
        else
        {
            // Confirm ON：仮置き → ConfirmController経由で確定する設計
            // 今は最小実装として「仮置き」＝手札に戻さず、表示は残す（親を元に戻して固定）
            PlaceAsPending(zone);
        }
    }

    private void CommitDrop(DropZone zone)
    {
        audioManager?.PlayDrop();

        // 手札から除外状態を確定
        handManager?.ConfirmRemoveDragged();

        // ここで「カードの行動」をBattle側へ通知
        // ResolveManager が本格稼働したらここを呼ぶ：
        // ResolveManager.I.EnqueuePlay(view, zone.zoneType);

        // ワープ消滅（あれば）
        var vanish = GetComponent<CardVanishVfx>();
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
        // “仮置き”は、今は最小として「元の親に戻す＆位置固定」。
        // 本番は zone の中に見た目を置くなどもOK。
        transform.SetParent(originalParent, true);
        transform.SetSiblingIndex(originalSiblingIndex);

        // ひとまず手札には戻さない（手札から抜いたまま）
        handManager?.ConfirmRemoveDragged();
        handLayout?.SetHoverEnabled(true);
        handLayout?.Rebuild();

        // TODO: ConfirmController に pending を登録して、実行ボタンで CommitDrop する形に拡張
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
            handAreaRect, eventData.position, eventData.pressEventCamera);
    }
}
