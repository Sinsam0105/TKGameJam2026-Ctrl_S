using Sinsam.SingletonSystem;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionManager : MonoSingleton<InteractionManager>
{
    [SerializeField] private InputActionAsset inputActions;
    public InputAction Interact => inputActions.FindAction("Interact");
}
