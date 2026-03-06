using System.Collections;
using UnityEngine;

public class BattleBootstrap : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SaveManager saveManager;
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private GameConfig gameConfig;

    [SerializeField] private DeckDefinition deckDefinition;
    [SerializeField] private DeckManager deckManager;
    [SerializeField] private HandManager handManager;
    [SerializeField] private CardDealAnimator cardDealAnimator;

    [Header("Boot Options")]
    [SerializeField] private bool dealInitialHandOnStart = true;

    private IEnumerator Start()
    {
        // SaveManager は PlayerPrefs を直接読む設計なので Load() は不要
        if (audioManager != null)
        {
            audioManager.ApplySavedVolume();
        }

        if (gameConfig == null)
        {
            Debug.LogError("[BattleBootstrap] GameConfig is not set.");
            yield break;
        }

        if (deckDefinition == null)
        {
            Debug.LogError("[BattleBootstrap] DeckDefinition is not set.");
            yield break;
        }

        if (deckManager == null)
        {
            Debug.LogError("[BattleBootstrap] DeckManager is not set.");
            yield break;
        }

        if (handManager == null)
        {
            Debug.LogError("[BattleBootstrap] HandManager is not set.");
            yield break;
        }

        if (cardDealAnimator == null)
        {
            Debug.LogError("[BattleBootstrap] CardDealAnimator is not set.");
            yield break;
        }

        bool ok = deckManager.SetupFromModeAndCharacter();
        if (!ok)
        {
            Debug.LogError("[BattleBootstrap] Deck setup failed.");
            if (deckManager.winLose != null)
            {
                deckManager.winLose.Lose();
            }
            yield break;
        }

        if (dealInitialHandOnStart)
        {
            yield return StartCoroutine(cardDealAnimator.DealStartHand(gameConfig.startHandCount));
        }

        Debug.Log("[BattleBootstrap] Initial hand dealt.");
    }
}
