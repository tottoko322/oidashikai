using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterSelectUI : MonoBehaviour
{
    public Image portrait;
    public TMP_Text nameText;

    public void Set(CharacterData data)
    {
        Debug.Log(data);
        portrait.sprite = data.portrait;
        nameText.text = data.displayName;
    }
}
