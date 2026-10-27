using System.Collections;
using UnityEngine;

public class CardDealAnimator : MonoBehaviour
{
    public GameConfig config;
    public DeckManager deck;
    public HandManager hand;
    public HandLayoutController layout;

    [Header("Timing (simple version)")]
    public float interval = 0.08f;
    public bool LastDrawFailed { get; private set; }

    public IEnumerator DealStartHand(int count)
    {
        LastDrawFailed = false;
        for (int i = 0; i < count; i++)
        {
            if (!TryDrawOne()) yield break;
            layout.Rebuild();
            yield return new WaitForSeconds(interval);
        }
    }

    public IEnumerator Draw(int count)
    {
        LastDrawFailed = false;
        for (int i = 0; i < count; i++)
        {
            if (!TryDrawOne()) yield break;
            layout.Rebuild();
            yield return new WaitForSeconds(interval);
        }
    }

    private bool TryDrawOne()
    {
        if (!hand.CanAdd()) return true;

        if (!deck.TryDraw(out var c))
        {
            LastDrawFailed = true;
            BattleStateMachine.I?.NotifyDeckEmpty(false);
            return false;
        }

        var view = hand.AddCard(c);
        AudioManager.I?.PlayDraw();

        // ホバー通知や入力は CardInteraction 側で付ける想定（後で）
        var hover = view.gameObject.GetComponent<CardHoverNotifier>();
        if (hover == null) hover = view.gameObject.AddComponent<CardHoverNotifier>();
        hover.layout = layout;

        return true;
    }
}

