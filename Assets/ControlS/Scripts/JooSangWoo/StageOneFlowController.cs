using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum StageOnePhase
{
    Prologue,
    RecoveryPrompt,
    Collecting,
    Assembling,
    Scanning,
    Stage1Complete,
    Stage2Preview,
}

[Serializable]
public sealed class StageOneScriptIds
{
    public string PrologueOpening = "PrologueOpening";
    public string PrologueReturnToWork = "PrologueReturnToWork";
    public string PrologueStudyBook = "PrologueStudyBook";
    public string PrologueFinish = "PrologueFinish";
    public string CrashReaction = "CrashReaction";
    public string RecoveryReaction = "RecoveryReaction";
    public string Stage1Intro = "Stage1Intro";
    public string PickupBed = "PickupBed";
    public string PickupHalf = "PickupHalf";
    public string CollectionComplete = "CollectionComplete";
    public string PhotoReveal = "PhotoReveal";
    public string ScanComplete = "ScanComplete";
    public string BalconyReaction = "BalconyReaction";
}

[DefaultExecutionOrder(100)]
public sealed class StageOneFlowController : MonoBehaviour
{
    private const int RequiredPhotoCount = 12;
    private const string ScannerActionId = "scanner";
    private const string ComputerActionId = "computer";

    [Header("Existing UI and Prefabs")]
    [SerializeField] private Text objectiveText;
    [SerializeField] private Text actionText;
    [SerializeField] private Text recoveryBodyText;
    [SerializeField] private Text recoveryProgressText;
    [SerializeField] private Image monitorBlackout;
    [SerializeField] private GameObject recoveryWindow;
    [SerializeField] private GameObject collectionHud;
    [SerializeField] private GameObject stageTwoPreviewRoot;
    [SerializeField] private Button startRecoveryButton;
    [SerializeField] private ComputerWindowedUI computerDesktop;
    [SerializeField] private LightController computerScreenLight;

    [Header("Existing Stage 1 Systems")]
    [SerializeField] private PuzzleWindowedUI photoPuzzleWindow;
    [SerializeField] private PictureCollector pictureCollector;
    [SerializeField] private RoomInteractable computerInteractable;
    [SerializeField] private List<RoomInteractable> photoPieces = new();
    [SerializeField] private List<FurnitureViewWindow> furnitureViews = new();

    [Header("Stage 2 Preview Only")]
    [SerializeField] private List<GameObject> stageTwoInvestigationMarkers = new();
    [SerializeField] private StageTwoFlowController stageTwoFlow;

    [Header("Balcony Staging")]
    [SerializeField] private Transform balconyDoor;
    [SerializeField] private Vector3 balconyDoorOpenOffset = new(-1.25f, 0f, 0f);
    [SerializeField, Min(0.1f)] private float balconyDoorOpenDuration = 1.25f;
    [SerializeField] private AudioSource balconyAmbienceSource;

    [Header("Ambience and Effects")]
    [SerializeField] private List<AudioSource> ambienceSources = new();
    [SerializeField] private AudioSource keyboardLoopSource;
    [SerializeField] private AudioSource keyPressSource;
    [SerializeField] private AudioSource effectsSource;
    [SerializeField] private AudioClip keyPressClip;
    [SerializeField] private AudioClip computerCrashClip;
    [SerializeField] private AudioClip computerBootClip;
    [SerializeField] private AudioClip paperHintClip;
    [SerializeField] private AudioClip photoPickupClip;
    [SerializeField] private AudioClip scannerClip;
    [SerializeField, Min(1f)] private float hintDelay = 20f;

    [Header("Dialogue Data")]
    [SerializeField] private StageOneScriptIds scriptIds = new();

    [Header("Events")]
    [SerializeField] private UnityEvent onStageOneCompleted = new();
    [SerializeField] private UnityEvent onStageTwoPreview = new();

    private float hintTimer;
    private bool recoveryStarted;
    private bool puzzleRevealRunning;
    private Coroutine sequenceRoutine;
    private Coroutine actionRoutine;
    private CollectionSystem collectionSystem;
    private GameConditionManager conditionManager;
    private Vector3 balconyDoorClosedPosition;
    private float balconyAmbienceTargetVolume;

