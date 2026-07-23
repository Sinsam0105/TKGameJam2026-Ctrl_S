using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 진행률에 따라 화장실 연출이 바뀐다. 40% 미만은 아무 연출 없다
/// </summary>
public class BathroomDirection : MonoBehaviour
{
    [SerializeField] private Image _cutscene;

    private Coroutine _coPlaySound;

    private void Awake()
    {
        Init();
    }

    void Init()
    {
        _cutscene ??= GameObject.Find("Cutscene").GetComponent<Image>();
        _cutscene.gameObject.SetActive(false);
    }

    // 혹시 몰라서 해둠
    private void OnDisable()
    {
        // 진행률 40%에서 물 떨어지는 소리 끄기
        if (_coPlaySound != null)
        {
            StopCoroutine(_coPlaySound);
            _coPlaySound = null;
        }

        _cutscene.gameObject.SetActive(false);
    }

    public void OnEnteredBathroom()
    {
        if (GameConditionManager.Instance.GameProgress == 40f)
        {
            // 수도꼭지에서 정확히 5초 간격으로 물이 떨어짐
            _cutscene.gameObject.SetActive(true);
            _cutscene = Resources.Load<Image>("Arts/CondensedFaucet");
            StartCoroutine(CoPlaySound(5f));
        }
        else if (GameConditionManager.Instance.GameProgress == 60f)
        {
            // 김 서린 거울에 손자국 나타남. 닦은 뒤 돌아보면 다시 생김
            // TODO: 팀장팀장이가 해줘
        }
        else if (GameConditionManager.Instance.GameProgress == 80f)
        {
            // 김 서린 거울에 손자국 나타남. 닦은 뒤 돌아보면 다시 생김
            // TODO: 팀장팀장이가 해줘
        }
    }

    public void StopSound()
    {
        // 진행률 40%에서 물 떨어지는 소리 끄기
        if (_coPlaySound != null)
        {
            StopCoroutine(_coPlaySound);
            _coPlaySound = null;
        }
    }

    IEnumerator CoPlaySound(float interval)
    {
        while (true)
        {
            // TODO: 팀장님 사운드 재생 좀
            yield return new WaitForSeconds(interval);
        }
    }
}
