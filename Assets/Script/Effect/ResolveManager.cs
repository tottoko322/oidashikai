using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResolveManager : MonoBehaviour
{
    public static ResolveManager I { get; private set; }

    private readonly Queue<IEnumerator> queue = new();
    private bool running;

    private void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
    }

    public void Enqueue(IEnumerator routine)
    {
        queue.Enqueue(routine);
        if (!running) StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        running = true;
        while (queue.Count > 0)
        {
            var r = queue.Dequeue();
            yield return StartCoroutine(r);
        }
        running = false;
    }
}
