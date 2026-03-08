using UnityEngine;

public class BtnClick : MonoBehaviour
{
    private AudioManager audioManager;

    public void Start()
    {
        audioManager=AudioManager.I;
    }

    public void Click()
    {
        if (audioManager != null)
        {
            audioManager.PlayClick();
        }
    }
}
