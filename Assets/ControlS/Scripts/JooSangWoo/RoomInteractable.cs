using UnityEngine;
using UnityEngine.InputSystem;

public class RoomInteractable : MonoBehaviour
{
    public PuzzleAction puzzleAction;
    [SerializeField] private string playerTag = "Player";
    public InputAction interact => InteractionManager.Instance.Interact;
    private bool onRange = false;
    void OnEnable()
    {
        interact.performed += OnInteract;
    }
    void OnDisable()
    {
        interact.performed -= OnInteract;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
            return;
        onRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
            return;
        onRange = false;
    }
    private bool IsPlayer(Collider other)
    {
        // Collider가 플레이어 자식에 붙어 있는 경우도 처리
        if (other.CompareTag(playerTag))
            return true;

        Rigidbody attachedRigidbody = other.attachedRigidbody;

        return attachedRigidbody != null &&
               attachedRigidbody.CompareTag(playerTag);
    }
    void OnInteract(InputAction.CallbackContext context)
    {
        if (!onRange) return; 
        if (context.performed)
        {
            puzzleAction.OnAction();
        }
    }
}
