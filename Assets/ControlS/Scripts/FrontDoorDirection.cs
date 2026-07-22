using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 이 코드도 연출만 있어서 그냥 상호작용 스크립트로 옮기면 될 것 같다
/// </summary>
public class FrontDoorDirection : MonoBehaviour
{
    /// <summary>
    /// 이벤트를 이미 봤는가 (중복 방지)
    /// </summary>
    public bool HasSeenEvent { get; set; } = false;  // TODO: 임의로 false 해뒀다. 다음에 게임 데이터 로드할 때 불러오도록 변경할 듯

    [SerializeField] string _narrationScriptName;   // TODO: 스크립트 매니저를 통해 말풍선이 끝날 때마다 이벤트 활성화
    [SerializeField] Transform _intercomScreen;

    /// <summary>
    /// 픽셀 = _noiseResolution * _noiseResolution 
    /// </summary>
    [SerializeField] int _noiseResolution = 32;
    /// <summary>
    /// 간격 = 1/_noiseFps  => 낮을수록 뚝뚝 끊기는 아날로그 느낌
    /// </summary>
    [SerializeField] float _noiseFps = 12f;
    [SerializeField, Range(0f, 1f)] float _spotChance = 0.08f;   // 밝은 점이 섞일 확률 (나머지는 검은색)

    [SerializeField] RawImage _intercomImage;
    Texture2D _noiseTexture;
    Coroutine _coPlayNoise;

    void Awake()
    {
        Init();
    }

    void Init()
    {
        _intercomScreen ??= transform.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "IntercomScreen");
        _intercomImage ??= _intercomScreen.GetComponent<RawImage>();
        _noiseTexture = new Texture2D(_noiseResolution, _noiseResolution, TextureFormat.RGBA32, false);
        _noiseTexture.filterMode = FilterMode.Point;   // 부드럽게 뭉개지지 않고 도트 노이즈처럼 보이게
        _noiseTexture.wrapMode = TextureWrapMode.Clamp;

        StopNoise();
        OnIntercomInteracted(); // test
    }

    /// <summary>
    /// Project Structure Restored 문구가 사라지고 다음 단계로 넘어가기 직전
    /// TODO: 상호작용 스크립트랑 연결 필요
    /// </summary>
    public void Onanjtlrl() // 이름 뭘로 하지..........
    {
        if (HasSeenEvent)
            return;

        HasSeenEvent = true;

        // TODO: 버전 체인 검증음과 같은 세 번의 강한 타격음이 울린다
        //ScriptManager.Instance.Play(_investigationScriptName);    // TODO: 대본이 없다

        // TODO: 짧은 정적 뒤 같은 문에서 세 번 더 두드리는 소리가 난다. 마지막 타격음은 비정상적으로 길게 울린다.
    }

    /// <summary>
    /// 플레이어가 인터폰과 상호작용 할 때, 검은 노이즈만 표시
    /// TODO: 상호작용 스크립트랑 연결 필요
    /// </summary>
    void OnIntercomInteracted()
    {
        // 도어뷰 또는 간단한 인터폰 화면은 검은 노이즈만 표시
        _intercomScreen.gameObject.SetActive(true);
        _intercomImage.texture = _noiseTexture;
        _coPlayNoise = StartCoroutine(CoPlaynNoise());
    }

    /// <summary>
    /// 현관문을 닫으면 컴퓨터에서 정상적인 Workspace Recovery 알림음이 울린다
    /// TODO: 상호작용 스크립트 연결
    /// </summary>
    void OnClosed()
    {
        // TODO: 현관문을 닫으면 컴퓨터에서 정상적인 Workspace Recovery 알림음이 울린다
    }

    void StopNoise()
    {
        _intercomScreen.gameObject.SetActive(false);
        if (_coPlayNoise != null)
            StopCoroutine(CoPlaynNoise());
        _coPlayNoise = null;
    }

    IEnumerator CoPlaynNoise()
    {
        float interval = 1f / _noiseFps;
        // TODO: 노이즈 소리 재생 (지지직)

        // 대부분 검은색이며 가끔 밝은 점이 섞이는 노이즈
        while (true)
        {
            Color32[] pixels = new Color32[_noiseResolution * _noiseResolution];
            for (int i = 0; i < pixels.Length; i++)
            {
                // 밝은 점이 될지 검은색에 가까운 점이 될지 결정
                // 밝은 픽셀의 밝기 : 어두운 픽셀의 밝기
                byte v = (byte)(Random.value < _spotChance ? Random.Range(120, 255) : Random.Range(0, 20));
                pixels[i] = new Color32(v, v, v, 255);
            }

            _noiseTexture.SetPixels32(pixels);
            _noiseTexture.Apply(false);
            yield return new WaitForSeconds(interval);
        }
    }
}
