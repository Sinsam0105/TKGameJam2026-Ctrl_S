using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [3단계] 현관문 연출.
/// </summary>
public class FrontDoorDirection : MonoBehaviour
{
    /// <summary>
    /// 이벤트를 이미 봤는가 (중복 방지)
    /// </summary>
    public bool Event_60_Played { get; set; } = false;  // TODO: 임의로 false 해뒀다. 다음에 게임 데이터 로드할 때 불러오도록 변경할 듯

    [SerializeField] string _narrationScriptName;   // TODO: 스크립트 매니저를 통해 말풍선이 끝날 때마다 이벤트 활성화
    [SerializeField] Transform _intercomScreen;
    bool _isViewingIntercom = false;

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

    [Header("현관 배경 (정상/공포/복도)")]
    [SerializeField] SpriteRenderer _doorRenderer;   // 현관 정면 배경
    [SerializeField] Sprite _normalSprite;           // 문 닫힘 (노크 시)
    [SerializeField] Sprite _horrorSprite;           // 문 열림 글리치 (개방 순간)
    [SerializeField] Sprite _corridorSprite;         // 열린 뒤 빈 복도

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

        // 기본은 문 닫힌 현관 정면.
        if (_doorRenderer != null && _normalSprite != null)
            _doorRenderer.sprite = _normalSprite;

        StopNoise();
        //OnIntercomInteracted(); // test
    }

    /// <summary>
    /// 플레이어가 현관문을 열었다 => 잠깐 글리치 후 빈 복도. (기획 29.6: 아무도 없고 센서등만)
    /// </summary>
    public void OnDoorOpened()
    {
        StartCoroutine(CoOpenDoor());
    }

    IEnumerator CoOpenDoor()
    {
        if (_doorRenderer != null && _horrorSprite != null)
        {
            _doorRenderer.sprite = _horrorSprite;   // 개방 순간 글리치
            yield return new WaitForSeconds(0.4f);
        }

        if (_doorRenderer != null && _corridorSprite != null)
            _doorRenderer.sprite = _corridorSprite; // 아무도 없는 복도

        // TODO: 2초 후 복도 끝 엘리베이터 도착음 "딩—"
    }

    /// <summary>
    /// Project Structure Restored 문구가 사라지고 다음 단계로 넘어가기 직전에 호출된다
    /// </summary>
    public void OnProjectStructureRestoredMessageFinished() // 이름 뭘로 하지..........
    {
        if (Event_60_Played)
            return;

        Event_60_Played = true;

        // TODO: 버전 체인 검증음과 같은 세 번의 강한 타격음이 울린다

        //ScriptManager.Instance.Play(_investigationScriptName);    // TODO: 대본이 없다

        // TODO: 짧은 정적 뒤 같은 문에서 세 번 더 두드리는 소리가 난다. 마지막 타격음은 비정상적으로 길게 울린다.
    }

    /// <summary>
    /// 플레이어가 인터폰을 만졌다 => 인터폰을 볼 때는 검은 노이즈만 표시 / 이미 보고 있는 상태라면 인터폰 끄기
    /// </summary>
    public void OnIntercomInteracted()
    {
        if (_isViewingIntercom)
        {
            _isViewingIntercom = false;
            StopNoise();
            _intercomScreen.gameObject.SetActive(false);
            return;
        }

        _isViewingIntercom = true;
        _intercomScreen.gameObject.SetActive(true);
        _intercomImage.texture = _noiseTexture;
        _coPlayNoise = StartCoroutine(CoPlaynNoise());
    }

    /// <summary>
    /// 현관문을 닫으면 컴퓨터에서 정상적인 Workspace Recovery 알림음이 울린다
    /// TODO: 상호작용 스크립트 연결
    /// </summary>
    public void OnClosed()
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