    public StageOnePhase CurrentPhase { get; private set; }
    public int RecoveryProgress => GameConditionManager.Instance != null
        ? GameConditionManager.Instance.RecoveryProgress
        : 0;

    public event Action<StageOnePhase> PhaseChanged;
    public event Action<int> RecoveryProgressChanged;

    private void Awake()
    {
        ApplyNewScriptIds();

        if (balconyDoor != null)
            balconyDoorClosedPosition = balconyDoor.localPosition;
        if (balconyAmbienceSource != null)
            balconyAmbienceTargetVolume = balconyAmbienceSource.volume;

        if (startRecoveryButton != null)
            startRecoveryButton.onClick.AddListener(StartRecovery);

        collectionSystem = CollectionSystem.Instance;
        conditionManager = GameConditionManager.Instance;

        if (collectionSystem != null)
        {
            collectionSystem.OnCollectionCountChanged += OnCollectionCountChanged;
            collectionSystem.OnCollectionCompleted += OnCollectionCompleted;
        }

        if (pictureCollector != null)
        {
            pictureCollector.OnPuzzleCompleted.AddListener(OnPicturePuzzleCompleted);
            pictureCollector.OnScanRequested.AddListener(OnScanRequested);
        }

        if (conditionManager != null)
            conditionManager.OnRecoveryProgressChanged += OnRecoveryProgressUpdated;

        foreach (FurnitureViewWindow view in furnitureViews)
        {
            if (view != null)
                view.ActionClicked += OnFurnitureAction;
        }
    }

    private void Start()
    {
        ResetStage();
        sequenceRoutine = StartCoroutine(PrologueSequence());
    }

    // 새 대본(D0xx)을 적용한다. 씬에 직렬화된 옛 ID를 코드에서 새 ID로 덮어써
    // 이후 모든 scriptIds.X 참조가 새 대본을 재생하도록 한다.
    private void ApplyNewScriptIds()
    {
        scriptIds.PrologueOpening = "D001ProAssignment";
        scriptIds.PrologueFinish = "D003ProSaveAttempt";
        scriptIds.CrashReaction = "D004ProCrash";
        scriptIds.RecoveryReaction = "D005ProRecovery";
        scriptIds.Stage1Intro = "D006Stage1Start";
        scriptIds.PickupBed = "D007Stage1PiecePicked";
        scriptIds.PickupHalf = "D008Stage1HalfCollected";
        scriptIds.CollectionComplete = "D009Stage1PhotoAssembled";
        scriptIds.PhotoReveal = "D010Stage1PhotoReaction";
        scriptIds.ScanComplete = "D011Stage1ScanComplete";
        scriptIds.BalconyReaction = "D012Dialogue20BalconyDoorNoticed";
    }

    private void Update()
    {
        if (CurrentPhase != StageOnePhase.Collecting)
            return;

        hintTimer += Time.unscaledDeltaTime;
        if (hintTimer >= hintDelay)
        {
            hintTimer = 0f;
            PlayNearestPhotoHint();
        }
    }

    private void OnDestroy()
    {
        if (startRecoveryButton != null)
            startRecoveryButton.onClick.RemoveListener(StartRecovery);

        if (collectionSystem != null)
        {
            collectionSystem.OnCollectionCountChanged -= OnCollectionCountChanged;
            collectionSystem.OnCollectionCompleted -= OnCollectionCompleted;
        }

        if (pictureCollector != null)
        {
            pictureCollector.OnPuzzleCompleted.RemoveListener(OnPicturePuzzleCompleted);
            pictureCollector.OnScanRequested.RemoveListener(OnScanRequested);
        }

        if (conditionManager != null)
            conditionManager.OnRecoveryProgressChanged -= OnRecoveryProgressUpdated;

        foreach (FurnitureViewWindow view in furnitureViews)
        {
            if (view != null)
                view.ActionClicked -= OnFurnitureAction;
        }
    }

