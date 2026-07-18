using UnityEngine;
using Sinsam.SingletonSystem;
using System.Collections.Generic;

public class RoomManager
{
    public List<Canvas> RoomHorrorLayer;
    public List<int> Progress;
    public int CurrentRoomLayerIndex = -1;

    private void Update()
    {
        if (GameConditionManager.Instance == null)
            return;

        int count = Mathf.Min(RoomHorrorLayer.Count, Progress.Count);

        while (CurrentRoomLayerIndex + 1 < count)
        {
            int nextIndex = CurrentRoomLayerIndex + 1;

            if (Progress[nextIndex] > GameConditionManager.Instance.GameProgress)
                break;

            CurrentRoomLayerIndex = nextIndex;

            if (RoomHorrorLayer[CurrentRoomLayerIndex] != null)
            {
                RoomHorrorLayer[CurrentRoomLayerIndex]
                    .gameObject
                    .SetActive(true);
            }
        }
    }
}