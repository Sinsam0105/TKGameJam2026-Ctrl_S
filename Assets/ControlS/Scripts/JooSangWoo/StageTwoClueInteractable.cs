using UnityEngine;

public enum StageTwoClueType
{
    Microwave,
    PostIt,
    OutsideClock,
}

[DisallowMultipleComponent]
[RequireComponent(typeof(RoomInteractable), typeof(Collider2D))]
public sealed class StageTwoClueInteractable : MonoBehaviour
{
    [SerializeField] private StageTwoClueType clueType;
    [SerializeField] private RoomInteractable roomInteractable;
    [SerializeField] private Collider2D interactionTrigger;

    public StageTwoClueType ClueType => clueType;
    public RoomInteractable RoomInteractable => roomInteractable;

    private void Awake()
    {
        if (roomInteractable == null)
            roomInteractable = GetComponent<RoomInteractable>();
        if (interactionTrigger == null)
            interactionTrigger = GetComponent<Collider2D>();
    }

    public void SetStageEnabled(bool value)
    {
        if (roomInteractable != null)
        {
            roomInteractable.ResetInteraction();
            roomInteractable.enabled = value;
        }

        if (interactionTrigger != null)
            interactionTrigger.enabled = value;
    }

}