    // 책상 정면샷의 스캐너를 눌렀을 때. 조립이 끝난 뒤에만 통과한다.
    private void OnFurnitureAction(string actionId)
    {
        bool isScanner = string.Equals(actionId, ScannerActionId, StringComparison.OrdinalIgnoreCase);
        bool isComputer = string.Equals(actionId, ComputerActionId, StringComparison.OrdinalIgnoreCase);
        if (!isScanner && !isComputer)
            return;

        foreach (FurnitureViewWindow view in furnitureViews)
        {
            if (view != null && view.gameObject.activeInHierarchy)
                view.CloseWindow();
        }

        if (isScanner)
            OnScanRequested();
        else
            computerInteractable?.TryInteract();
    }

    private void ResetStage()
    {
        recoveryStarted = false;
        puzzleRevealRunning = false;
        hintTimer = 0f;

        GameConditionManager.Instance?.ResetProgress();
        CollectionSystem.Instance?.ResetCollection(CollectionType.Picture, RequiredPhotoCount);
        GameStateManager.Instance?.SetState(GameState.UI);

        SetPhase(StageOnePhase.Prologue);
        SetText(objectiveText, string.Empty);
        SetText(actionText, string.Empty);
        SetText(recoveryProgressText, "Recovery Progress: 0%");

        SetActive(recoveryWindow, false);
        SetActive(collectionHud, false);
        SetActive(stageTwoPreviewRoot, false);
        SetBlackout(false);
        computerScreenLight?.TurnOn(true);

        if (balconyDoor != null)
        {
            balconyDoor.gameObject.SetActive(true);
            balconyDoor.localPosition = balconyDoorClosedPosition;
        }
        if (balconyAmbienceSource != null)
            balconyAmbienceSource.volume = balconyAmbienceTargetVolume;

        if (photoPuzzleWindow != null)
            photoPuzzleWindow.gameObject.SetActive(false);
        ConfigureComputerForDesktop(false);

        foreach (RoomInteractable photoPiece in photoPieces)
        {
            if (photoPiece == null)
                continue;

            photoPiece.ResetInteraction();
            photoPiece.gameObject.SetActive(false);
        }

        foreach (GameObject marker in stageTwoInvestigationMarkers)
            SetActive(marker, false);
        stageTwoFlow?.ResetStage();

        StartAmbience();
        computerDesktop?.OpenForStory();
    }

    private IEnumerator PrologueSequence()
    {
        if (keyboardLoopSource != null)
            keyboardLoopSource.Play();

        // 새 대본: D001이 과제 투덜 3~6줄을 모두 담으므로 옛 Opening/Return/Study를 하나로 대체한다.
        yield return PlayScript(scriptIds.PrologueOpening);   // D001ProAssignment
        yield return PlayScript("D002ProComplete");          // 과제 완료 반응
        yield return PlayScript(scriptIds.PrologueFinish);   // D003ProSaveAttempt: "이제 저장하고 자자"

        // 저장 안내는 System 말풍선으로 띄운다. (목표 텍스트 대체)
        SpeechBubbleController.ShowSystem("Ctrl + S 를 눌러 저장한다.");

        Keyboard keyboard = Keyboard.current;
        while (true)
        {
            keyboard = Keyboard.current;
            if (keyboard != null)
            {
                bool ctrlHeld = keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
                if (ctrlHeld && keyboard.sKey.wasPressedThisFrame)
                    break;
            }
            yield return null;
        }

        SetText(objectiveText, string.Empty);
        SpeechBubbleController.Get(ScriptData.EObject.System)?.ForceClose();
        if (keyboardLoopSource != null)
            keyboardLoopSource.Stop();

        if (keyPressSource != null && keyPressClip != null)
        {
            keyPressSource.pitch = 0.45f;
            keyPressSource.clip = keyPressClip;
            keyPressSource.Play();
        }

        yield return new WaitForSecondsRealtime(0.45f);
        StopAmbience();
        computerScreenLight?.TurnOff();
        PlayEffect(computerCrashClip);
        SetBlackout(true);

        yield return PlayScript(scriptIds.CrashReaction);
        yield return new WaitForSecondsRealtime(2f);

        computerScreenLight?.TurnOn(true);
        PlayEffect(computerBootClip);
        yield return new WaitForSecondsRealtime(1.5f);
        SetBlackout(false);
        SetActive(recoveryWindow, true);
        SetText(recoveryBodyText,
            "Unsaved Project Detected.\n\nRecovery Wizard is available.");
        if (startRecoveryButton != null)
        {
            startRecoveryButton.gameObject.SetActive(true);
            startRecoveryButton.interactable = true;
        }

        yield return PlayScript(scriptIds.RecoveryReaction);
        SetPhase(StageOnePhase.RecoveryPrompt);
        sequenceRoutine = null;
    }

