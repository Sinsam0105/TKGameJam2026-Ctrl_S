using System;
using UnityEngine;

namespace ControlS
{
    public sealed class RoomSceneView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer floor;
        [SerializeField] private SpriteRenderer[] lampGlow = Array.Empty<SpriteRenderer>();
        [SerializeField] private GameObject restoredPhoto;
        [SerializeField] private TopDownPlayer player;
        [SerializeField] private ProgressFlagSO atmosphereChangedFlag;
        [SerializeField] private ProgressFlagSO restoredPhotoFlag;
        [SerializeField] private RoomArtSetSO artSet;

        private float flickerSeed;
        private ProgressManager progressManager;
        private ContentManager contentManager;
        public TopDownPlayer Player => player;

        private void Awake()
        {
            ApplyArt();
        }

        private void OnEnable()
        {
            progressManager = ProgressManager.Instance;
            contentManager = ContentManager.Instance;
            if (artSet == null && contentManager != null) artSet = contentManager.Current?.roomArt;
            ApplyArt();
            if (progressManager != null) progressManager.ProgressChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (progressManager != null) progressManager.ProgressChanged -= Refresh;
            progressManager = null;
            contentManager = null;
        }

        private void Update()
        {
            if (progressManager == null) return;
            if (floor != null)
            {
                var normalized = progressManager.CompletionRatio;
                var pulse = Mathf.Sin(Time.unscaledTime * (1.1f + normalized * .8f)) * .012f;
                var baseColor = artSet != null ? artSet.floorBaseColor : new Color(.075f, .09f, .105f, 1f);
                floor.color = new Color(baseColor.r + pulse, baseColor.g + pulse * .5f,
                    baseColor.b + pulse * .25f, baseColor.a);
            }

            var intensity = .62f;
            if (atmosphereChangedFlag != null && progressManager.GetFlag(atmosphereChangedFlag))
            {
                flickerSeed += Time.unscaledDeltaTime;
                var randomCut = Mathf.PerlinNoise(flickerSeed * 7f, 12.7f) < .18f;
                intensity = randomCut ? .08f : .48f + Mathf.Sin(Time.unscaledTime * 9f) * .06f;
            }
            foreach (var glow in lampGlow)
            {
                if (glow == null) continue;
                var color = glow.color;
                color.a = intensity;
                glow.color = color;
            }
        }

        public void ApplyArt()
        {
            if (artSet == null) return;
            if (floor != null && artSet.floor != null) floor.sprite = artSet.floor;
            var playerRenderer = player != null ? player.GetComponent<SpriteRenderer>() : null;
            if (playerRenderer != null && artSet.player != null) playerRenderer.sprite = artSet.player;
            var photoRenderer = restoredPhoto != null ? restoredPhoto.GetComponent<SpriteRenderer>() : null;
            if (photoRenderer != null && artSet.restoredPhoto != null) photoRenderer.sprite = artSet.restoredPhoto;
        }

        private void Refresh()
        {
            if (restoredPhoto != null)
                restoredPhoto.SetActive(restoredPhotoFlag != null && progressManager != null && progressManager.GetFlag(restoredPhotoFlag));
        }
    }
}
