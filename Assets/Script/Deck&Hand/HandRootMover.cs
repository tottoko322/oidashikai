using UnityEngine;

public class HandRootMover : MonoBehaviour
{
    public RectTransform handRoot;
    public float loweredOffsetY = 40f;
    private Vector2 normalPos;

    private void Awake()
    {
        normalPos = handRoot.anchoredPosition;
    }

    public void SetLowered(bool lowered)
    {
        handRoot.anchoredPosition = lowered
            ? normalPos + new Vector2(0f, -loweredOffsetY)
            : normalPos;
    }
}