    public void StartRecovery()
    {
        if (recoveryStarted || CurrentPhase != StageOnePhase.RecoveryPrompt)
            return;

        recoveryStarted = true;
        if (startRecoveryButton != null)
        {
            startRecoveryButton.interactable = false;
            startRecoveryButton.gameObject.SetActive(false);
        }

        sequenceRoutine = StartCoroutine(StartCollectionSequence());
    }

    private IEnumerator StartCollectionSequence()
    {
        GameConditionManager.Instance?.SetCondition(GameCondition.PrologueEnded);
        CollectionSystem.Instance?.ResetCollection(CollectionType.Picture, RequiredPhotoCount);

        SetText(recoveryBodyText,
            "STEP 1\n\nIMAGE RECOVERY\n\nPreview Image Missing.\n\nPlease locate and reconstruct\nthe original printed image.");
        SetText(recoveryProgressText, "Recovery Progress: 0%");

        yield return PlayScript(scriptIds.Stage1Intro);
        SetActive(recoveryWindow, false);
        computerDesktop?.CloseForStory();
        ConfigureComputerForDesktop(true);
        SetActive(collectionHud, true);

        foreach (RoomInteractable photoPiece in photoPieces)
        {
            if (photoPiece != null)
                photoPiece.gameObject.SetActive(true);
        }

        SetText(objectiveText, "사진 조각 찾기  0/12");
        SetPhase(StageOnePhase.Collecting);
        GameStateManager.Instance?.SetRoom();
        sequenceRoutine = null;
    }

    private void OnCollectionCountChanged(CollectionType type, string id, int count, int total)
    {
        if (type != CollectionType.Picture || CurrentPhase != StageOnePhase.Collecting)
            return;

        hintTimer = 0f;
        PlayEffect(photoPickupClip);
        SetText(objectiveText, $"사진 조각 찾기  {count}/{Mathf.Max(total, RequiredPhotoCount)}");
        FlashAction("사진 조각을 찾았다.", 1.4f);

        if (count == 1 && id.StartsWith("bed", StringComparison.OrdinalIgnoreCase))
            StartCoroutine(PlayRoomScript(scriptIds.PickupBed));
        else if (count == RequiredPhotoCount / 2)
            StartCoroutine(PlayRoomScript(scriptIds.PickupHalf));
    }

    private void OnCollectionCompleted(CollectionType type)
    {
        if (type != CollectionType.Picture || CurrentPhase != StageOnePhase.Collecting)
            return;

        SetPhase(StageOnePhase.Assembling);
        SetText(objectiveText, "컴퓨터에서 사진을 조립하고 스캔하기");
        ConfigureComputerForPuzzle();
        StartCoroutine(PlayRoomScript(scriptIds.CollectionComplete));
    }

    private IEnumerator PlayRoomScript(string id)
    {
        GameState previous = GameStateManager.Instance != null
            ? GameStateManager.Instance.State
            : GameState.Room;
        GameStateManager.Instance?.SetState(GameState.UI);
        yield return PlayScript(id);

        if (CurrentPhase is StageOnePhase.Collecting or StageOnePhase.Assembling)
            GameStateManager.Instance?.SetRoom();
        else
            GameStateManager.Instance?.SetState(previous);
    }

    private void OnPicturePuzzleCompleted()
    {
        if (puzzleRevealRunning || CurrentPhase != StageOnePhase.Assembling)
            return;

        puzzleRevealRunning = true;
        StartCoroutine(PhotoRevealSequence());
    }

