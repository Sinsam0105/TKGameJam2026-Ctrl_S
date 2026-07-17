using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    [SerializeField] private PlayerMove playerMove;
    [SerializeField] private Animator animator;

    public string MoveAnimationDirection()
    {
        if (playerMove.moveDirection.x > 0)
        {
            return "Right";
        }
        else if (playerMove.moveDirection.x < 0)
        {
            return "Left";
        }
        else if (playerMove.moveDirection.y > 0)
        {
            return "Up";
        }
        else if (playerMove.moveDirection.y < 0)
        {
            return "Down";
        }
        else
        {
            return "Idle";
        }
    }
    public void Update()
    {
        if (animator != null)
        {
            animator.SetBool("isMoving", playerMove.isMoving);
            animator.SetTrigger(MoveAnimationDirection());
        }
    }
}
