using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainSceneUI : MonoBehaviour
{
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _optionsButton;
    [SerializeField] private Button _exitButton;

    [SerializeField] private Transform _optionsUI;

    void Awake()
    {
        Init();
    }

    void Init()
    {
        _startButton ??= transform.GetComponentsInChildren<Button>(true).FirstOrDefault(t => t.name == "StartButton");
        _optionsButton ??= transform.GetComponentsInChildren<Button>(true).FirstOrDefault(t => t.name == "OptionsButton");
        _exitButton ??= transform.GetComponentsInChildren<Button>(true).FirstOrDefault(t => t.name == "ExitButton");
        _optionsUI = GameObject.Find("OptionsUI").transform;

        _startButton.onClick.AddListener(OnStartButtonClicked);
        _optionsButton.onClick.AddListener(OnOptionsButtonClicked);
        _exitButton.onClick.AddListener(OnExitButtonClicked);
        _optionsUI.gameObject.SetActive(false);
    }

    void OnStartButtonClicked()
    {
        Debug.Log("OnStartButtonClicked");
        //SceneManager.LoadScene("GameScene");
    }

    void OnOptionsButtonClicked()
    {
        Debug.Log("OnOptionsButtonClicked");
        _optionsUI.gameObject.SetActive(true);
    }

    void OnExitButtonClicked()
    {
        Debug.Log("OnExitButtonClicked");
        Application.Quit();
    }
}
