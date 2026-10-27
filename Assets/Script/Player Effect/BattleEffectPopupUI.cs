using System.Collections;
using TMPro;
using UnityEngine;

public class BattleEffectPopupUI : MonoBehaviour
{
    [Header("References")]
    public GameObject popupRoot;
    public TMP_Text messageText;

    [Header("Timing")]
    [Tooltip("通常のポップアップ表示時間")]
    public float defaultDuration = 1.0f;

    private void Awake()
    {
        HideImmediate();
    }

    public IEnumerator Show(string message)
    {
        yield return Show(message, defaultDuration);
    }

    public IEnumerator Show(string message, float duration)
    {
        if (popupRoot == null || messageText == null)
        {
            yield break;
        }

        messageText.text = message;

        popupRoot.SetActive(true);

        if (duration > 0f)
        {
            yield return new WaitForSeconds(duration);
        }

        popupRoot.SetActive(false);
    }

    public void HideImmediate()
    {
        if (popupRoot != null)
        {
            popupRoot.SetActive(false);
        }
    }
}