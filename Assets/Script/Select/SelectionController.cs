using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectionController : MonoBehaviour
{
    public CardSelectUI selectUI;

    private List<CardView> result = new();
    private bool done;

    public IEnumerator SelectCards(List<CardView> candidates, SelectionRule rule, System.Action<List<CardView>> onDone)
    {
        done = false;
        result.Clear();

        selectUI.Open(candidates, rule);

        while (!selectUI.IsFinished)
            yield return null;

        result = selectUI.GetResult();
        selectUI.Close();

        onDone?.Invoke(result);
        done = true;
    }
}
