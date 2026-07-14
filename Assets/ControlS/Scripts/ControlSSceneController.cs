using UnityEngine;
using UnityEngine.SceneManagement;

namespace ControlS
{
    /// <summary>Coordinates scene systems. Content, HUD and atmosphere are delegated to dedicated components.</summary>
    public sealed class ControlSSceneController : MonoBehaviour
    {
        public static ControlSSceneController Current { get; private set; }

        [Header("Editable Content")]
        [SerializeField] private ControlSContent content;
        [Header("Scene Systems")]
        [SerializeField] private RoomSceneView room;
        [SerializeField] private VirtualDesktop desktop;
        [SerializeField] private ControlSHudController hud;
        [SerializeField] private ControlSAtmosphereController atmosphere;
        [Header("Content Components")]
        [SerializeField] private DrawerKeypadContent drawerKeypad;
        [SerializeField] private InteractionSequence[] sequences;

        private ControlSState state;

        public ControlSContent Content => content;
        public ControlSState State => state;
        public VirtualDesktop Desktop => desktop;
        public RoomSceneView Room => room;
        public bool BlocksRoomInput => (hud != null && hud.BlocksRoomInput) ||
                                       (desktop != null && desktop.IsOpen);

        public void ConfigureSceneReferences(ControlSContent gameContent, RoomSceneView roomView,
            VirtualDesktop desktopView, ControlSHudController hudController,
            ControlSAtmosphereController atmosphereController, DrawerKeypadContent drawerContent,
            InteractionSequence[] interactionSequences)
        {
            content = gameContent;
            room = roomView;
            desktop = desktopView;
            hud = hudController;
            atmosphere = atmosphereController;
            drawerKeypad = drawerContent;
            sequences = interactionSequences;
        }

        private void Awake()
        {
            Current = this;
            var hudReady = hud != null && hud.ValidateReferences();
            var atmosphereReady = atmosphere != null && atmosphere.ValidateReferences();
            var drawerReady = drawerKeypad != null && drawerKeypad.ValidateReferences();
            var sequencesReady = sequences != null && sequences.Length > 0;
            if (content == null || room == null || desktop == null || !hudReady || !atmosphereReady ||
                !drawerReady || !sequencesReady)
            {
                Debug.LogError($"CONTROL S scene references are incomplete " +
                    $"(content={content != null}, room={room != null}, desktop={desktop != null}, " +
                    $"hud={hud != null}, hudReady={hudReady}, atmosphere={atmosphere != null}, " +
                    $"atmosphereReady={atmosphereReady}, drawerReady={drawerReady}, " +
                    $"sequencesReady={sequencesReady}). Rebuild SampleScene from Tools > CONTROL S > Rebuild SampleScene.", this);
                enabled = false;
                return;
            }

            state = new ControlSState(content);
            atmosphere.Initialize(state);
            hud.Initialize(content, state, atmosphere);
            desktop.Initialize(this, state);
            room.Initialize(this, state);
            drawerKeypad.Initialize(content, state, hud, atmosphere);
            foreach (var sequence in sequences)
                if (sequence != null) sequence.Initialize(this);
        }

        private void Update() => room?.Tick(Time.unscaledTime);

        public void SetPrompt(string value) => hud?.SetPrompt(value);

        /// <summary>Inspector의 Dynamic NarrationSO UnityEvent에서 바인딩할 수 있습니다.</summary>
        public void ShowNarration(NarrationSO narration) => hud?.ShowNarration(narration);

        /// <summary>Inspector의 Dynamic GameObject UnityEvent에서 바인딩할 수 있습니다.</summary>
        public void ShowUI(GameObject uiObject) => hud?.ShowUI(uiObject);

        public void HideUI(GameObject uiObject) => hud?.HideUI(uiObject);

        public void ToggleUI(GameObject uiObject) => hud?.ToggleUI(uiObject);

        public void HideCurrentUI() => hud?.HideCurrentUI();

        public void PlayUiTone(float frequency, float duration) => atmosphere?.PlayUiTone(frequency, duration);

        public void OpenDesktop() => desktop?.Open();

        public void CloseDesktop() => desktop?.Close();

        public void SetPlayerInput(bool value)
        {
            if (room?.Player != null) room.Player.InputEnabled = value;
        }

        public void RestartScene() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
