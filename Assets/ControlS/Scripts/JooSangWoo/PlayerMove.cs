using ScriptData;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(PlayerInput), typeof(Animator))]
public class PlayerMove : MonoBehaviour
{
    public EObject ObjectType { get; private set; } = EObject.Player;

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Vector3 _moveDirection;

    private InputAction moveAction;
    private Rigidbody2D body;

    public Animator Animator { get; private set; }
    public Vector3 MoveDirection
    {
        get => _moveDirection;
        private set
        {
            if (_moveDirection == value)
                return;

            Vector3 previousDirection = _moveDirection;
            _moveDirection = value;
            UpdateAnimation(previousDirection);
        }
    }

    public bool isMoving => MoveDirection.sqrMagnitude > 0.001f;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        Animator = GetComponent<Animator>();
        body = GetComponent<Rigidbody2D>();

        PlayerInput playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Move"];
        playerInput.camera = Camera.main;
    }

    private void Update()
    {
        Vector2 input = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        if (GameStateManager.Instance != null && GameStateManager.Instance.State != GameState.Room)
            input = Vector2.zero;

        input = Vector2.ClampMagnitude(input, 1f);
        MoveDirection = new Vector3(input.x, input.y, 0f);
    }

    private void FixedUpdate()
    {
        if (body == null)
            return;

        Vector2 next = body.position
            + (Vector2)MoveDirection * (moveSpeed * Time.fixedDeltaTime);
        body.MovePosition(next);
    }

    private void UpdateAnimation(Vector3 previousDirection)
    {
        if (Animator == null || spriteRenderer == null)
            return;

        if (!isMoving)
        {
            if (Mathf.Abs(previousDirection.x) > 0f)
                Animator.Play("IdleSide");
            else if (previousDirection.y > 0f)
            {
                spriteRenderer.flipX = false;
                Animator.Play("IdleUp");
            }
            else if (previousDirection.y < 0f)
            {
                spriteRenderer.flipX = false;
                Animator.Play("IdleDown");
            }
            return;
        }

        if (Mathf.Abs(MoveDirection.x) > Mathf.Abs(MoveDirection.y))
        {
            spriteRenderer.flipX = MoveDirection.x < 0f;
            Animator.Play("WalkSide");
        }
        else if (MoveDirection.y > 0f)
        {
            spriteRenderer.flipX = false;
            Animator.Play("WalkUp");
        }
        else
        {
            spriteRenderer.flipX = false;
            Animator.Play("WalkDown");
        }
    }
}
