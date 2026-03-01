using UnityEngine;

public class FirstTurnDecider : MonoBehaviour
{
    public bool DecidePlayerFirst(CharacterData character, CoinFace faceResult)
    {
        return faceResult == character.faceForFirst;
    }
}
