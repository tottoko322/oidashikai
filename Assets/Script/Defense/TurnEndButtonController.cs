using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TurnEndButtonController : MonoBehaviour
{
    public Button button;
    public TMP_Text label;

    private string normalText = "ターン終了";
    private string idleText = "何もしない";

    public void SetIdleOnly(bool idle)
    {
        if (label) label.text = idle ? idleText : normalText;
    }
}
