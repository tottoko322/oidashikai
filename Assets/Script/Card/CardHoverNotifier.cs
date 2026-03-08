using UnityEngine;
using UnityEngine.EventSystems;

public class CardHoverNotifier : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] public HandLayoutController layout;

    private void Awake()
    {
        if (layout == null)
        {
            layout = FindFirstObjectByType<HandLayoutController>();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (layout == null) return;
        layout.NotifyHoverChanged(gameObject, true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (layout == null) return;
        layout.NotifyHoverChanged(gameObject, false);
    }
}
