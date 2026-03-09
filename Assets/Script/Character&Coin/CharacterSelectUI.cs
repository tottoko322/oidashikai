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
        portrait.sprite = data.portrait;
        shadow.sprite=data.portrait;
        nameText.text = data.displayName;
    }
}
