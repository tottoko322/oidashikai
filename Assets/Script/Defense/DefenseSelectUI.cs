using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DefenseSelectUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject root;
    public TMP_Text message;
    public Button skipButton;

    private bool waiting;

    private void Awake()
    {
        if (root != null)
            root.SetActive(false);

        if (skipButton != null)
        {
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(OnSkip);
        }
    }

    public void BeginSelection(string msg = "defense or skip")
    {
        waiting = true;

        if (root != null)
            root.SetActive(true);

        if (message != null)
            message.text = msg;
    }

    public void EndSelection()
    {
        waiting = false;

        if (root != null)
            root.SetActive(false);
    }

    public void SelectDefense(CardView view)
    {
        if (!waiting) return;

        BattleStateMachine.I?.TrySelectDefenseByDrop(view);
    }

    public void OnSkip()
    {
        if (!waiting) return;

        Debug.Log("[DefenseUI] Skip button pressed");

        // ここから1回だけ呼ぶ
        BattleStateMachine.I?.SkipDefense();
    }
}