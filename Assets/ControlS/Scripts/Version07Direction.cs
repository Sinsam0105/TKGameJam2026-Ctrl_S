using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// D, B, F, A, E, C가 시간순으로 빠르게 재생된 뒤 목록에 없던 손상 버전이 0.5초 나타난다.
/// </summary>
public class Version07Direction : MonoBehaviour
{
    /// <summary>
    /// 이벤트를 이미 봤는가 (중복 방지)
    /// </summary>
    public bool HasSeenEvent { get; set; } = false;  // TODO: 임의로 false 해뒀다. 다음에 게임 데이터 로드할 때 불러오도록 변경할 듯

    [SerializeField] LightController _light;
    [SerializeField] TMP_Text _display;

    [SerializeField] string _narrationScriptName;   // TODO: 스크립트 매니저를 통해 말풍선이 끝날 때마다 이벤트 활성화

    private void Awake()
    {
        Init();
    }

    void Init()
    {
        _light ??= transform.GetComponentInChildren<LightController>();
        _light.Init();
        _light.TurnOn();

        _display ??= transform.GetComponentInChildren<TMP_Text>();
        _display.text = "";
        OnUSBConnected();
    }

    /// <summary>
    /// USB 연결 후, 복원 진행
    /// => D, B, F, A, E, C 카드가 0.25초 간격으로 전체화면 재생
    /// </summary>
    public void OnUSBConnected()
    {
        if (HasSeenEvent)
            return;

        StartCoroutine(CoPlay());
    }

    IEnumerator CoPlay()
    {
        // TODO: 실제 UI에 띄워야 한다
        char[] code = { 'D', 'B', 'F', 'A', 'E', 'C' };
        _display.text += $"{code[0]}";

        for (int i = 1; i < code.Length; ++i)
        {
            yield return new WaitForSeconds(0.25f);
            _display.text += $" -> {code[i]}";
        }

        // TODO: 목록에 없던 손상 버전이 0.5초 나타난다.
        // => 0.5초 동안 표시. 현재 방과 유사하지만 검은 형태가 사람인지 의자인지 판별 불가
        // 기획자님께 허락 받았어요. 재량껏 지속 시간 조정해도 된대요
        Debug.Log("Version07Direction => 손상 버전");

        float elapsedTime = 0f;
        while (elapsedTime < 0.5f)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // TODO: 0.5초 뒤 사라진다
        Debug.Log("Version07Direction => 손상 버전 사라짐");

        //ScriptManager.Instance.Play(_narrationScriptName);    // TODO: 대본이 없다
    }
}
