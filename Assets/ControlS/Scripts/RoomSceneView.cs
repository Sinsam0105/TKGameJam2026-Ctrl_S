using System;
using UnityEngine;

namespace ControlS
{
    /// <summary>References the room objects that are saved directly in SampleScene.</summary>
    public sealed class RoomSceneView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer floor;
        [SerializeField] private SpriteRenderer[] lampGlow = Array.Empty<SpriteRenderer>();
        [SerializeField] private GameObject restoredPhoto;
        [SerializeField] private TopDownPlayer player;

        private ControlSState state;
        private float flickerSeed;

        public TopDownPlayer Player => player;

        public void Configure(SpriteRenderer floorRenderer, SpriteRenderer[] lampRenderers,
            GameObject photoObject, TopDownPlayer playerController)
        {
            floor = floorRenderer;
            lampGlow = lampRenderers;
            restoredPhoto = photoObject;
            player = playerController;
        }

        public void Initialize(ControlSSceneController owner, ControlSState gameState)
        {
            state = gameState;
            if (player != null) player.Initialize(owner);
            foreach (var interactable in GetComponentsInChildren<RoomInteractable>(true))
                interactable.Initialize(owner);

            state.Changed += Refresh;
            Refresh();
        }

        public void Tick(float time)
        {
            if (state == null) return;
            if (floor != null)
            {
                var progress = state.CompletedPuzzleCount / 4f;
                var pulse = Mathf.Sin(time * (1.1f + progress * .8f)) * .012f;
                floor.color = new Color(.075f + pulse, .09f + pulse * .5f, .105f + pulse * .25f, 1f);
            }

            var intensity = .62f;
            if (state.SaveFileRepaired)
            {
                flickerSeed += Time.unscaledDeltaTime;
                var randomCut = Mathf.PerlinNoise(flickerSeed * 7f, 12.7f) < .18f;
                intensity = randomCut ? .08f : .48f + Mathf.Sin(time * 9f) * .06f;
            }

            foreach (var glow in lampGlow)
            {
                if (glow == null) continue;
                var color = glow.color;
                color.a = intensity;
                glow.color = color;
            }
        }

        private void Refresh()
        {
            if (restoredPhoto != null && state != null) restoredPhoto.SetActive(state.FamilyPhotoRestored);
        }
    }
}
