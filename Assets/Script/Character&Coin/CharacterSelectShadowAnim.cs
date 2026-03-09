using UnityEngine;

public class CharacterSelectShadowAnim : MonoBehaviour
{
    private Animator shadow;
    private void Awake()
    {
        shadow=GetComponent<Animator>();
        if(!shadow)Debug.LogWarning("cannot find shadow animation");
    }

    public void StartAnim()
    {
        shadow.Play("shadowClip",0,0f);
    }
}
