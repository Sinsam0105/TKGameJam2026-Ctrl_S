using UnityEngine;
using UnityEngine.InputSystem;

namespace ControlS
{
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
                focused.Interact(gameObject);
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
}
