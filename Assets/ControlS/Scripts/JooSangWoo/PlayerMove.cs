using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] SpriteRenderer spriteRenderer;
    public Vector3 moveDirection;
    public bool isMoving => moveDirection != Vector3.zero;
    void OnMove(InputValue value)
    {
        Vector2 moveInput = value.Get<Vector2>();
        moveDirection = new Vector3(moveInput.x, moveInput.y,0f);
        
    }
    private void Update()
    {
        transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);
        if (moveDirection != Vector3.zero)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = moveDirection.x < 0;
            }
        }
    }
}
