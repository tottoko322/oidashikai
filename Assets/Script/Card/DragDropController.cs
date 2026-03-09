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
        if (I != null)
        {
            Destroy(gameObject);
            return;
        }

        I = this;

        if (eventSystem == null) eventSystem = EventSystem.current;
        if (raycaster == null) raycaster = FindFirstObjectByType<GraphicRaycaster>();
    }

    public DropZone RaycastDropZone(PointerEventData eventData)
    {
        if (eventSystem == null || raycaster == null || eventData == null) return null;

        results.Clear();
        raycaster.Raycast(eventData, results);

        for (int i = 0; i < results.Count; i++)
        {
            GameObject go = results[i].gameObject;
            DropZone zone = go.GetComponentInParent<DropZone>();
            if (zone != null) return zone;
        }

        return null;
    }

    // ドラッグ開始時：両方光らせる
    public void StartHighlight()
    {
        if (highlighter == null) return;

        highlighter.Clear();
        highlighter.SetEnemy(true);
        highlighter.SetEffect(true);
    }

    // ドラッグ終了時：全部消す
    public void StopHighlight()
    {
        if (highlighter == null) return;

        highlighter.Clear();
    }

    // ドラッグ中：現在のゾーンに乗っていなくても両方光らせる
    public void UpdateHighlight(DropZone zone)
    {
        if (highlighter == null) return;

        highlighter.Clear();
        highlighter.SetEnemy(true);
        highlighter.SetEffect(true);
    }
}
