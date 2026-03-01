using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    // 最小：後で本格化するための枠
    // 防御：最大防御、同値なら攻撃低い方
    public CardView ChooseDefense(List<CardView> enemyHand)
    {
        CardView best = null;
        foreach (var v in enemyHand)
        {
            if (best == null) { best = v; continue; }
            int d0 = best.Data.defense;
            int d1 = v.Data.defense;
            if (d1 > d0) best = v;
            else if (d1 == d0 && v.Data.attack < best.Data.attack) best = v;
        }
        return best;
    }

    // 攻撃/効果：とりあえず攻撃最大を選ぶ（後で強化）
    public CardView ChooseAction(List<CardView> enemyHand)
    {
        CardView best = null;
        foreach (var v in enemyHand)
        {
            if (best == null) { best = v; continue; }
            if (v.Data.attack > best.Data.attack) best = v;
        }
        return best;
    }
}
