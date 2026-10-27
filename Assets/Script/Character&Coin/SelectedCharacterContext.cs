using UnityEngine;

public class SelectedCharacterContext : MonoBehaviour
{
    public static SelectedCharacterContext I { get; private set; }
    public CharacterData selected;
    public CharacterData opponent;

    public static SelectedCharacterContext Ensure()
    {
        if (I == null)
            return new GameObject("SelectedCharacterContext").AddComponent<SelectedCharacterContext>();
        return I;
    }

    public void Select(CharacterData player, CharacterData enemy)
    {
        selected = player;
        opponent = enemy != player ? enemy : null;
    }

    private void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (I == this) I = null;
    }
}

