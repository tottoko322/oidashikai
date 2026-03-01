using UnityEngine;
using UnityEngine.EventSystems;

public class CardHoverNotifier : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public HandLayoutController layout;
    private CardView view;

    private void Awake() => view = GetComponent<CardView>();

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (InputLockManager.I != null && InputLockManager.I.IsLocked) { /* hover OK */ }
        layout.SetHovered(view);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        layout.ClearHovered();
    }
}
