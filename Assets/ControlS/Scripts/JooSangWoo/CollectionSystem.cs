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
    public Dictionary<CollectionType, GameCondition> CollectionProgress = new Dictionary<CollectionType, GameCondition>();
    public Dictionary<CollectionType, int> NeededCollectionCounts = new Dictionary<CollectionType, int>();
    public Dictionary<CollectionType, int> CurrentCollectionCounts = new Dictionary<CollectionType, int>();

    public CollectionType CurrentCollection;

    public void AddCollection(CollectionType type)
    {
        if (type == CollectionType.None)
        {
            Debug.LogWarning("Cannot add collection of type None.");
            return;
        }
        if (type != CurrentCollection)
        {
            Debug.LogWarning($"Current collection type is {CurrentCollection}, but tried to add {type}. Ignoring.");
            return;
        }
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
    public void StartCollection(CollectionType type, int neededCount)
    {
        CurrentCollection = type;
        if (!NeededCollectionCounts.ContainsKey(type))
        {
            NeededCollectionCounts[type] = neededCount; // Set the needed count
        }
        if (!CurrentCollectionCounts.ContainsKey(type))
        {
            CurrentCollectionCounts[type] = 0; // Initialize current count if not set
        }
    }
    public void CollectionCompletion(CollectionType type)
    {
        GameConditionManager.Instance.SetCondition(CollectionProgress[type]);
    }
}
