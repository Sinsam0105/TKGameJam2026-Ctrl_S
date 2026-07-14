using System;
using System.Collections;
using UnityEngine;

namespace ControlS
{
    [Serializable]
    public sealed class ControlSFlagCondition : InteractionCondition
    {
        [SerializeField] private ControlSFlag flag;
        [SerializeField] private bool expectedValue = true;

        public ControlSFlagCondition() { }
        public ControlSFlagCondition(ControlSFlag targetFlag, bool expected)
        {
            flag = targetFlag;
            expectedValue = expected;
        }

        public override bool Evaluate(InteractionContext context) =>
            context.State != null && context.State.GetFlag(flag) == expectedValue;
    }

    [Serializable]
    public sealed class CompletedPuzzleCountCondition : InteractionCondition
    {
        [SerializeField, Min(0)] private int minimumCount = 1;

        public CompletedPuzzleCountCondition() { }
        public CompletedPuzzleCountCondition(int minimum) => minimumCount = Mathf.Max(0, minimum);

        public override bool Evaluate(InteractionContext context) =>
            context.State != null && context.State.CompletedPuzzleCount >= minimumCount;
    }

    [Serializable]
    public sealed class SetControlSFlagAction : InteractionAction
    {
        [SerializeField] private ControlSFlag flag;
        [SerializeField] private bool value = true;

        public SetControlSFlagAction() { }
        public SetControlSFlagAction(ControlSFlag targetFlag, bool targetValue = true)
        {
            flag = targetFlag;
            value = targetValue;
        }

        public override IEnumerator Execute(InteractionContext context)
        {
            context.State?.SetFlag(flag, value);
            yield break;
        }
    }

    [Serializable]
    public sealed class ShowNarrationAction : InteractionAction
    {
        [SerializeField] private NarrationSO narration;
        [SerializeField] private bool waitForNarration;
        [SerializeField, Min(0f)] private float delayAfter;

        public ShowNarrationAction() { }
        public ShowNarrationAction(NarrationSO value, bool wait = false, float after = 0f)
        {
            narration = value;
            waitForNarration = wait;
            delayAfter = Mathf.Max(0f, after);
        }

        public override IEnumerator Execute(InteractionContext context)
        {
            context.Scene?.ShowNarration(narration);
            var wait = (waitForNarration && narration != null ? narration.Duration : 0f) + delayAfter;
            if (wait > 0f) yield return new WaitForSecondsRealtime(wait);
        }
    }

    [Serializable]
    public sealed class ShowInteractionUIAction : InteractionAction
    {
        [SerializeField] private GameObject uiObject;

        public ShowInteractionUIAction() { }
        public ShowInteractionUIAction(GameObject target) => uiObject = target;

        public override IEnumerator Execute(InteractionContext context)
        {
            context.Scene?.ShowUI(uiObject);
            yield break;
        }
    }

    [Serializable]
    public sealed class OpenDesktopAction : InteractionAction
    {
        public override IEnumerator Execute(InteractionContext context)
        {
            context.Scene?.OpenDesktop();
            yield break;
        }
    }

    [Serializable]
    public sealed class CloseDesktopAction : InteractionAction
    {
        public override IEnumerator Execute(InteractionContext context)
        {
            context.Scene?.CloseDesktop();
            yield break;
        }
    }

    [Serializable]
    public sealed class HideCurrentUIAction : InteractionAction
    {
        public override IEnumerator Execute(InteractionContext context)
        {
            context.Scene?.HideCurrentUI();
            yield break;
        }
    }

    [Serializable]
    public sealed class OpenDrawerKeypadAction : InteractionAction
    {
        [SerializeField] private DrawerKeypadContent drawerKeypad;
        [SerializeField] private GameObject uiObject;

        public OpenDrawerKeypadAction() { }
        public OpenDrawerKeypadAction(DrawerKeypadContent keypad, GameObject target)
        {
            drawerKeypad = keypad;
            uiObject = target;
        }

        public override IEnumerator Execute(InteractionContext context)
        {
            drawerKeypad?.Open(uiObject);
            yield break;
        }
    }

    [Serializable]
    public sealed class RequestGlitchAction : InteractionAction
    {
        [SerializeField, Range(0f, 1f)] private float strength = .25f;

        public RequestGlitchAction() { }
        public RequestGlitchAction(float value) => strength = Mathf.Clamp01(value);

        public override IEnumerator Execute(InteractionContext context)
        {
            context.State?.RequestGlitch(strength);
            yield break;
        }
    }

    [Serializable]
    public sealed class SetPlayerInputAction : InteractionAction
    {
        [SerializeField] private bool enabled = true;

        public SetPlayerInputAction() { }
        public SetPlayerInputAction(bool value) => enabled = value;

        public override IEnumerator Execute(InteractionContext context)
        {
            context.Scene?.SetPlayerInput(enabled);
            yield break;
        }
    }

    [Serializable]
    public sealed class RestartSceneAction : InteractionAction
    {
        public override IEnumerator Execute(InteractionContext context)
        {
            context.Scene?.RestartScene();
            yield break;
        }
    }
}