    private IEnumerator PhotoRevealSequence()
    {
        GameStateManager.Instance?.SetState(GameState.UI);
        yield return PlayScript(scriptIds.PhotoReveal);

        // 스캔은 이제 책상 정면샷의 스캐너에서 한다. 퍼즐 창을 닫고 방으로 돌려보낸다.
        pictureCollector?.SetScanAvailable(false);
        pictureCollector?.SetStatus("스캐너에 넣어야 한다.");
        photoPuzzleWindow?.CloseWindow();
        SetText(objectiveText, "책상의 스캐너에 사진을 넣기");
        GameStateManager.Instance?.SetRoom();
        puzzleRevealRunning = false;
    }

    private void OnScanRequested()
    {
        if (CurrentPhase != StageOnePhase.Assembling || pictureCollector == null || !pictureCollector.IsCompleted)
            return;

        SetPhase(StageOnePhase.Scanning);
        StartCoroutine(ScanSequence());
    }

    private IEnumerator ScanSequence()
    {
        GameStateManager.Instance?.SetState(GameState.UI);
        PlayEffect(scannerClip);

        pictureCollector.SetStatus("Scanning source...");
        yield return new WaitForSecondsRealtime(1f);
        pictureCollector.SetStatus("Image integrity confirmed.");
        yield return new WaitForSecondsRealtime(1f);
        pictureCollector.SetStatus("IMAGE RESTORED");
        yield return new WaitForSecondsRealtime(0.8f);

        GameConditionManager.Instance?.SetRecoveryStage(RecoveryStage.ImageRecovery);
        pictureCollector.SetStatus("Recovery Progress: 20%");
        SetText(recoveryProgressText, "Recovery Progress: 20%");
        SetPhase(StageOnePhase.Stage1Complete);
        onStageOneCompleted?.Invoke();

        yield return new WaitForSecondsRealtime(0.8f);
        photoPuzzleWindow?.CloseWindow();
        GameStateManager.Instance?.SetState(GameState.UI);
        yield return PlayScript(scriptIds.ScanComplete);
        yield return OpenBalconyDoor();
        yield return new WaitForSecondsRealtime(0.4f);
        yield return PlayScript(scriptIds.BalconyReaction);

        computerDesktop?.OpenForStory();
        SetText(recoveryBodyText,
            "STEP 2\n\nACTIVITY LOG RECOVERY\n\nSecurity Verification Required.\n\nVerify the last valid edit time using\nthe microwave and the outside clock.");
        SetActive(recoveryWindow, true);
        SetActive(stageTwoPreviewRoot, true);
        foreach (GameObject marker in stageTwoInvestigationMarkers)
            SetActive(marker, true);

        SetText(objectiveText, "2단계 조사 대상: 전자레인지 · 베란다 외부 시계");
        yield return new WaitForSecondsRealtime(3f);
        SetActive(recoveryWindow, false);
        computerDesktop?.CloseForStory();
        ConfigureComputerForDesktop(false);

        SetPhase(StageOnePhase.Stage2Preview);
        onStageTwoPreview?.Invoke();
        GameStateManager.Instance?.SetRoom();
        stageTwoFlow?.BeginStage(computerInteractable, objectiveText);
    }

    private IEnumerator OpenBalconyDoor()
    {
        if (balconyDoor == null)
            yield break;

        Vector3 start = balconyDoorClosedPosition;
        Vector3 destination = start + balconyDoorOpenOffset;
        balconyDoor.localPosition = start;

        if (balconyAmbienceSource != null)
        {
            balconyAmbienceSource.Stop();
            balconyAmbienceSource.volume = 0f;
            balconyAmbienceSource.Play();
        }

        float elapsed = 0f;
        while (elapsed < balconyDoorOpenDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / balconyDoorOpenDuration);
            float eased = Mathf.SmoothStep(0f, 1f, normalized);
            balconyDoor.localPosition = Vector3.LerpUnclamped(start, destination, eased);

            if (balconyAmbienceSource != null)
                balconyAmbienceSource.volume = Mathf.Lerp(0f, balconyAmbienceTargetVolume, eased);

            yield return null;
        }

