using Sinsam.SingletonSystem;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DefaultExecutionOrder(-250)]
public class InteractionManager : MonoSingleton<InteractionManager>
{
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private Text promptText;

    private readonly Dictionary<RoomInteractable, Transform> candidates = new();
    private InputAction interactAction;

    public InputAction Interact => interactAction;

    protected override bool ShouldPersist() => false;

    protected override void Awake()
    {
        base.Awake();
        if (inputActions == null)
        {
            Debug.LogError("InteractionManager: InputActionAsset is not assigned.", this);
            return;
        }

        interactAction = inputActions.FindAction("Interact", false);
        if (interactAction == null)
        {
            Debug.LogError("InteractionManager: Interact action was not found.", this);
            return;
        }

        interactAction.performed += OnInteractPerformed;
        interactAction.Enable();
    }

    public void SetPromptText(Text value)
    {
        promptText = value;
        RefreshPrompt();
    }

    public void Register(RoomInteractable interactable, Transform player)
    {
        if (interactable == null || player == null)
            return;

        candidates[interactable] = player;
        RefreshPrompt();
    }

    public void Unregister(RoomInteractable interactable)
    {
        if (interactable == null)
            return;

        candidates.Remove(interactable);
        RefreshPrompt();
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        if (GameStateManager.Instance != null && GameStateManager.Instance.State != GameState.Room)
            return;

        RoomInteractable target = GetNearest();
        if (target != null)
            target.TryInteract();

        RefreshPrompt();
    }

    private RoomInteractable GetNearest()
    {
        RoomInteractable nearest = null;
        float nearestDistance = float.PositiveInfinity;
        List<RoomInteractable> invalid = null;

        foreach (KeyValuePair<RoomInteractable, Transform> pair in candidates)
        {
            RoomInteractable interactable = pair.Key;
            Transform player = pair.Value;
            if (interactable == null || player == null || !interactable.CanInteract)
            {
                invalid ??= new List<RoomInteractable>();
                invalid.Add(interactable);
                continue;
            }

            float distance = (interactable.transform.position - player.position).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = interactable;
            }
        }

        if (invalid != null)
        {
            foreach (RoomInteractable item in invalid)
                candidates.Remove(item);
        }

        return nearest;
    }

    private void RefreshPrompt()
    {
        if (promptText == null)
            return;

        RoomInteractable target = GetNearest();
        promptText.text = target != null ? target.Prompt : string.Empty;
        promptText.gameObject.SetActive(target != null);
    }

    protected override void OnDestroy()
    {
        if (interactAction != null)
        {
            interactAction.performed -= OnInteractPerformed;
            interactAction.Disable();
        }

        base.OnDestroy();
    }
}
