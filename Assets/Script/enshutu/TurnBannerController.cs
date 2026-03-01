using System.Collections;
using UnityEngine;
using TMPro;

public class TurnBannerController : MonoBehaviour
{
    public GameObject root;
    public TMP_Text text;
    public float showTime = 0.8f;

    public void Show(string message)
    {
        StopAllCoroutines();
        StartCoroutine(CoShow(message));
    }

    private IEnumerator CoShow(string msg)
    {
        if (root) root.SetActive(true);
        if (text) text.text = msg;
        yield return new WaitForSeconds(showTime);
        if (root) root.SetActive(false);
    }
}
