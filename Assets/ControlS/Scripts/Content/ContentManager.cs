using System;
using Sinsam.SingletonSystem;
using UnityEngine;

namespace ControlS
{
    public sealed class ContentManager : MonoSingleton<ContentManager>
    {
        [SerializeField] private GameContentSetSO current;

        public event Action<GameContentSetSO> ContentChanged;
        public GameContentSetSO Current => current;

        protected override bool ShouldPersist() => false;

        private void OnEnable()
        {
            ApplyProgressSet();
        }

        public void SetContent(GameContentSetSO content)
        {
            if (current == content) return;
            current = content;
            ApplyProgressSet();
            ContentChanged?.Invoke(current);
        }

        public void ApplyProgressSet()
        {
            ProgressManager.Instance?.Configure(current != null ? current.progress : null);
        }

        public void RestartCurrentGame() => GameSessionManager.Instance?.RestartCurrentGame();
    }
}
