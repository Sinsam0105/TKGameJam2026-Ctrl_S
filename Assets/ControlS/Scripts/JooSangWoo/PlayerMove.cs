using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerMove : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] SpriteRenderer spriteRenderer;

    [SerializeField] Vector3 _moveDirection;
    public Vector3 moveDirection    // To 팀장님. public 변수라서 혹시 MoveDirection로 바꿔주실 수 있나요? (파스칼)
    {
        get => _moveDirection;
        private set
        {
            // 애니메이션 중복 실행 방지
            if (_moveDirection != value)
            {
                Vector3 lastMoveDir = _moveDirection;
                _moveDirection = value;
                UpdateAnimation(lastMoveDir);
            }
        }
    }

    public bool isMoving => moveDirection != Vector3.zero;

    public Animator Animator { get; private set; }

    private void Awake()
    {
        Init();
    }

    void Init()
    {
        Debug.Log("PlayerMove Init");
        spriteRenderer = GetComponent<SpriteRenderer>();
        Animator = GetComponent<Animator>();
    }

    // 입력한 이동키(wasd/방향키)가 2개 이상일 때 가만히 있는다
    // => 상/하, 좌/우 끼리는 문제 없어요. 대각선 이동만 처리하시면 될 것 같아요.
    void OnMove(InputValue value)
    {
        Vector2 moveInput = value.Get<Vector2>();
        moveDirection = new Vector3(moveInput.x, moveInput.y, 0f);
    }

    private void Update()
    {
        transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);
        //if (moveDirection != Vector3.zero)
        //{
        //    if (spriteRenderer != null)
        //    {
        //        spriteRenderer.flipX = moveDirection.x < 0;
        //        UpdateAnimation();
        //    }
        //}
    }

    void UpdateAnimation(Vector3 lastMoveDir)
    {
        if (spriteRenderer == null)
        {
            Debug.LogError("spriteRenderer가 없다");
            return;
        }

        if (isMoving == false)
        {
            if (lastMoveDir.x != 0)
                Animator.Play("IdleSide");
            else if (lastMoveDir.y > 0)
                Animator.Play("IdleUp");
            else if (lastMoveDir.y < 0)
                Animator.Play("IdleDown");
        }
        else if (moveDirection.x > 0 || moveDirection.x < 0)
        {
            spriteRenderer.flipX = moveDirection.x < 0;
            Animator.Play("WalkSide");
        }
        else if (moveDirection.y > 0)
            Animator.Play("WalkUp");
        else if (moveDirection.y < 0)
            Animator.Play("WalkDown");
    }
}