using UnityEngine;

public class InputLockManager : MonoBehaviour
{
    public static InputLockManager I { get; private set; }
    public bool IsLocked { get; private set; }

    private void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Lock() => IsLocked = true;
    public void Unlock() => IsLocked = false;
}
