using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 타이틀 화면. 시작 버튼으로 본편 씬을 로드하고, 종료 버튼으로 게임을 끝낸다.
/// </summary>
[DisallowMultipleComponent]
public sealed class TitleScreen : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "SampleScene";
    [SerializeField] private Button startButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private AudioSource bgmSource;

    private bool loading;

    private void Awake()
    {
        if (startButton != null)
            startButton.onClick.AddListener(StartGame);
        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
    }

    private void OnDestroy()
    {
        if (startButton != null)
            startButton.onClick.RemoveListener(StartGame);
        if (quitButton != null)
            quitButton.onClick.RemoveListener(QuitGame);
    }

    public void StartGame()
    {
        if (loading)
            return;
        loading = true;
        SceneManager.LoadScene(gameSceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
