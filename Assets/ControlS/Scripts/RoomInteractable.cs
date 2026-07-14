using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ControlS
{
    public enum RoomInteractionKind
    {
        Computer,
        Clock,
        Drawer,
        Photo,
        Door
    }

    [Serializable]
    public sealed class NarrationSOEvent : UnityEvent<NarrationSO> { }

    [Serializable]
    public sealed class GameObjectEvent : UnityEvent<GameObject> { }

    /// <summary>씬에 Payload와 Persistent UnityEvent를 저장하는 상호작용 지점입니다.</summary>
    public sealed class RoomInteractable : MonoBehaviour
    {
        public static readonly List<RoomInteractable> Active = new List<RoomInteractable>();

        [SerializeField] private RoomInteractionKind kind;
        [SerializeField] private string prompt = "조사";
        [SerializeField, Min(.25f)] private float radius = 1.45f;

        [Header("Conditional Rules (first match wins)")]
        [SerializeField] private List<InteractionRule> rules = new List<InteractionRule>();

        [Header("Interaction Payload")]
        [SerializeField, Tooltip("상호작용할 때 NarrationSOEvent로 전달할 내레이션입니다.")]
        private NarrationSO interactionNarration;
        [SerializeField, Tooltip("상호작용할 때 GameObjectEvent로 전달할 UI 오브젝트입니다.")]
        private GameObject interactionUI;

        [Header("Interaction Events")]
        [SerializeField, Tooltip("상호작용할 때 인자 없이 호출됩니다.")]
        private UnityEvent onInteract = new UnityEvent();
        [SerializeField, Tooltip("Interaction Narration 필드를 인자로 전달합니다.")]
        private NarrationSOEvent onInteractNarration = new NarrationSOEvent();
        [SerializeField, Tooltip("Interaction UI 필드를 인자로 전달합니다.")]
        private GameObjectEvent onInteractGameObject = new GameObjectEvent();

        private SpriteRenderer sprite;
        private Color baseColor;
        private ControlSSceneController scene;
        private Coroutine interactionRoutine;

        public string Prompt => prompt;
        public RoomInteractionKind Kind => kind;
        public bool Available { get; set; } = true;
        public float Radius => radius;
        public NarrationSO InteractionNarration => interactionNarration;
        public GameObject InteractionUI => interactionUI;
        public UnityEvent OnInteract => onInteract;
        public NarrationSOEvent OnInteractNarration => onInteractNarration;
        public GameObjectEvent OnInteractGameObject => onInteractGameObject;
        public IReadOnlyList<InteractionRule> Rules => rules;

        public void Configure(RoomInteractionKind interactionKind, string interactionPrompt, float interactionRadius)
        {
            kind = interactionKind;
            prompt = interactionPrompt;
            radius = interactionRadius;
        }

        public void ConfigurePayload(NarrationSO narration, GameObject uiObject)
        {
            interactionNarration = narration;
            interactionUI = uiObject;
        }

        public void ConfigureRules(IEnumerable<InteractionRule> interactionRules)
        {
            rules = interactionRules != null
                ? new List<InteractionRule>(interactionRules)
                : new List<InteractionRule>();
        }

        public void Initialize(ControlSSceneController owner) => scene = owner;

        public void Interact()
        {
            if (!Available) return;
            Interact(null);
        }

        public void Interact(GameObject interactor)
        {
            if (!Available || interactionRoutine != null) return;
            if (rules != null && rules.Count > 0)
            {
                interactionRoutine = StartCoroutine(ExecuteRules(interactor));
                return;
            }

            InvokeLegacyEvents();
        }

        private IEnumerator ExecuteRules(GameObject interactor)
        {
            if (scene == null) scene = GetComponentInParent<ControlSSceneController>();
            if (scene == null) scene = ControlSSceneController.Current;
            var context = new InteractionContext(interactor, gameObject, scene);
            foreach (var rule in rules)
            {
                if (rule == null || !rule.IsSatisfied(context)) continue;
                yield return rule.Execute(context);
                if (rule.StopAfterExecution) break;
            }
            interactionRoutine = null;
        }

        private void InvokeLegacyEvents()
        {
            onInteract?.Invoke();
            if (interactionNarration != null) onInteractNarration?.Invoke(interactionNarration);
            if (interactionUI != null) onInteractGameObject?.Invoke(interactionUI);
        }

        public void SetFocused(bool focused)
        {
            if (sprite == null) return;
            sprite.color = focused ? Color.Lerp(baseColor, Color.white, .28f) : baseColor;
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
