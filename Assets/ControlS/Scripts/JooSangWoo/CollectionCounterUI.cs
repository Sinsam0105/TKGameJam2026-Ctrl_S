using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public sealed class CollectionCounterUI : MonoBehaviour
{
    [SerializeField] private CollectionType collectionType = CollectionType.Picture;
    [SerializeField] private int fallbackTotalCount = 12;
    [SerializeField] private bool startCollectionOnEnable = true;

    private Text counterText;
    private bool collectionStarted;
    private int previousCount = -1;
    private int previousTotal = -1;

    private void Awake()
    {
        counterText = GetComponent<Text>();
    }
    private void Update()
    {
        CollectionSystem collectionSystem = CollectionSystem.Instance;
        if (collectionSystem == null)
        {
            return;
        }
        if (collectionType == CollectionType.None)
        {
            counterText.text = string.Empty;
        }

        int currentCount = collectionSystem.CurrentCollectionCounts.TryGetValue(collectionType, out int current)
            ? current
            : 0;
        int totalCount = collectionSystem.NeededCollectionCounts.TryGetValue(collectionType, out int total)
            ? total
            : fallbackTotalCount;

        if (currentCount == previousCount && totalCount == previousTotal)
        {
            return;
        }

        previousCount = currentCount;
        previousTotal = totalCount;
        counterText.text = $"{Mathf.Clamp(currentCount, 0, totalCount)}/{totalCount}";
    }
}
