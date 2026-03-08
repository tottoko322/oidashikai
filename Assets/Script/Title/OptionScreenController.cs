using UnityEngine;

public class OptionScreenController : MonoBehaviour
{
    public GameObject optionScr;
    void Start()
    {
        Check();
    }
    public void ToggleOption()
    {
        Debug.Log("hell yeah!");
        if(!Check())return;
        optionScr.SetActive(!optionScr.activeSelf);
    }
    private bool Check()
    {
        if (!optionScr)
        {
            Debug.LogWarning("Cannot find option screen game object!");
            return false;
        }
        return true;
    }
}
