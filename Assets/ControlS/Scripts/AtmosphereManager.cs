using Sinsam.SingletonSystem;
using UnityEngine;

namespace ControlS
{
    public sealed class AtmosphereManager : MonoSingleton<AtmosphereManager>
    {
        [SerializeField] private Camera worldCamera;
        private ProgressManager progressManager;

        protected override bool ShouldPersist() => false;

        private void OnEnable()
        {
            progressManager = ProgressManager.Instance;
        }

        private void OnDisable()
        {
            progressManager = null;
        }

        private void Update()
        {
            if (worldCamera == null || progressManager == null) return;
            var progress = progressManager.CompletionRatio;
            var pulse = Mathf.Sin(Time.unscaledTime * (1.25f + progress)) * .004f;
            worldCamera.backgroundColor = new Color(.012f + pulse, .016f, .021f + progress * .006f, 1f);
        }

        public bool ValidateReferences() => worldCamera != null;
    }
}
