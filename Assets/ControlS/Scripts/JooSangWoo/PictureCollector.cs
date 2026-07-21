using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PictureCollector : MonoBehaviour
{
    [SerializeField] private List<Transform> Pictures = new List<Transform>();
    [SerializeField] private List<Transform> Answers = new List<Transform>();
    [SerializeField] private GameCondition completionCondition = GameCondition.ImagePuzzleCompleted;
    [SerializeField] private bool shuffleTrayOnReset = true;

    [Header("Completion UI")]
    [SerializeField] private Image completedImage;
    [SerializeField] private Button scanButton;
    [SerializeField] private Text statusText;

    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip incorrectClip;
    [SerializeField] private AudioClip successClip;

    [Header("Events")]
    [SerializeField] private UnityEvent onPuzzleCompleted = new UnityEvent();
    [SerializeField] private UnityEvent onScanRequested = new UnityEvent();

    private readonly HashSet<int> placedPieces = new HashSet<int>();
    private Canvas puzzleCanvas;
    private bool completed;

    public bool IsCompleted => completed;
    public Button ScanButton => scanButton;
    public UnityEvent OnPuzzleCompleted => onPuzzleCompleted;
    public UnityEvent OnScanRequested => onScanRequested;

    private void Awake()
    {
        puzzleCanvas = GetComponentInParent<Canvas>();
        if (scanButton != null)
            scanButton.onClick.AddListener(RequestScan);
    }

    private void OnEnable()
    {
        ResetPuzzle();
    }

    private void OnDestroy()
    {
        if (scanButton != null)
            scanButton.onClick.RemoveListener(RequestScan);
    }

    public void TryPlace(DragHandler piece, Vector2 screenPosition, Camera eventCamera)
    {
        if (completed || piece == null || piece.IsPlaced)
            return;

        int index = piece.PieceId;
        if (index < 0 || index >= Answers.Count || Answers[index] == null)
        {
            piece.RejectDrop();
            return;
        }

        RectTransform answer = Answers[index] as RectTransform;
        Camera cameraToUse = puzzleCanvas != null && puzzleCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? puzzleCanvas.worldCamera
            : eventCamera;

        if (answer != null && RectTransformUtility.RectangleContainsScreenPoint(answer, screenPosition, cameraToUse))
        {
            piece.SnapTo(answer);
            placedPieces.Add(index);
            if (statusText != null)
                statusText.text = $"ASSEMBLY  {placedPieces.Count}/12";

            if (placedPieces.Count >= Pictures.Count && Pictures.Count == Answers.Count)
                CompletePuzzle();
            return;
        }

        PlayClip(incorrectClip);
        piece.RejectDrop();
    }

    public void ResetPuzzle()
    {
        placedPieces.Clear();
        completed = false;

        foreach (Transform picture in Pictures)
        {
            if (picture != null && picture.TryGetComponent(out DragHandler drag))
                drag.ResetPiece();
        }

        if (shuffleTrayOnReset)
            ShuffleTrayPositions();

        if (completedImage != null)
            completedImage.gameObject.SetActive(false);
        if (scanButton != null)
            scanButton.gameObject.SetActive(false);
        if (statusText != null)
            statusText.text = "RECONSTRUCT THE IMAGE  0/12";
    }

    private void ShuffleTrayPositions()
    {
        List<DragHandler> pieces = new List<DragHandler>();
        List<Vector2> positions = new List<Vector2>();

        foreach (Transform picture in Pictures)
        {
            if (picture == null || !picture.TryGetComponent(out DragHandler drag))
                continue;

            pieces.Add(drag);
            positions.Add(((RectTransform)picture).anchoredPosition);
        }

        for (int i = positions.Count - 1; i > 0; i--)
        {
            int other = Random.Range(0, i + 1);
            (positions[i], positions[other]) = (positions[other], positions[i]);
        }

        for (int i = 0; i < pieces.Count; i++)
            pieces[i].SetHomePosition(positions[i]);
    }

    public void SetScanAvailable(bool value)
    {
        if (scanButton != null)
        {
            scanButton.gameObject.SetActive(value);
            scanButton.interactable = value;
        }
    }

    public void SetStatus(string value)
    {
        if (statusText != null)
            statusText.text = value;
    }

    private void CompletePuzzle()
    {
        if (completed)
            return;

        completed = true;
        PlayClip(successClip);

        if (completedImage != null)
            completedImage.gameObject.SetActive(true);
        if (statusText != null)
            statusText.text = "IMAGE RECONSTRUCTED";

        GameConditionManager.Instance.SetCondition(completionCondition);
        onPuzzleCompleted?.Invoke();
    }

    private void RequestScan()
    {
        if (!completed)
            return;

        if (scanButton != null)
            scanButton.interactable = false;
        onScanRequested?.Invoke();
    }

    private void PlayClip(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
            sfxSource.PlayOneShot(clip);
    }
}
