using UnityEngine;

public class CharacterSelectManager : MonoBehaviour
{
    public SceneFlowManager flow;
    public CharacterData[] characters;
    public CarouselController carousel;
    public CharacterSelectUI ui;

    private void Start()
    {
        if (characters == null || characters.Length == 0 || carousel == null)
        {
            Debug.LogError("[CharacterSelect] Characters or carousel are missing.");
            return;
        }
        carousel.Init(characters.Length);
        var previous = SelectedCharacterContext.I != null ? SelectedCharacterContext.I.selected : null;
        for (int i = 0; i < characters.Length; i++)
            if (previous != null && characters[i] == previous) { carousel.Select(i); break; }
        Apply();
    }

    public void OnPrev() { carousel.Prev(); Apply(); }
    public void OnNext() { carousel.Next(); Apply(); }

    public void OnConfirm()
    {
        if (!ConfirmSelection()) return;
        flow?.GoBattle();
    }

    public bool ConfirmSelection()
    {
        if (carousel == null || characters == null || characters.Length == 0 ||
            carousel.Index >= characters.Length || characters[carousel.Index] == null) return false;
        var player = characters[carousel.Index];
        CharacterData enemy = null;
        foreach (var character in characters)
            if (character != null && character != player) { enemy = character; break; }
        SelectedCharacterContext.Ensure().Select(player, enemy);
        return true;
    }

    public void OnBack() => flow.GoTitle();

    private void Apply()
    {
        if (ui != null && carousel != null && characters != null && characters.Length > 0)
            ui.Set(characters[carousel.Index]);
    }
}

