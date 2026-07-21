using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 정답 입력 후 재생되는 전자레인지 납량 연출. => 나중에 전자레인지 상호작용 스크립트로 다 옮기면 될 것 같다
/// 대기 -> 버튼음 -> 조명 켜짐 -> 00:30초 동안 회전판 회전 -> 주인공 대사
/// 플레이어가 전자레인지 문을 열면 즉시 멈춘다 -> 빈 내부를 확인한 조사 대사
/// </summary>
public class MicrowaveDirection : MonoBehaviour
{
    /// <summary>
    /// 이벤트를 이미 봤는가 (중복 방지)
    /// </summary>
    public bool HasSeenEvent { get; set; } = false;  // TODO: 임의로 false 해뒀다. 다음에 게임 데이터 로드할 때 불러오도록 변경할 듯

    [SerializeField] LightController _light;
    [SerializeField] Text _display;  // TODO: TMP
    [SerializeField] Transform _turntable;
    Vector3 _originalTurntableScale;

    [SerializeField] string _narrationScriptName;      // 주인공 대사 (돌아가는 동안)
    [SerializeField] string _investigationScriptName;  // 조사 대사 (문 열어서 빈 걸 확인한 뒤)

    Coroutine _coPlay;

    private void Awake()
    {
        Init();
        Play();
    }

    void Init()
    {
        _light = transform.GetComponentInChildren<LightController>();
        _light.Init();

        _display = transform.GetComponentInChildren<Text>();
        _turntable = transform.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Turntable");
        _originalTurntableScale = _turntable.localScale;
    }

    void Play()
    {
        if (HasSeenEvent || _coPlay != null)
            return;

        HasSeenEvent = true;
        _coPlay = StartCoroutine(CoPlay());
    }

    IEnumerator CoPlay()
    {
        _light.TurnOff();
        yield return new WaitForSeconds(2f);

        // TODO: 전자레인지 버튼음 재생
        _light.Intensity = 3f;
        _light.TurnOn();
        _display.text = "00:30";

        //ScriptManager.Instance.Play(_narrationScriptName);    // TODO: 대본이 없다

        float speed = 120f;
        float angle = 0f;

        // 30초 동안 회전판 계속 회전
        float elapsedTime = 0f;
        while (elapsedTime < 30f)
        {
            elapsedTime += Time.deltaTime;
            int remaining = Mathf.Max(0, 30 - Mathf.FloorToInt(elapsedTime));
            _display.text = $"00:{remaining:00}";

            angle += speed * Time.deltaTime;
            angle %= 360f;

            // 전자레인지 회전판 연출 이게 맞나..? 이게 옆인지 위에서 내려다보는건지 감이 안온다 (위에서 보는거면 Rotate)
            float squash = (Mathf.Cos(angle * Mathf.Deg2Rad) + 1f) * 0.5f;  // 0 ~ 1
            _turntable.localScale = new Vector3(_originalTurntableScale.x * squash, _originalTurntableScale.y, _originalTurntableScale.z);
            yield return null;
        }

        _turntable.localScale = _originalTurntableScale;
    }

    // 플레이어가 전자레인지 문을 열었다
    // TODO: 전자레인지 상호작용 스크립트에서 이벤트 연결해줘용
    void OnOpened()
    {
        if (_coPlay != null)
        {
            StopCoroutine(_coPlay);
            _coPlay = null;
        }

        _light.TurnOff();
        _display.text = "";
        _turntable.localScale = _originalTurntableScale;

        //ScriptManager.Instance.Play(_investigationScriptName);    // TODO: 대본이 없다

        enabled = false;    // 꺼진다
    }
}
