using System.Collections.Generic;
using UnityEngine;

public class DeckRuntimeContext : MonoBehaviour
{
    public static DeckRuntimeContext I { get; private set; }
    public List<CardData> builtDeck = new();

    private void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }
}
