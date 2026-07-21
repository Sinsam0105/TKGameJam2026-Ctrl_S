using UnityEngine;
using UnityEngine.Events;

public class RoomInteractable : MonoBehaviour
{
    public PuzzleAction puzzleAction;

    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string interactionId;
    [SerializeField] private string prompt = "[E] 조사하기";
    [SerializeField] private bool deactivateAfterSuccess;
    [SerializeField] private UnityEvent onInteracted = new UnityEvent();

    private Transform playerInRange;
    private bool consumed;
    private InteractionManager interactionManager;

    public string InteractionId => interactionId;
    public string Prompt => prompt;
    public bool IsConsumed => consumed;
    public bool CanInteract => isActiveAndEnabled && !consumed && playerInRange != null;
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
        playerInRange = null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other))
            return;

        playerInRange = other.attachedRigidbody != null
            ? other.attachedRigidbody.transform
            : other.transform;
        interactionManager = InteractionManager.Instance;
        interactionManager?.Register(this, playerInRange);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other))
            return;

        playerInRange = null;
        interactionManager?.Unregister(this);
    }

    private bool IsPlayer(Collider2D other)
    {
        if (other.CompareTag(playerTag))
            return true;

        Rigidbody2D attachedRigidbody = other.attachedRigidbody;
        return attachedRigidbody != null && attachedRigidbody.CompareTag(playerTag);
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
            interactionManager?.Unregister(this);
            gameObject.SetActive(false);
        }

        return true;
    }

    private void OnDisable()
    {
        playerInRange = null;
        interactionManager?.Unregister(this);
    }
}
