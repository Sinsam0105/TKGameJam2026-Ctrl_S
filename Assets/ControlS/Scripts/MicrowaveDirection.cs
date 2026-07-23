using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [2단계] 정답 입력 후 재생되는 전자레인지 납량 연출.
/// 대기 -> 버튼음 -> 조명 켜짐 -> 00:30초 동안 회전판 회전 -> 주인공 대사
/// 플레이어가 전자레인지 문을 열면 즉시 멈춘다 -> 빈 내부를 확인한 조사 대사
/// </summary>
public class MicrowaveDirection : MonoBehaviour
{
    /// <summary>
    /// 이벤트를 이미 봤는가 (중복 방지)
    /// </summary>
    public bool Event_40_Played { get; set; } = false;  // TODO: 임의로 false 해뒀다. 다음에 게임 데이터 로드할 때 불러오도록 변경할 듯

    [SerializeField] LightController _light;
    [SerializeField] TMP_Text _display;  // TODO: TMP
    [SerializeField] Transform _turntable;
    Vector3 _originalTurntableScale;

    [SerializeField] string _narrationScriptName;      // 주인공 대사 (돌아가는 동안)
    [SerializeField] string _investigationScriptName;  // 조사 대사 (문 열어서 빈 걸 확인한 뒤)

    Coroutine _coPlay;
    int _runningLoopId;   // SoundManager 루프 핸들 (작동음)

    private void Awake()
    {
        Init();
    }

    void Init()
    {
        _light ??= transform.GetComponentInChildren<LightController>();
        _light.Init();
        _light.TurnOff();

        _display ??= transform.GetComponentInChildren<TMP_Text>();
        _turntable = transform.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Turntable");
        _originalTurntableScale = _turntable.localScale;
    }

    /// <summary>
    /// 정답을 입력했다. => 전자레인지 버튼음 재생 / 불 켜짐 / 30초 타이머 동안 회전판 회전 후 연출 비활성화
    /// </summary>
    public void OnAnswerValidated()
    {
        if (Event_40_Played || _coPlay != null)
            return;

        _coPlay = StartCoroutine(CoPlay());
    }

    IEnumerator CoPlay()
    {
        //GameObject player = GameObject.FindGameObjectWithTag("Player");
        //Vector3 dir = transform.position - player.transform.position;
        //dir.y = 0f;
        //float dot = Vector3.Dot(player.transform.forward, dir);

        //// 플레이어 앞에 전자레인지가 있으면 버튼음을 먼저 재생한 뒤 0.3초 후 작동
        //yield return (dir.magnitude <= 1.5f && dot > 0.8f ? new WaitForSeconds(0.3f) : new WaitForSeconds(2f));

        yield return new WaitForSeconds(2f);

        // 효과음은 일괄 SoundManager로 보내되, 없으면 로컬 소스로 대체한다.
        if (_buttonClip != null)
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySfxAt(_buttonClip, transform.position, 1f, 1f, 14f);
            else if (_audioSource != null)
                _audioSource.PlayOneShot(_buttonClip);
        }

        _light.Intensity = 3f;
        _light.TurnOn();
        _display.text = "00:30";

        //ScriptManager.Instance.Play(_narrationScriptName);    // TODO: 대본이 없다

        // 작동음은 30초 내내 이어지므로 루프로 돌린다. SoundManager 루프 핸들로 관리한다.
        if (_runningClip != null)
        {
            if (SoundManager.Instance != null)
                _runningLoopId = SoundManager.Instance.PlayLoop(_runningClip, 1f, transform.position, 1f, 14f);
            else if (_audioSource != null)
            {
                _audioSource.clip = _runningClip;
                _audioSource.loop = true;
                _audioSource.Play();
            }
        }

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

        //ScriptManager.Instance.Play(_investigationScriptName);    // TODO: 대본이 없다

        _turntable.localScale = _originalTurntableScale;
        enabled = false;
    }

    // 작동음 루프를 멈춘다. 30초를 다 채우거나 플레이어가 문을 열면 호출된다.
    void StopRunningSound()
    {
        // SoundManager 루프 핸들 정리
        if (_runningLoopId != 0)
        {
            SoundManager.Instance?.StopLoop(_runningLoopId);
            _runningLoopId = 0;
        }

        // 로컬 소스 대체 경로 정리
        if (_audioSource == null || _audioSource.clip != _runningClip)
            return;

        _audioSource.Stop();
        _audioSource.loop = false;
        _audioSource.clip = null;
    }

    /// <summary>
    /// 플레이어가 전자레인지 문을 열었다 -> 회전 멈춤 / 전자레인지 연출 비활성화
    /// </summary>
    public void OnOpened()
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
