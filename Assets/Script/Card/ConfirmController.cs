using UnityEngine;

public class ConfirmController : MonoBehaviour
{
    public GameConfig config;
    public bool UseConfirm => config != null && config.useConfirm;
}
