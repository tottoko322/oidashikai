using UnityEngine;

public class CarouselController : MonoBehaviour
{
    public int Index { get; private set; }
    public int Count { get; private set; }

    public void Init(int count)
    {
        Count = Mathf.Max(1, count);
        Index = Mathf.Clamp(Index, 0, Count - 1);
    }

    public int Prev()
    {
        Index = (Index - 1 + Count) % Count;
        Debug.Log(Index);
        return Index;
    }

    public int Next()
    {
        Index = (Index + 1) % Count;
        Debug.Log(Index);
        return Index;
    }
}
