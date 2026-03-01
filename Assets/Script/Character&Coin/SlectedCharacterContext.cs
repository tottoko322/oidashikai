using UnityEngine;

public class SelectedCharacterContext : MonoBehaviour
{
    public static SelectedCharacterContext I { get; private set; }
    public CharacterData selected;

    private void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }
}
