using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragDropController : MonoBehaviour
{
    public static DragDropController I { get; private set; }

    [Header("UI Raycast")]
    public EventSystem eventSystem;
    public GraphicRaycaster raycaster;

    [Header("Refs")]
    public ZoneHighlighter highlighter;

    private readonly List<RaycastResult> results = new();

    private void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;

        if (eventSystem == null) eventSystem = EventSystem.current;
        if (raycaster == null) raycaster = FindFirstObjectByType<GraphicRaycaster>();
    }

    public DropZone RaycastDropZone(PointerEventData eventData)
    {
        if (eventSystem == null || raycaster == null) return null;

        results.Clear();
        raycaster.Raycast(eventData, results);

        for (int i = 0; i < results.Count; i++)
        {
            var go = results[i].gameObject;
            var zone = go.GetComponentInParent<DropZone>();
            if (zone != null) return zone;
        }
        return null;
    }

    public void UpdateHighlight(DropZone zone)
    {
        if (highlighter == null) return;

        if (zone == null)
        {
            highlighter.Clear();
            return;
        }

        if (zone.zoneType == DropZoneType.Enemy) highlighter.SetEnemy(true);
        if (zone.zoneType == DropZoneType.Effect) highlighter.SetEffect(true);
    }
}
