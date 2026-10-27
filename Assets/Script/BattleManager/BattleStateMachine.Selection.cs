using UnityEngine;
using UnityEngine.UI;

public partial class BattleStateMachine
{
    public CharacterData PlayerCharacter { get; private set; }
    public CharacterData EnemyCharacter { get; private set; }
    public string PlayerCharacterName => PlayerCharacter != null ? PlayerCharacter.displayName : "自分";
    public string EnemyCharacterName => EnemyCharacter != null ? EnemyCharacter.displayName : "相手";

    public void ApplyCharacterSelection()
    {
        var context = SelectedCharacterContext.I;
        PlayerCharacter = context != null ? context.selected : null;
        EnemyCharacter = context != null ? context.opponent : null;
        ApplyPortrait(playerHitVfx != null ? playerHitVfx.targetImage : null, PlayerCharacter);
        ApplyPortrait(enemyHitVfx != null ? enemyHitVfx.targetImage : null, EnemyCharacter);
    }

    private static void ApplyPortrait(Image image, CharacterData data)
    {
        if (image == null || data == null || data.portrait == null) return;
        image.sprite = data.portrait;
        image.preserveAspect = true;
    }
}
