using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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

    /// <summary>Scene-authored interaction point. The scene controller binds its action at runtime.</summary>
    public sealed class RoomInteractable : MonoBehaviour
    {
        public static readonly List<RoomInteractable> Active = new List<RoomInteractable>();

        [SerializeField] private RoomInteractionKind kind;
        [SerializeField] private string prompt = "조사";
        [SerializeField, Min(.25f)] private float radius = 1.45f;

        private Action action;
        private SpriteRenderer sprite;
        private Color baseColor;

        public string Prompt => prompt;
        public RoomInteractionKind Kind => kind;
        public bool Available { get; set; } = true;
        public float Radius => radius;

        public void Configure(RoomInteractionKind interactionKind, string interactionPrompt, float interactionRadius)
        {
            kind = interactionKind;
            prompt = interactionPrompt;
            radius = interactionRadius;
        }

        public void Bind(Action interact)
        {
            action = interact;
            sprite = GetComponent<SpriteRenderer>();
            if (sprite != null) baseColor = sprite.color;
        }

        public void Interact()
        {
            if (Available) action?.Invoke();
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

        private void OnDisable() => Active.Remove(this);
    }

    public sealed class TopDownPlayer : MonoBehaviour
    {
        [SerializeField, Min(.1f)] private float speed = 3.6f;

        private Rigidbody2D body;
        private Vector2 move;
        private RoomInteractable focused;
        private ControlSSceneController game;

        public bool InputEnabled { get; set; } = true;

        public void Initialize(ControlSSceneController owner)
        {
            game = owner;
            body = GetComponent<Rigidbody2D>();
        }

        private void Awake() => body = GetComponent<Rigidbody2D>();

        private void Update()
        {
            if (game == null || body == null) return;
            if (!InputEnabled || game.BlocksRoomInput)
            {
                move = Vector2.zero;
                ClearFocused();
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            var x = 0f;
            var y = 0f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) y -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) y += 1f;
            move = new Vector2(x, y).normalized;
            UpdateFocus();

            if ((keyboard.eKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame) && focused != null)
                focused.Interact();
        }

        private void FixedUpdate()
        {
            if (body != null) body.linearVelocity = move * speed;
        }

        private void UpdateFocus()
        {
            RoomInteractable nearest = null;
            var nearestDistance = float.MaxValue;
            for (var i = RoomInteractable.Active.Count - 1; i >= 0; i--)
            {
                var candidate = RoomInteractable.Active[i];
                if (candidate == null || !candidate.Available || !candidate.gameObject.activeInHierarchy) continue;
                var distance = Vector2.Distance(transform.position, candidate.transform.position);
                if (distance <= candidate.Radius && distance < nearestDistance)
                {
                    nearest = candidate;
                    nearestDistance = distance;
                }
            }

            if (focused == nearest) return;
            if (focused != null) focused.SetFocused(false);
            focused = nearest;
            if (focused != null) focused.SetFocused(true);
            game.SetPrompt(focused == null ? string.Empty : $"[E] {focused.Prompt}");
        }

        private void ClearFocused()
        {
            if (focused != null) focused.SetFocused(false);
            focused = null;
            game?.SetPrompt(string.Empty);
        }

        private void OnDisable()
        {
            if (body != null) body.linearVelocity = Vector2.zero;
            ClearFocused();
        }
    }

    /// <summary>References the room objects that are saved directly in SampleScene.</summary>
    public sealed class RoomSceneView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer floor;
        [SerializeField] private SpriteRenderer[] lampGlow = Array.Empty<SpriteRenderer>();
        [SerializeField] private GameObject restoredPhoto;
        [SerializeField] private TopDownPlayer player;

        private ControlSSceneController game;
        private ControlSState state;
        private float flickerSeed;

        public TopDownPlayer Player => player;

        public void Configure(SpriteRenderer floorRenderer, SpriteRenderer[] lampRenderers,
            GameObject photoObject, TopDownPlayer playerController)
        {
            floor = floorRenderer;
            lampGlow = lampRenderers;
            restoredPhoto = photoObject;
            player = playerController;
        }

        public void Initialize(ControlSSceneController owner, ControlSState gameState)
        {
            game = owner;
            state = gameState;
            if (player != null) player.Initialize(owner);

            foreach (var interactable in GetComponentsInChildren<RoomInteractable>(true))
            {
                switch (interactable.Kind)
                {
                    case RoomInteractionKind.Computer:
                        interactable.Bind(game.OpenDesktop);
                        break;
                    case RoomInteractionKind.Clock:
                        interactable.Bind(state.InspectClock);
                        break;
                    case RoomInteractionKind.Drawer:
                        interactable.Bind(game.OpenDrawerKeypad);
                        break;
                    case RoomInteractionKind.Photo:
                        interactable.Bind(state.InspectRestoredPhoto);
                        break;
                    case RoomInteractionKind.Door:
                        interactable.Bind(InspectDoor);
                        break;
                }
            }

            state.Changed += Refresh;
            Refresh();
        }

        public void Tick(float time)
        {
            if (state == null) return;
            if (floor != null)
            {
                var progress = state.CompletedPuzzleCount / 4f;
                var pulse = Mathf.Sin(time * (1.1f + progress * .8f)) * .012f;
                floor.color = new Color(.075f + pulse, .09f + pulse * .5f, .105f + pulse * .25f, 1f);
            }

            var intensity = .62f;
            if (state.SaveFileRepaired)
            {
                flickerSeed += Time.unscaledDeltaTime;
                var randomCut = Mathf.PerlinNoise(flickerSeed * 7f, 12.7f) < .18f;
                intensity = randomCut ? .08f : .48f + Mathf.Sin(time * 9f) * .06f;
            }

            foreach (var glow in lampGlow)
            {
                if (glow == null) continue;
                var color = glow.color;
                color.a = intensity;
                glow.color = color;
            }
        }

        private void Refresh()
        {
            if (restoredPhoto != null && state != null) restoredPhoto.SetActive(state.FamilyPhotoRestored);
        }

        private void InspectDoor()
        {
            state.RequestMessage(state.CompletedPuzzleCount < 3
                ? "문고리가 움직이지 않는다. 잠긴 게 아니라, 문 반대편에서 잡고 있는 것 같다."
                : "문 아래 틈으로 모니터와 같은 푸른빛이 새어 나온다.", 4f);
        }
    }
}
