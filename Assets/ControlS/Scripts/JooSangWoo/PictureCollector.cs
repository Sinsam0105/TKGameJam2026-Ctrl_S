using UnityEngine;
using System.Collections.Generic;

public class PictureCollector : MonoBehaviour
{
    [SerializeField]
    private List<Transform> Pictures = new List<Transform>();
    [SerializeField]
    private List<Transform> Answers = new List<Transform>();
    [SerializeField]
    private float detectionDistance = 5f;
    [SerializeField]
    private GameCondition completionCondition = GameCondition.ImagePuzzleCompleted;

    private bool completed;

    private void Update()
    {
        if (completed || Pictures.Count == 0 || Pictures.Count != Answers.Count)
        {
            return;
        }

        for (int i = 0; i < Pictures.Count; i++)
        {
            if (!IsWithinDistance(Pictures[i], Answers[i], detectionDistance))
            {
                return;
            }
        }

        completed = true;
        GameConditionManager.Instance.SetCondition(completionCondition);
    }
    private bool IsWithinDistance(Transform target1 ,Transform target2, float maxDistance)
    {
        if (target1 == null || target2 == null)
            return false;

        Vector3 offset = target1.position - target2.position;
        return offset.sqrMagnitude <= maxDistance * maxDistance;
    }
}
