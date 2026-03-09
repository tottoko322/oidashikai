using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TurnEndButtonController : MonoBehaviour
{
    public Button button;
    public TMP_Text label;

    private string normalText = "turn end";
    private string idleText = "Skip";

    public void SetIdleOnly(bool idle)
    {
        if (label) label.text = idle ? idleText : normalText;
    }
}
