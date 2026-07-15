using System;
using System.Collections;
using UnityEngine;

namespace ControlS
{
    [Serializable]
    public sealed class ProgressFlagCondition : InteractionCondition
    {
        [SerializeField] private ProgressFlagSO flag;
        [SerializeField] private bool expectedValue = true;
        public override bool Evaluate(InteractionContext context) =>
            context.Progress != null && flag != null && context.Progress.GetFlag(flag) == expectedValue;

        public ProgressFlagCondition() { }
        public ProgressFlagCondition(ProgressFlagSO target, bool expected = true)
        {
            flag = target;
            expectedValue = expected;
        }
    }

    [Serializable]
    public sealed class CompletedPuzzleCountCondition : InteractionCondition
    {
        [SerializeField, Min(0)] private int minimumCount = 1;
        public override bool Evaluate(InteractionContext context) =>
            context.Progress != null && context.Progress.CompletedPuzzleCount >= minimumCount;
    }

    [Serializable]
    public sealed class SetProgressFlagAction : InteractionAction
    {
        [SerializeField] private ProgressFlagSO flag;
        [SerializeField] private bool value = true;
        public override IEnumerator Execute(InteractionContext context)
        {
            context.Progress?.SetFlag(flag, value);
            yield break;
        }

        public SetProgressFlagAction() { }
        public SetProgressFlagAction(ProgressFlagSO target, bool targetValue = true)
        {
            flag = target;
            value = targetValue;
        }
    }

    [Serializable]
    public sealed class ShowNarrationAction : InteractionAction
    {
        [SerializeField] private NarrationSO narration;
        [SerializeField] private bool waitForNarration;
        [SerializeField, Min(0f)] private float delayAfter;
        public override IEnumerator Execute(InteractionContext context)
        {
            context.UI?.ShowNarration(narration);
            var wait = (waitForNarration && narration != null ? narration.Duration : 0f) + delayAfter;
            if (wait > 0f) yield return new WaitForSecondsRealtime(wait);
        }

        public ShowNarrationAction() { }
        public ShowNarrationAction(NarrationSO value, bool wait = false, float after = 0f)
        {
            narration = value;
            waitForNarration = wait;
            delayAfter = Mathf.Max(0f, after);
        }
    }

    [Serializable]
    public sealed class ShowInteractionUIAction : InteractionAction
    {
        [SerializeField] private GameObject uiObject;
        public override IEnumerator Execute(InteractionContext context)
        {
            context.UI?.ShowUI(uiObject);
            yield break;
        }

        public ShowInteractionUIAction() { }
        public ShowInteractionUIAction(GameObject target) => uiObject = target;
    }

    [Serializable]
    public sealed class OpenDesktopAction : InteractionAction
    {
        public override IEnumerator Execute(InteractionContext context)
        {
            context.Desktop?.Open();
            yield break;
        }
    }

    [Serializable]
    public sealed class CloseDesktopAction : InteractionAction
    {
        public override IEnumerator Execute(InteractionContext context)
        {
            context.Desktop?.Close();
            yield break;
        }
    }

    [Serializable]
    public sealed class HideCurrentUIAction : InteractionAction
    {
        public override IEnumerator Execute(InteractionContext context)
        {
            context.UI?.HideCurrentUI();
            yield break;
        }
    }

    [Serializable]
    public sealed class RequestGlitchAction : InteractionAction
    {
        [SerializeField, Range(0f, 1f)] private float strength = .25f;
        public override IEnumerator Execute(InteractionContext context)
        {
            context.UI?.TriggerGlitch(strength);
            yield break;
        }

        public RequestGlitchAction() { }
        public RequestGlitchAction(float value) => strength = Mathf.Clamp01(value);
    }

    [Serializable]
    public sealed class SetPlayerInputAction : InteractionAction
    {
        [SerializeField] private TopDownPlayer player;
        [SerializeField] private bool enabled = true;
        public override IEnumerator Execute(InteractionContext context)
        {
            if (player != null) player.InputEnabled = enabled;
            yield break;
        }
    }

    [Serializable]
    public sealed class RestartSceneAction : InteractionAction
    {
        public override IEnumerator Execute(InteractionContext context)
        {
            GameSessionManager.Instance?.RestartCurrentGame();
            yield break;
        }
    }

}
