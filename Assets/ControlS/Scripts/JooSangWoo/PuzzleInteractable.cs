using UnityEngine;
public struct AnswerInput
{
    public string input;
    public bool isCorrect;
}
public abstract class PuzzleInteractable : MonoBehaviour
{
    public PuzzleAction action;
    public abstract bool OnAnswerInput(AnswerInput answer);
}
