using UnityEngine;

public class ZoneHighlighter : MonoBehaviour
{
    public GameObject enemyHighlight;
    public GameObject effectHighlight;

    public void SetEnemy(bool on) { if (enemyHighlight) enemyHighlight.SetActive(on); }
    public void SetEffect(bool on) { if (effectHighlight) effectHighlight.SetActive(on); }

    public void Clear()
    {
        SetEnemy(false);
        SetEffect(false);
    }
}
