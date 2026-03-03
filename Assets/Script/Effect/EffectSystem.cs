using System.Collections.Generic;
using UnityEngine;

public class EffectSystem : MonoBehaviour
{
    [Header("Refs")]
    public DeckManager deck;
    public HandManager hand;
    public HandLayoutController layout;

    public IEnumerator<System.Object> ApplyEffect(EffectData effect)
    {
        if (effect == null || effect.type == EffectType.None) yield break;

        switch (effect.type)
        {
            case EffectType.Draw:
                {
                    int n = Mathf.Max(0, effect.valueA);
                    for (int i = 0; i < n; i++)
                    {
                        // ドロー時0は敗北になるので、上位（Resolve/WinLose）で処理
                        if (!deck.TryDraw(out var c))
                        {
                            yield break;
                        }
                        if (hand.CanAdd())
                        {
                            var v = hand.AddCard(c);
                            var hover = v.GetComponent<CardHoverNotifier>() ?? v.gameObject.AddComponent<CardHoverNotifier>();
                            hover.layout = layout;
                            layout.Rebuild();
                        }
                    }
                    yield break;
                }
            case EffectType.Heal:
                // HP管理導入後に実装
                yield break;

            case EffectType.DealDamage:
                yield break;

            default:
                yield break;
        }
    }
}
