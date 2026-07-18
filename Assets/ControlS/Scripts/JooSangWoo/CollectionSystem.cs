using UnityEngine;
using System.Collections;
using Sinsam.SingletonSystem;
using System.Collections.Generic;

public enum CollectionType
{
    None,
}
public class CollectionSystem : MonoSingleton<CollectionSystem>
{
    public Dictionary<CollectionType, int> NeededCollectionCounts = new Dictionary<CollectionType, int>();
    public Dictionary<CollectionType, int> CurrentCollectionCounts = new Dictionary<CollectionType, int>();

    public CollectionType CurrentCollection;

    public void AddCollection(CollectionType type)
    {
        if (!CurrentCollectionCounts.ContainsKey(type))
        {
            CurrentCollectionCounts[type] = 0;
        }
        CurrentCollectionCounts[type]++;
        CheckCollectionCompletion(type);
    }
    public void CheckCollectionCompletion(CollectionType type)
    {
        if (CurrentCollectionCounts.ContainsKey(type) && NeededCollectionCounts.ContainsKey(type))
        {
            if (CurrentCollectionCounts[type] >= NeededCollectionCounts[type])
            {
                // Collection complete
                Debug.Log($"Collection of type {type} is complete!");
                CollectionCompletion(type);
            }
        }
    }
    public void CollectionCompletion(CollectionType type)
    {

    }
}