        balconyDoor.localPosition = destination;
        if (balconyAmbienceSource != null)
            balconyAmbienceSource.volume = balconyAmbienceTargetVolume;
    }

    private void PlayNearestPhotoHint()
    {
        Transform player = FindAnyObjectByType<PlayerMove>()?.transform;
        RoomInteractable nearest = null;
        float nearestDistance = float.PositiveInfinity;

        foreach (RoomInteractable photoPiece in photoPieces)
        {
            if (photoPiece == null || !photoPiece.gameObject.activeInHierarchy || photoPiece.IsConsumed)
                continue;

            float distance = player == null
                ? 0f
                : (photoPiece.transform.position - player.position).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = photoPiece;
            }
        }

        if (nearest == null || paperHintClip == null)
            return;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySfxAt(paperHintClip, nearest.transform.position, 0.75f);
        else
            AudioSource.PlayClipAtPoint(paperHintClip, nearest.transform.position, 0.75f);
    }

    private void ConfigureComputerForDesktop(bool enabled)
    {
        if (computerInteractable == null || computerInteractable.puzzleAction == null)
            return;

        computerInteractable.Configure("computer_desktop", "[E] 컴퓨터 사용", false);
        computerInteractable.puzzleAction.Conditions = new List<GameCondition> { GameCondition.PrologueEnded };
        computerInteractable.puzzleAction.OpeningUI = computerDesktop;
        computerInteractable.enabled = enabled;
    }

    private void ConfigureComputerForPuzzle()
    {
        if (computerInteractable == null || computerInteractable.puzzleAction == null)
            return;

        computerInteractable.Configure("computer_stage1", "[E] 사진 조립 시작", false);
        computerInteractable.puzzleAction.Conditions = new List<GameCondition> { GameCondition.AllImageFound };
        computerInteractable.puzzleAction.OpeningUI = photoPuzzleWindow;
        computerInteractable.enabled = true;
    }

    private IEnumerator PlayScript(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || ScriptManager.Instance == null)
            yield break;

        ScriptManager.Instance.Play(id);
        yield return null;
        while (ScriptManager.Instance != null && ScriptManager.Instance.IsPlaying)
            yield return null;
    }

    // 조사 결과를 짧게 띄운다. 연속으로 찾아도 마지막 문구만 남도록 이전 것을 끊는다.
    private void FlashAction(string message, float duration)
    {
        if (actionRoutine != null)
            StopCoroutine(actionRoutine);
        actionRoutine = StartCoroutine(FlashActionRoutine(message, duration));
    }

    private IEnumerator FlashActionRoutine(string message, float duration)
    {
        yield return ShowAction(message, duration);
        actionRoutine = null;
    }

    private IEnumerator ShowAction(string message, float duration)
    {
        SetText(actionText, message);
        yield return new WaitForSecondsRealtime(duration);
        SetText(actionText, string.Empty);
    }

    private void StartAmbience()
    {
        foreach (AudioSource source in ambienceSources)
        {
            if (source != null && source.clip != null)
                source.Play();
        }
    }

    private void StopAmbience()
    {
        foreach (AudioSource source in ambienceSources)
            source?.Stop();
        keyboardLoopSource?.Stop();
    }

    private void PlayEffect(AudioClip clip)
    {
        if (clip == null)
            return;

        // 효과음은 일괄 SoundManager로 보내고, 없으면 로컬 소스로 대체한다.
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySfx(clip);
        else if (effectsSource != null)
            effectsSource.PlayOneShot(clip);
    }

    private void SetBlackout(bool value)
    {
        if (monitorBlackout == null)
            return;

        monitorBlackout.gameObject.SetActive(value);
        Color color = monitorBlackout.color;
        color.a = value ? 1f : 0f;
        monitorBlackout.color = color;
    }

    private void SetPhase(StageOnePhase phase)
    {
        if (CurrentPhase == phase && phase != StageOnePhase.Prologue)
            return;

        CurrentPhase = phase;
        PhaseChanged?.Invoke(CurrentPhase);
    }

    private void OnRecoveryProgressUpdated(int value)
    {
        RecoveryProgressChanged?.Invoke(value);
    }

    private static void SetText(Text text, string value)
    {
        if (text != null)
            text.text = value;
    }

    private static void SetActive(GameObject target, bool value)
    {
        if (target != null)
            target.SetActive(value);
    }
}
