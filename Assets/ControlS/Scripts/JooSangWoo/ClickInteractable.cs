using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// <summary>
/// RoomInteractable과 거의 동일하되, 근접 + [E] 대신 마우스 클릭으로 상호작용한다.
/// 정면샷(FurnitureView) 안에서 사진 조각을 클릭해 줍거나, 클릭형 단서에 쓴다.
///
/// UI 오브젝트(Graphic이 있는 경우)에서는 IPointerClickHandler로 동작하고,
/// 월드의 스프라이트(Collider2D + 카메라의 Physics2DRaycaster)에서도 동일하게 동작한다.
/// 카메라에 Physics2DRaycaster가 없어도 OnMouseUpAsButton으로 한 번 더 받는다.
/// </summary>
[DisallowMultipleComponent]
public class ClickInteractable : MonoBehaviour, IPointerClickHandler
{
    public PuzzleAction puzzleAction;

    [SerializeField] private string interactionId;
    [SerializeField] private string prompt = string.Empty;
    [SerializeField] private bool deactivateAfterSuccess = true;
    [SerializeField] private UnityEvent onInteracted = new UnityEvent();

    private bool consumed;

    public string InteractionId => interactionId;
    public string Prompt => prompt;
    public bool IsConsumed => consumed;
    public bool CanInteract => isActiveAndEnabled && !consumed;
    public UnityEvent OnInteracted => onInteracted;

    public void Configure(string id, string promptText, bool deactivateOnSuccess)
    {
        interactionId = id;
        prompt = promptText;
        deactivateAfterSuccess = deactivateOnSuccess;
        consumed = false;
    }

    public void ResetInteraction()
    {
        consumed = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
            return;

        TryInteract();
    }

    // 카메라에 Physics2DRaycaster가 없는 월드 스프라이트를 위한 보조 경로.
    private void OnMouseUpAsButton()
    {
        TryInteract();
    }

    public bool TryInteract()
    {
        if (!CanInteract || puzzleAction == null)
            return false;

        if (!puzzleAction.OnAction(interactionId))
            return false;

        onInteracted?.Invoke();
        if (deactivateAfterSuccess)
        {
            consumed = true;
            gameObject.SetActive(false);
        }

        return true;
    }

    private void OnDisable()
    {
        // RoomInteractable과 달리 매니저 등록이 없으므로 상태만 유지한다.
    }
}
