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
        private UIManager uiManager;
        private VirtualDesktop virtualDesktop;

        public bool InputEnabled { get; set; } = true;

        private void Awake() => body = GetComponent<Rigidbody2D>();

        private void Start()
        {
            uiManager = UIManager.Instance;
            virtualDesktop = VirtualDesktop.Instance;
        }

        private void Update()
        {
            if (body == null) return;
            var blocked = (uiManager != null && uiManager.BlocksRoomInput) ||
                          (virtualDesktop != null && virtualDesktop.IsOpen);
            if (!InputEnabled || blocked)
            {
                move = Vector2.zero;
                ClearFocused();
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            var x = 0f;
            var y = 0f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x--;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x++;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) y--;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) y++;
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
            uiManager?.SetPrompt(focused == null ? string.Empty : $"[E] {focused.Prompt}");
        }

        private void ClearFocused()
        {
            if (focused != null) focused.SetFocused(false);
            focused = null;
            uiManager?.SetPrompt(string.Empty);
        }

        private void OnDisable()
        {
            if (body != null) body.linearVelocity = Vector2.zero;
            ClearFocused();
        }
    }
}
