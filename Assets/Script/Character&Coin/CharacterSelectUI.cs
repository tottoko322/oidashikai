using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterSelectUI : MonoBehaviour
{
    public Image portrait;
    public Image shadow;
    public TMP_Text nameText;

    public void Set(CharacterData data)
    {
        if (data == null) return;
        if (portrait != null) portrait.sprite = data.portrait;
        if (shadow != null) shadow.sprite = data.portrait;
        if (nameText != null) nameText.text = data.displayName;
    }
}

