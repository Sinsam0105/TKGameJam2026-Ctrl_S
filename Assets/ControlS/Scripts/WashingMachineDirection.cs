using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [2단계] 세탁기 납량 연출.
/// 전자레인지 확인 -> 베란다 위치에서 세탁기 완료음 3D 재생
/// 세탁기 문을 닫으면 디스플레이 03:05 -> 0.5초 후 끄기 -> 컴퓨터 알림 재생
/// </summary>
public class WashingMachineDirection : MonoBehaviour
{
    /// <summary>
    /// 이벤트를 이미 봤는가 (중복 방지)
    /// </summary>
    public bool Event_40_Played { get; set; } = false;  // TODO: 임의로 false 해뒀다. 다음에 게임 데이터 로드할 때 불러오도록 변경할 듯

    [SerializeField] Text _display;   // TODO: TMP로 변경
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _finishClip;   // 세탁기 완료음

    void Awake()
    {
        Init();
    }

    void Init()
    {
        _display ??= transform.GetComponentInChildren<Text>();
        _display.text = "";

        _audioSource ??= transform.GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.loop = false;
        _audioSource.spatialBlend = 1f; // 3D
        _audioSource.dopplerLevel = 0f; // 음원과 플레이어가 빠르게 가까워지거나 멀어질 때 음높이가 변하는 도플러 효과의 강도 => 고정 물체라서 0
        _audioSource.rolloffMode = AudioRolloffMode.Logarithmic;    // 가까이서는 크고 멀어지면 감소

        // 이건 소리 테스트 해봐야 알듯
        _audioSource.minDistance = 1f;  // Min Distance 밖부터 음량 감소
        _audioSource.maxDistance = 14f;  // maxDistance부터 음량 0 (방 가로가 약 15유닛)
    }

    /// <summary>
    /// 플레이어가 전자레인지를 확인했다. => 베란다 위치에서 세탁기 완료음 3D 재생
    /// </summary>
    public void OnMicrowaveOpened()
    {
        if (_finishClip == null)
            return;

        // 효과음은 일괄 SoundManager로 보내되, 베란다 위치의 3D 감쇠(최대 14유닛)를 그대로 유지한다.
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySfxAt(_finishClip, transform.position, 1f, 1f, 14f);
        else if (_audioSource != null)
            _audioSource.PlayOneShot(_finishClip);
    }

    /// <summary>
    /// 플레이어가 세탁기 문을 닫았다. => 0.5초 동안 03:05 표시.
    /// StageTwoFlowController가 세탁기 정면샷의 "문 닫기" 액션에서 호출한다.
    /// </summary>
    public void OnClosed()
    {
        if (Event_40_Played)
            return;

        Event_40_Played = true;
        StartCoroutine(CoFlickerDisplay());
    }

    IEnumerator CoFlickerDisplay()
    {
        // 세탁기 전원은 꺼진 상태 유지
        _display.text = "03:05";
        yield return new WaitForSeconds(0.5f);
        _display.text = "";
        // TODO: 컴퓨터 알림 재생
    }
}