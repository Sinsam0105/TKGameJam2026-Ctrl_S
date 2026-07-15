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

        public void SetContent(GameContentSetSO content)
        {
            if (current == content) return;
            current = content;
            ContentChanged?.Invoke(current);
        }

        public void RestartCurrentGame() => GameSessionManager.Instance?.RestartCurrentGame();
    }
}
