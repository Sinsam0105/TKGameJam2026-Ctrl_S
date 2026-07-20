using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] SpriteRenderer spriteRenderer;
    public Animator Animator { get; private set; }

    InputAction _moveAction;

    [SerializeField] Vector3 _moveDirection;
    public Vector3 MoveDirection
    {
        get => _moveDirection;
        private set
        {
            if (_moveDirection != value)
            {
                Vector3 lastMoveDir = MoveDirection;
                _moveDirection = value;
                UpdateAnimation(lastMoveDir);
            }
        }
    }
    
    public bool isMoving => MoveDirection != Vector3.zero;

    private void Awake()
    {
        Init();
    }

    void Init()
    {
        //Debug.Log("PlayerMove Init");
        spriteRenderer = GetComponent<SpriteRenderer>();
        Animator = GetComponent<Animator>();
        _moveAction = GetComponent<PlayerInput>().actions["Move"];
    }

    private void Update()
    {
        Vector2 moveInput = _moveAction.ReadValue<Vector2>();

        // 대각선 입력 금지 => x, y 둘 다 눌려있으면 정지
        if (moveInput.x != 0f && moveInput.y != 0f)
            MoveDirection = Vector3.zero;
        else
            MoveDirection = new Vector3(moveInput.x, moveInput.y, 0f);
    }

    private void FixedUpdate()
    {
        transform.Translate(MoveDirection * moveSpeed * Time.deltaTime, Space.World);
    }

    void UpdateAnimation(Vector3 lastMoveDir)
    {
        if (isMoving == false)
        {
            if (lastMoveDir.x > 0 || lastMoveDir.x < 0)
                Animator.Play("IdleSide");
            else if (lastMoveDir.y > 0)
                Animator.Play("IdleUp");
            else if (lastMoveDir.y < 0)
                Animator.Play("IdleDown");
        }
        else if (MoveDirection.x > 0 || MoveDirection.x < 0)
        {
            spriteRenderer.flipX = MoveDirection.x < 0;
            Animator.Play("WalkSide");
        }
        else if (MoveDirection.y > 0)
            Animator.Play("WalkUp");
        else if (MoveDirection.y < 0)
            Animator.Play("WalkDown");
    }
}