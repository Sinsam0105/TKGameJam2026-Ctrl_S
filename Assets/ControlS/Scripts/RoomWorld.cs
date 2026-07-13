using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ControlS
{
    internal enum RoomInteractionKind
    {
        Computer,
        Clock,
        Drawer,
        Photo,
        Door
    }

    internal sealed class RoomInteractable : MonoBehaviour
    {
        public static readonly List<RoomInteractable> Active = new List<RoomInteractable>();

        public string Prompt { get; private set; }
        public RoomInteractionKind Kind { get; private set; }
        public bool Available { get; set; } = true;
        public float Radius { get; private set; } = 1.45f;

        private Action action;
        private SpriteRenderer sprite;
        private Color baseColor;

        public void Initialize(RoomInteractionKind kind, string prompt, Action interact, float radius = 1.45f)
        {
            Kind = kind;
            Prompt = prompt;
            action = interact;
            Radius = radius;
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

        private void OnEnable()
        {
            if (!Active.Contains(this)) Active.Add(this);
        }

        private void OnDisable() => Active.Remove(this);
    }

    internal sealed class TopDownPlayer : MonoBehaviour
    {
        private Rigidbody2D body;
        private Vector2 move;
        private RoomInteractable focused;
        private ControlSBootstrap game;

        public bool InputEnabled { get; set; } = true;
        public float Speed { get; set; } = 3.6f;

        public void Initialize(ControlSBootstrap owner)
        {
            game = owner;
            body = GetComponent<Rigidbody2D>();
        }

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
            {
                focused.Interact();
            }
        }

        private void FixedUpdate()
        {
            if (body != null) body.linearVelocity = move * Speed;
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

    internal sealed class RoomWorld
    {
        private readonly Transform root;
        private readonly ControlSBootstrap game;
        private readonly ControlSState state;
        private readonly List<SpriteRenderer> lampGlow = new List<SpriteRenderer>();
        private GameObject restoredPhoto;
        private SpriteRenderer floor;
        private float flickerSeed;

        public TopDownPlayer Player { get; private set; }

        public RoomWorld(Transform parent, ControlSBootstrap owner, ControlSState gameState)
        {
            game = owner;
            state = gameState;
            root = new GameObject("Room - Runtime Prototype").transform;
            root.SetParent(parent, false);
            Build();
            state.Changed += Refresh;
            Refresh();
        }

        public void Tick(float time)
        {
            if (floor != null)
            {
                var progress = state.CompletedPuzzleCount / 4f;
                var pulse = Mathf.Sin(time * (1.1f + progress * .8f)) * .012f;
                floor.color = new Color(.075f + pulse, .09f + pulse * .5f, .105f + pulse * .25f, 1f);
            }

            if (lampGlow.Count == 0) return;
            var intensity = .62f;
            if (state.SaveFileRepaired)
            {
                flickerSeed += Time.unscaledDeltaTime;
                var randomCut = Mathf.PerlinNoise(flickerSeed * 7f, 12.7f) < .18f;
                intensity = randomCut ? .08f : .48f + Mathf.Sin(time * 9f) * .06f;
            }
            foreach (var glow in lampGlow)
            {
                if (glow != null)
                {
                    var color = glow.color;
                    color.a = intensity;
                    glow.color = color;
                }
            }
        }

        private void Build()
        {
            floor = Box("Floor", new Vector2(0, 0), new Vector2(15.8f, 8.8f), new Color(.075f, .09f, .105f), -20);
            Box("Rug", new Vector2(0, -.75f), new Vector2(6.4f, 3.8f), new Color(.12f, .135f, .145f), -18);
            AddWall(new Vector2(0, 4.52f), new Vector2(16.4f, .45f));
            AddWall(new Vector2(0, -4.52f), new Vector2(16.4f, .45f));
            AddWall(new Vector2(-8.12f, 0), new Vector2(.45f, 9.4f));
            AddWall(new Vector2(8.12f, 0), new Vector2(.45f, 9.4f));

            // Desk and computer.
            Box("Desk", new Vector2(-4.65f, 2.75f), new Vector2(3.25f, 1.15f), new Color(.19f, .14f, .12f), -2, true);
            Box("Desk shadow", new Vector2(-4.65f, 2.15f), new Vector2(3.15f, .25f), new Color(.025f, .03f, .035f, .7f), -3);
            var computer = Box("Computer", new Vector2(-4.65f, 2.82f), new Vector2(1.65f, .82f), new Color(.08f, .12f, .135f), 1);
            Box("Monitor screen", new Vector2(-4.65f, 2.86f), new Vector2(1.4f, .56f), new Color(.12f, .31f, .31f), 2);
            var pcInteraction = computer.gameObject.AddComponent<RoomInteractable>();
            pcInteraction.Initialize(RoomInteractionKind.Computer, "컴퓨터 사용", game.OpenDesktop, 1.75f);

            // Clock and its red second hand.
            var clock = Box("Stopped Clock", new Vector2(.2f, 3.55f), new Vector2(1.18f, 1.18f), new Color(.62f, .6f, .52f), 0);
            Box("Clock face", new Vector2(.2f, 3.55f), new Vector2(.96f, .96f), new Color(.13f, .145f, .15f), 1);
            var hand = Box("Clock hand", new Vector2(.2f, 3.75f), new Vector2(.055f, .42f), new Color(.76f, .18f, .17f), 2);
            hand.transform.rotation = Quaternion.Euler(0, 0, -35f);
            var clockInteraction = clock.gameObject.AddComponent<RoomInteractable>();
            clockInteraction.Initialize(RoomInteractionKind.Clock, "멈춘 시계 조사", state.InspectClock, 1.55f);

            // Drawer cabinet.
            Box("Cabinet", new Vector2(5.05f, 1.75f), new Vector2(2.35f, 2.2f), new Color(.22f, .165f, .13f), -1, true);
            var drawer = Box("Locked Drawer", new Vector2(5.05f, 1.85f), new Vector2(2.05f, .78f), new Color(.31f, .22f, .16f), 1);
            Box("Drawer handle", new Vector2(5.05f, 1.8f), new Vector2(.55f, .08f), new Color(.68f, .62f, .49f), 2);
            var drawerInteraction = drawer.gameObject.AddComponent<RoomInteractable>();
            drawerInteraction.Initialize(RoomInteractionKind.Drawer, "잠긴 서랍 확인", game.OpenDrawerKeypad, 1.7f);

            // Bed and unsettling shape under it.
            Box("Bed", new Vector2(4.8f, -2.6f), new Vector2(4.55f, 1.9f), new Color(.22f, .24f, .25f), -1, true);
            Box("Blanket", new Vector2(4.75f, -2.5f), new Vector2(4.15f, 1.48f), new Color(.17f, .255f, .27f), 0);
            Box("Under-bed shadow", new Vector2(4.65f, -3.62f), new Vector2(3.3f, .24f), new Color(.005f, .005f, .008f, .95f), 1);

            // Door is deliberately unusable in the slice.
            var door = Box("Door", new Vector2(0, 3.95f), new Vector2(1.85f, 1.0f), new Color(.18f, .15f, .14f), -1);
            var doorInteraction = door.gameObject.AddComponent<RoomInteractable>();
            doorInteraction.Initialize(RoomInteractionKind.Door, "문 열기", () =>
                state.RequestMessage(state.CompletedPuzzleCount < 3
                    ? "문고리가 움직이지 않는다. 잠긴 게 아니라, 문 반대편에서 잡고 있는 것 같다."
                    : "문 아래 틈으로 모니터와 같은 푸른빛이 새어 나온다.", 4f), 1.35f);

            // Lamp and translucent pool of light.
            Box("Lamp pole", new Vector2(-6.55f, -.7f), new Vector2(.12f, 2.2f), new Color(.38f, .36f, .3f), 0);
            Box("Lamp shade", new Vector2(-6.55f, .42f), new Vector2(1.05f, .52f), new Color(.64f, .55f, .35f), 2);
            var glow = Box("Lamp glow", new Vector2(-6.55f, -.35f), new Vector2(2.5f, 2.65f), new Color(.58f, .5f, .28f, .62f), -8);
            lampGlow.Add(glow);

            // Restored family photo starts hidden.
            restoredPhoto = new GameObject("Restored Family Photo");
            restoredPhoto.transform.SetParent(root, false);
            var photoRenderer = restoredPhoto.AddComponent<SpriteRenderer>();
            photoRenderer.sprite = RuntimeUI.WhiteSprite;
            photoRenderer.color = new Color(.41f, .34f, .28f);
            photoRenderer.sortingOrder = 2;
            restoredPhoto.transform.position = new Vector3(6.25f, 3.55f, 0);
            restoredPhoto.transform.localScale = new Vector3(1.18f, 1.0f, 1);
            var inner = Box("Erased figures", new Vector2(6.25f, 3.55f), new Vector2(.9f, .72f), new Color(.12f, .135f, .14f), 3);
            inner.transform.SetParent(restoredPhoto.transform, true);
            var photoInteraction = restoredPhoto.AddComponent<RoomInteractable>();
            photoInteraction.Initialize(RoomInteractionKind.Photo, "복원된 사진 조사", state.InspectRestoredPhoto, 1.55f);

            BuildPlayer();
        }

        private void BuildPlayer()
        {
            var playerRoot = new GameObject("Player");
            playerRoot.transform.SetParent(root, false);
            playerRoot.transform.position = new Vector3(0, -2.1f, 0);

            var shadow = new GameObject("Shadow");
            shadow.transform.SetParent(playerRoot.transform, false);
            shadow.transform.localPosition = new Vector3(0, -.42f, 0);
            var shadowRenderer = shadow.AddComponent<SpriteRenderer>();
            shadowRenderer.sprite = RuntimeUI.WhiteSprite;
            shadowRenderer.color = new Color(0, 0, 0, .45f);
            shadowRenderer.sortingOrder = 4;
            shadow.transform.localScale = new Vector3(.7f, .24f, 1);

            var bodyVisual = new GameObject("Body");
            bodyVisual.transform.SetParent(playerRoot.transform, false);
            var bodyRenderer = bodyVisual.AddComponent<SpriteRenderer>();
            bodyRenderer.sprite = RuntimeUI.WhiteSprite;
            bodyRenderer.color = new Color(.54f, .64f, .61f);
            bodyRenderer.sortingOrder = 6;
            bodyVisual.transform.localScale = new Vector3(.56f, .8f, 1);

            var head = new GameObject("Head");
            head.transform.SetParent(playerRoot.transform, false);
            head.transform.localPosition = new Vector3(0, .48f, 0);
            var headRenderer = head.AddComponent<SpriteRenderer>();
            headRenderer.sprite = RuntimeUI.WhiteSprite;
            headRenderer.color = new Color(.68f, .61f, .54f);
            headRenderer.sortingOrder = 7;
            head.transform.localScale = new Vector3(.48f, .4f, 1);

            var rigidbody = playerRoot.AddComponent<Rigidbody2D>();
            rigidbody.gravityScale = 0;
            rigidbody.freezeRotation = true;
            rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
            var collider = playerRoot.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(.58f, .88f);
            collider.offset = new Vector2(0, -.04f);

            Player = playerRoot.AddComponent<TopDownPlayer>();
            Player.Initialize(game);
        }

        private void Refresh()
        {
            if (restoredPhoto != null) restoredPhoto.SetActive(state.FamilyPhotoRestored);
        }

        private SpriteRenderer Box(string name, Vector2 position, Vector2 size, Color color, int order,
            bool collider = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(position.x, position.y, 0);
            go.transform.localScale = new Vector3(size.x, size.y, 1);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = RuntimeUI.WhiteSprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            if (collider) go.AddComponent<BoxCollider2D>();
            return renderer;
        }

        private void AddWall(Vector2 position, Vector2 size)
        {
            Box("Wall", position, size, new Color(.16f, .17f, .18f), -5, true);
        }
    }
}
