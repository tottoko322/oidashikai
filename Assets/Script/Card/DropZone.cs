using UnityEngine;
using UnityEngine.EventSystems;

public enum DropZoneType { Enemy, Effect }

public class DropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public DropZoneType zoneType;
    public ZoneHighlighter highlighter;

    public void OnDrop(PointerEventData eventData)
    {
        // 実ドロップ処理は DragDropController / CardInteraction 側で拾う（後で）
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!highlighter) return;
        if (zoneType == DropZoneType.Enemy) highlighter.SetEnemy(true);
        if (zoneType == DropZoneType.Effect) highlighter.SetEffect(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!highlighter) return;
        highlighter.Clear();
    }
}
