using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 가구 정면샷 창. 방에서 가구를 조사하면 열리고, 이 안에서 숨어 있는 사진 조각을 클릭해 줍는다.
/// 아트가 들어오기 전에는 배경/조각 모두 흰 박스 플레이스홀더로 동작한다.
/// </summary>
[DisallowMultipleComponent]
public sealed class FurnitureViewWindow : PuzzleWindowedUI
{
    [Serializable]
    public sealed class PieceSlot
    {
        [Tooltip("CollectionSystem에 넘길 고유 id. 기존 사진 조각 id와 같아야 한다.")]
        public string CollectionId;
        public Button Button;
    }

    /// <summary>
    /// 조각 줍기가 아닌 클릭 지점. 스캐너, 2단계 단서(전자레인지 디스플레이 등)에 쓴다.
    /// RequiredCondition이 None이 아니면 그 조건이 충족돼야 버튼이 보인다.
    /// </summary>
    [Serializable]
    public sealed class ActionSlot
    {
        public string ActionId;
        public Button Button;
        public GameCondition RequiredCondition = GameCondition.None;
    }

    [Header("Furniture View")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Text titleText;
    [SerializeField] private Text hintText;
    [SerializeField] private List<PieceSlot> pieces = new List<PieceSlot>();
    [SerializeField] private List<ActionSlot> actions = new List<ActionSlot>();

    [Header("Events")]
    [SerializeField] private UnityEvent onPieceFound = new UnityEvent();

    private readonly HashSet<string> foundIds = new HashSet<string>();

    public UnityEvent OnPieceFound => onPieceFound;
    public int RemainingPieceCount => pieces.Count - foundIds.Count;

    /// <summary>액션 버튼이 눌렸을 때 ActionId를 넘긴다. 스캐너/단서 처리는 플로우 컨트롤러가 맡는다.</summary>
    public event Action<string> ActionClicked;

    /// <summary>아트가 준비되면 정면샷 스프라이트를 꽂는다. 비어 있으면 흰 박스가 그대로 남는다.</summary>
    public void SetBackground(Sprite sprite)
    {
        if (backgroundImage == null || sprite == null)
            return;

        backgroundImage.sprite = sprite;
        backgroundImage.color = Color.white;
        backgroundImage.preserveAspect = true;
    }

    public void SetTitle(string value)
    {
        if (titleText != null)
            titleText.text = value;
    }

#if UNITY_EDITOR
    /// <summary>에디터 셋업 스크립트가 직렬화 필드를 채울 때만 쓴다.</summary>
    public void EditorBind(Image background, Text title, Text hint,
        List<PieceSlot> slots, List<ActionSlot> actionSlots = null)
    {
        backgroundImage = background;
        titleText = title;
        hintText = hint;
        pieces = slots ?? new List<PieceSlot>();
        actions = actionSlots ?? new List<ActionSlot>();
    }
#endif

    protected override void Awake()
    {
        base.Awake();

        foreach (PieceSlot slot in pieces)
        {
            if (slot?.Button == null)
                continue;

            PieceSlot captured = slot;
            slot.Button.onClick.AddListener(() => Collect(captured));
        }

        foreach (ActionSlot slot in actions)
        {
            if (slot?.Button == null)
                continue;

            ActionSlot captured = slot;
            slot.Button.onClick.AddListener(() => ActionClicked?.Invoke(captured.ActionId));
        }
    }

    public override void OpenWindow()
    {
        base.OpenWindow();
        RefreshSlots();
    }

    public void ResetView()
    {
        foundIds.Clear();
        RefreshSlots();
        gameObject.SetActive(false);
    }

    private void Collect(PieceSlot slot)
    {
        if (slot == null || string.IsNullOrWhiteSpace(slot.CollectionId))
            return;

        if (!foundIds.Add(slot.CollectionId))
            return;

        // 실패하면(중복/타입 불일치) 찾은 것으로 치지 않는다.
        if (CollectionSystem.Instance == null ||
            !CollectionSystem.Instance.AddCollection(CollectionType.Picture, slot.CollectionId))
        {
            foundIds.Remove(slot.CollectionId);
            return;
        }

        RefreshSlots();
        onPieceFound?.Invoke();
    }

    private void RefreshSlots()
    {
        foreach (PieceSlot slot in pieces)
        {
            if (slot?.Button == null)
                continue;

            slot.Button.gameObject.SetActive(!foundIds.Contains(slot.CollectionId));
        }

        foreach (ActionSlot slot in actions)
        {
            if (slot?.Button == null)
                continue;

            bool unlocked = slot.RequiredCondition == GameCondition.None
                || (GameConditionManager.Instance != null
                    && GameConditionManager.Instance.Condition.HasFlag(slot.RequiredCondition));
            slot.Button.gameObject.SetActive(unlocked);
        }

        if (hintText != null)
        {
            hintText.text = RemainingPieceCount > 0
                ? "수상한 곳을 클릭해 살펴본다."
                : "여기서 더 나올 건 없다.";
        }
    }

    protected override void OnDestroy()
    {
        foreach (PieceSlot slot in pieces)
        {
            if (slot?.Button != null)
                slot.Button.onClick.RemoveAllListeners();
        }

        foreach (ActionSlot slot in actions)
        {
            if (slot?.Button != null)
                slot.Button.onClick.RemoveAllListeners();
        }

        base.OnDestroy();
    }
}
