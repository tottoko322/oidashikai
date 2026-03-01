using UnityEngine;

public class CharacterSelectManager : MonoBehaviour
{
    public SceneFlowManager flow;
    public CharacterData[] characters;
    public CarouselController carousel;
    public CharacterSelectUI ui;

    private void Start()
    {
        carousel.Init(characters.Length);
        Apply();
    }

    public void OnPrev() { carousel.Prev(); Apply(); }
    public void OnNext() { carousel.Next(); Apply(); }

    public void OnConfirm()
    {
        // 相談スクリプト：SelectedCharacterContext
        SelectedCharacterContext.I.selected = characters[carousel.Index];

        if (ModeContext.I.mode == GameMode.DistributedDeckBattle) flow.GoBattle();
        else flow.GoDeckBuild();
    }

    public void OnBack() => flow.GoTitle();

    private void Apply()
    {
        ui.Set(characters[carousel.Index]);
    }
}
