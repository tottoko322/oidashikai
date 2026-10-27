using TMPro;
using UnityEngine;
using UnityEngine.TextCore;

// Serialized text and TMP's default settings reference the same Complete asset.
// Use this for runtime-created labels as well, including their atlas material.
public static class GameFonts
{
    public static TMP_FontAsset Default
    {
        get
        {
            var font = TMP_Settings.instance != null ? TMP_Settings.defaultFontAsset : null;
            EnsureSpace(font);
            return font;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize() => EnsureSpace(Default);

    // The shipped static atlas omits U+0020. An empty glyph lets TMP advance
    // the cursor instead of replacing spaces with end-of-text characters.
    private static void EnsureSpace(TMP_FontAsset font)
    {
        if (font == null || font.HasCharacter(' ')) return;
        uint index = 0;
        foreach (var existing in font.glyphTable)
            if (existing.index >= index) index = existing.index + 1;
        var glyph = new Glyph(index, new GlyphMetrics(0f, 0f, 0f, 0f, font.faceInfo.pointSize * 0.5f),
            new GlyphRect(0, 0, 0, 0), 1f, 0);
        font.glyphTable.Add(glyph);
        font.characterTable.Add(new TMP_Character(32, font, glyph));
        font.ReadFontAssetDefinition();
    }

    public static void Apply(TMP_Text text)
    {
        var font = Default;
        if (text == null || font == null) return;
        text.font = font;
        text.fontSharedMaterial = font.material;
    }
}
