using System.Collections;
using UnityEngine;

public class CoinTossController : MonoBehaviour
{
    public CoinUI ui;
    public FirstTurnDecider decider;

    [HideInInspector] // Kept for existing serialized scenes; the reel uses spinDuration.
    public float tossDuration = 1.2f;
    [Header("Turn order monitor")]
    public TurnOrderSlotUI slotUI;
    public Sprite monitorArtwork;
    [Min(0.01f)] public float spinDuration = 2.2f;
    [Range(6, 60)] public int minimumRows = 14;
    [Min(0f)]
    public float resultHold = 0.8f;

    public CoinFace ResultFace { get; private set; }
    public bool PlayerFirst { get; private set; }
    public bool IsPlaying { get; private set; }
    public bool ResultReady { get; private set; }

    public IEnumerator Play()
    {
        if (IsPlaying)
        {
            while (IsPlaying) yield return null;
            yield break;
        }
        IsPlaying = true;
        ResultReady = false;
        var inputLock = InputLockManager.I;
        bool wasLocked = inputLock != null && inputLock.IsLocked;
        inputLock?.Lock();
        try
        {
            if (ui != null) ui.gameObject.SetActive(false);
            if (slotUI == null) slotUI = GetComponent<TurnOrderSlotUI>();
            if (slotUI == null) slotUI = gameObject.AddComponent<TurnOrderSlotUI>();
            // One fair draw. Animation never changes or rerolls this result.
            ResultFace = Random.value < 0.5f ? CoinFace.Q : CoinFace.I;
            var character = SelectedCharacterContext.I != null ? SelectedCharacterContext.I.selected : null;
            PlayerFirst = character != null && decider != null
                ? decider.DecidePlayerFirst(character, ResultFace)
                : ResultFace == (character != null ? character.faceForFirst : CoinFace.Q);
            yield return slotUI.Play(PlayerFirst, spinDuration, resultHold, minimumRows,
                monitorArtwork, ui != null && ui.label != null ? ui.label.font : null);
            ResultReady = slotUI.Completed;
        }
        finally
        {
            slotUI?.Hide();
            IsPlaying = false;
            if (!wasLocked && inputLock != null) inputLock.Unlock();
        }
    }
}

