using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ControlS
{
    public enum RoomInteractionKind { Computer, Clock, Drawer, Photo, Door }

    public sealed class RoomInteractable : MonoBehaviour
    {
        public static readonly List<RoomInteractable> Active = new();

        [SerializeField] private RoomInteractionKind kind;
        [SerializeField] private string prompt = "Interact";
        [SerializeField, Min(.25f)] private float radius = 1.45f;
        [SerializeField] private List<InteractionRule> rules = new();

        private SpriteRenderer sprite;
        private Color baseColor;
        private Coroutine interactionRoutine;

        public string Prompt => prompt;
        public RoomInteractionKind Kind => kind;
        public bool Available { get; set; } = true;
        public float Radius => radius;
        public IReadOnlyList<InteractionRule> Rules => rules;

        public void Interact() => Interact(null);

        public void Interact(GameObject interactor)
        {
            if (!Available || interactionRoutine != null || rules == null || rules.Count == 0) return;
            interactionRoutine = StartCoroutine(ExecuteRules(interactor));
        }

        private IEnumerator ExecuteRules(GameObject interactor)
        {
            var context = new InteractionContext(interactor, gameObject);
            foreach (var rule in rules)
            {
                if (rule == null || !rule.IsSatisfied(context)) continue;
                yield return rule.Execute(context);
                if (rule.StopAfterExecution) break;
            }
            interactionRoutine = null;
        }

        public void SetFocused(bool focused)
        {
            if (sprite != null) sprite.color = focused ? Color.Lerp(baseColor, Color.white, .28f) : baseColor;
        }

        private void Awake()
        {
            sprite = GetComponent<SpriteRenderer>();
            if (sprite != null) baseColor = sprite.color;
        }

        private void OnEnable()
        {
            if (!Active.Contains(this)) Active.Add(this);
        }

        private void OnDisable()
        {
            Active.Remove(this);
            if (interactionRoutine != null) StopCoroutine(interactionRoutine);
            interactionRoutine = null;
        }
    }
}
