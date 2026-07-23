using UnityEngine;
using System.Collections;
using Sinsam.SingletonSystem;
using System.Collections.Generic;
using System;

public enum CollectionType
{
    None,
    Picture,
}
[DefaultExecutionOrder(-250)]
public class CollectionSystem : MonoSingleton<CollectionSystem>
{
    public Dictionary<CollectionType, GameCondition> CollectionProgress = new Dictionary<CollectionType, GameCondition>
    {
        { CollectionType.Picture, GameCondition.AllImageFound },
    };
    public Dictionary<CollectionType, int> NeededCollectionCounts = new Dictionary<CollectionType, int>();
    public Dictionary<CollectionType, int> CurrentCollectionCounts = new Dictionary<CollectionType, int>();

    private readonly Dictionary<CollectionType, HashSet<string>> collectedIds = new();
    private readonly HashSet<CollectionType> completedCollections = new();

    public event Action<CollectionType, string, int, int> OnCollectionCountChanged;
    public event Action<CollectionType> OnCollectionCompleted;

    public CollectionType CurrentCollection;

    protected override bool ShouldPersist() => false;

    public bool AddCollection(CollectionType type, string uniqueId = null)
    {
        if (type == CollectionType.None)
        {
            Debug.LogWarning("Cannot add collection of type None.");
            return false;
        }
        if (type != CurrentCollection)
        {
            Debug.LogWarning($"Current collection type is {CurrentCollection}, but tried to add {type}. Ignoring.");
            return false;
        }

        if (!string.IsNullOrWhiteSpace(uniqueId))
        {
            if (!collectedIds.TryGetValue(type, out HashSet<string> ids))
            {
                ids = new HashSet<string>();
                collectedIds[type] = ids;
            }

            if (!ids.Add(uniqueId))
            {
                Debug.LogWarning($"Collection id '{uniqueId}' was already collected.");
                return false;
            }
        }
        if (!CurrentCollectionCounts.ContainsKey(type))
        {
            CurrentCollectionCounts[type] = 0;
        }
        CurrentCollectionCounts[type]++;
        int total = NeededCollectionCounts.TryGetValue(type, out int needed) ? needed : 0;
        OnCollectionCountChanged?.Invoke(type, uniqueId, CurrentCollectionCounts[type], total);
        CheckCollectionCompletion(type);
        return true;
    }
    public void CheckCollectionCompletion(CollectionType type)
    {
        if (CurrentCollectionCounts.ContainsKey(type) && NeededCollectionCounts.ContainsKey(type))
        {
            if (CurrentCollectionCounts[type] >= NeededCollectionCounts[type]
                && completedCollections.Add(type))
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
        NeededCollectionCounts[type] = Mathf.Max(0, neededCount);
        if (!CurrentCollectionCounts.ContainsKey(type))
        {
            CurrentCollectionCounts[type] = 0; // Initialize current count if not set
        }
    }
    public void CollectionCompletion(CollectionType type)
    {
        GameConditionManager.Instance.SetCondition(CollectionProgress[type]);
        CurrentCollection = CollectionType.None;
        OnCollectionCompleted?.Invoke(type);
    }

    public void ResetCollection(CollectionType type, int neededCount)
    {
        CurrentCollection = type;
        NeededCollectionCounts[type] = Mathf.Max(0, neededCount);
        CurrentCollectionCounts[type] = 0;
        collectedIds[type] = new HashSet<string>();
        completedCollections.Remove(type);
        OnCollectionCountChanged?.Invoke(type, string.Empty, 0, NeededCollectionCounts[type]);
    }
}
