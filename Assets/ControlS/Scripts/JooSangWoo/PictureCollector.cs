using UnityEngine;
using System.Collections.Generic;

public class PictureCollector : MonoBehaviour
{
    [SerializeField]
    private List<Transform> Pictures;
    [SerializeField]
    private List<Transform> Answers;
    [SerializeField]
    private float detectionDistance = 5f;

    private void Update()
    {
        foreach (var picture in Pictures)
        {
            foreach (var answer in Answers)
            {
                if (IsWithinDistance(picture, answer, detectionDistance))
                {
                    Debug.Log($"Picture {picture.name} is within {detectionDistance} units of Answer {answer.name}");
                }
            }
        }
    }
    private bool IsWithinDistance(Transform target1 ,Transform target2, float maxDistance)
    {
        if (target1 == null || target2 == null)
            return false;

        Vector3 offset = target1.position - target2.position;
        return offset.sqrMagnitude <= maxDistance * maxDistance;
    }
}
