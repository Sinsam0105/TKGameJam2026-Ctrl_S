using System.Collections;
using ScriptData;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    public EObject ObjectType { get; private set; } = EObject.Player;   // 여기 있으면 안될 것 같긴 하지만.. PlayerMove로 대체

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] SpriteRenderer spriteRenderer;
    public Animator Animator { get; private set; }

    //[Header("Test: 대사 한 줄 끝날 때마다 흔들림")]
    //[SerializeField] string testScriptName = "Stage3CardConfirmed";
    //[SerializeField] float shakeDuration = 0.3f;
    //[SerializeField] float shakeMagnitude = 0.08f;
    //Coroutine _coShake;
    //Vector3 _shakeOffset;

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
        GetComponent<PlayerInput>().camera = Camera.main;
    }

    #region test: 대사가 한 줄 끝날 때마다 흔들리는지 확인
    //void Start()
    //{
    //    ScriptManager.Instance.BubbleFinished += OnBubbleFinished;
    //    ScriptManager.Instance.Play(testScriptName);
    //}

    //void OnDestroy()
    //{
    //    if (ScriptManager.Instance != null)
    //        ScriptManager.Instance.BubbleFinished -= OnBubbleFinished;
    //}

    //void OnBubbleFinished(string scriptName, int index)
    //{
    //    Shake();
    //}

    //void Shake()
    //{
    //    if (_coShake != null)
    //        StopCoroutine(_coShake);
    //    _coShake = StartCoroutine(CoShake());
    //}

    //IEnumerator CoShake()
    //{
    //    float elapsed = 0f;
    //    while (elapsed < shakeDuration)
    //    {
    //        transform.position -= _shakeOffset;   // 지난 프레임의 흔들림 제거
    //        _shakeOffset = (Vector3)Random.insideUnitCircle * shakeMagnitude;
    //        transform.position += _shakeOffset;   // 새 흔들림 적용

    //        elapsed += Time.deltaTime;
    //        yield return null;
    //    }

    //    transform.position -= _shakeOffset;
    //    _shakeOffset = Vector3.zero;
    //    _coShake = null;
    //}
    #endregion

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