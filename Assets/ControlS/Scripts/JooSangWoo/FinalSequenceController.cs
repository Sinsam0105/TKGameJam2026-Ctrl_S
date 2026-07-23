using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using ScriptData;

/// <summary>
/// 엔딩: 최종 저장 시퀀스(D054~D064).
/// 5단계(User Verification, 100%) 이후 진행한다.
/// 끝났다 → 키가 없다 → Ctrl 찾기 → S 찾기 → 저장 실행 → 저장 진행(43/67/84/96%) → 트위스트.
/// 키캡/키 오브젝트 아트는 없으므로 "키 찾기"는 System 안내 + 실제 키 입력으로 대체한다.
/// </summary>
[DisallowMultipleComponent]
public sealed class FinalSequenceController : MonoBehaviour
{
    [Header("Save Screen")]
    [SerializeField] private GameObject saveScreen;        // 저장 진행 화면(캔버스/패널)
    [SerializeField] private Text saveProgressText;        // "SAVING...  43%"
    [SerializeField] private Image saveProgressFill;       // 진행 바(Filled). 없어도 됨
    [SerializeField] private GameObject fullScreenGlitch;  // 트위스트 글리치 연출

    [Header("Timing / Audio")]
    [SerializeField, Min(1f)] private float saveDuration = 12f;
    [SerializeField] private AudioClip saveHumClip;        // 저장 중 루프음(선택)

    [Header("Script Ids")]
    [SerializeField] private string startId = "D054FinalStart";
    [SerializeField] private string keysMissingId = "D055FinalKeysMissing";
    [SerializeField] private string ctrlId = "D056FinalCtrl";
    [SerializeField] private string sId = "D057FinalS";
    [SerializeField] private string pressId = "D059FinalPress";
    [SerializeField] private string saveStartId = "D060SaveStart";

    private static readonly (float pct, string id)[] SaveBeats =
    {
        (0.43f, "D061Save43"),
        (0.67f, "D062Save67"),
        (0.84f, "D063Save84"),
        (0.96f, "D064Save96"),
    };

    private bool started;

    /// <summary>5단계 완료 후 StageFive가 호출한다.</summary>
    public void BeginEnding()
    {
        if (started)
            return;
        started = true;
        StartCoroutine(EndingRoutine());
    }

    private IEnumerator EndingRoutine()
    {
        GameStateManager.Instance?.SetState(GameState.UI);
        SetActive(saveScreen, false);
        SetActive(fullScreenGlitch, false);

        yield return PlayScript(startId);          // 드디어 끝났네 / 저장만 하면
        yield return PlayScript(keysMissingId);    // 키가 왜 없어?

        // Ctrl 키 찾아 누르기
        SpeechBubbleController.ShowSystem("Ctrl 키를 찾아 누른다.");
        yield return WaitForKey(k => k.leftCtrlKey.wasPressedThisFrame || k.rightCtrlKey.wasPressedThisFrame);
        CloseSystem();
        yield return PlayScript(ctrlId);

        // S 키 찾아 누르기
        SpeechBubbleController.ShowSystem("S 키를 찾아 누른다.");
        yield return WaitForKey(k => k.sKey.wasPressedThisFrame);
        CloseSystem();
        yield return PlayScript(sId);

        yield return PlayScript(pressId);          // 이거 누르면 진짜 끝

        // Ctrl + S 로 저장 실행
        SpeechBubbleController.ShowSystem("Ctrl + S 로 저장한다.");
        yield return WaitForKey(k => (k.leftCtrlKey.isPressed || k.rightCtrlKey.isPressed) && k.sKey.wasPressedThisFrame);
        CloseSystem();

        // 저장 진행
        SetActive(saveScreen, true);
        yield return PlayScript(saveStartId);      // 제발 이번엔 제대로
        yield return SaveProgress();

        // 트위스트: 96%에서 화면 글리치 후 컷.
        SetActive(fullScreenGlitch, true);
        yield return new WaitForSecondsRealtime(2f);
    }

    private IEnumerator SaveProgress()
    {
        int beatIndex = 0;
        float t = 0f;
        int loopId = (saveHumClip != null && SoundManager.Instance != null)
            ? SoundManager.Instance.PlayLoop(saveHumClip, 0.6f)
            : 0;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.1f, saveDuration);
            float pct = Mathf.Clamp01(t);

            if (saveProgressFill != null)
                saveProgressFill.fillAmount = pct;
            if (saveProgressText != null)
                saveProgressText.text = $"SAVING...  {Mathf.RoundToInt(pct * 100f)}%";

            if (beatIndex < SaveBeats.Length && pct >= SaveBeats[beatIndex].pct)
            {
                ScriptManager.Instance?.Play(SaveBeats[beatIndex].id);
                beatIndex++;
            }

            yield return null;
        }

        if (loopId != 0)
            SoundManager.Instance?.StopLoop(loopId);
    }

    private static IEnumerator WaitForKey(Func<Keyboard, bool> predicate)
    {
        while (true)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && predicate(keyboard))
                yield break;
            yield return null;
        }
    }

    private static void CloseSystem()
    {
        SpeechBubbleController.Get(EObject.System)?.ForceClose();
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

    private static void SetActive(GameObject target, bool value)
    {
        if (target != null && target.activeSelf != value)
            target.SetActive(value);
    }
}
