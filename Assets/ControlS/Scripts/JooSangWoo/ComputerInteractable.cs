using UnityEngine;

public class ComputerInteractable : MonoBehaviour
{
    public PuzzleAction puzzleAction;

    private void OnMouseDown()
    {
        puzzleAction.OnAction();
    }
}
