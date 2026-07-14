using UnityEngine;

namespace ControlS
{
    [CreateAssetMenu(fileName = "NewNarration", menuName = "CONTROL S/Narration")]
    public sealed class NarrationSO : ScriptableObject
    {
        [SerializeField] private string narrationId;
        [SerializeField, TextArea(3, 10)] private string text;
        [SerializeField, Min(.1f)] private float duration = 3.5f;

        public string NarrationId => narrationId;
        public string Text => text;
        public float Duration => duration;

        public void Configure(string id, string value, float seconds)
        {
            narrationId = id;
            text = value;
            duration = Mathf.Max(.1f, seconds);
        }
    }
}
