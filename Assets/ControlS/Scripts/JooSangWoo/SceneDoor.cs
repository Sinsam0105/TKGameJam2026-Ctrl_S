using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// 화장실처럼 겹쳐 로드하는 씬을 드나드는 문. 트리거 안에서 E 키로 작동한다.
///
/// 진행 상태를 유지하려고 방 씬을 언로드하지 않는다. 대신 화장실 씬을 Additive로 겹쳐 올리고
/// 방 씬의 카메라/플레이어만 끈다. 돌아올 때는 화장실 씬만 언로드하고 방을 다시 켠다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public sealed class SceneDoor : MonoBehaviour
{
    public enum DoorMode
    {
        OpenOverlay,   // 대상 씬을 겹쳐 올리고 이 씬(방)의 뷰를 끈다
        CloseOverlay,  // 이 씬(화장실)을 언로드하고 아래 씬(방)의 뷰를 켠다
    }

    [SerializeField] private DoorMode mode = DoorMode.OpenOverlay;
    [SerializeField] private string targetScene = "Bathroom"; // OpenOverlay일 때만 사용
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private GameObject promptObject;

    private bool playerInRange;
    private bool transitioning;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        SetPrompt(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsPlayer(other))
        {
            playerInRange = true;
            SetPrompt(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (IsPlayer(other))
        {
            playerInRange = false;
            SetPrompt(false);
        }
    }

    private void Update()
    {
        if (!playerInRange || transitioning)
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
            StartCoroutine(Enter());
    }

    private IEnumerator Enter()
    {
        transitioning = true;
        SetPrompt(false);

        if (mode == DoorMode.OpenOverlay)
        {
            Scene here = gameObject.scene;
            AsyncOperation load = SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Additive);
            yield return load;

            Scene target = SceneManager.GetSceneByName(targetScene);
            if (target.IsValid())
            {
                SceneManager.SetActiveScene(target);
                SetViewActive(here, false); // 방 뷰 끄기 (매니저는 살려둔다)
            }
        }
        else // CloseOverlay
        {
            Scene here = gameObject.scene;
            // 아래에 남아 있는 다른 로드된 씬(방)을 다시 켠다.
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene other = SceneManager.GetSceneAt(i);
                if (other != here && other.isLoaded)
                {
                    SceneManager.SetActiveScene(other);
                    SetViewActive(other, true);
                    break;
                }
            }
            yield return SceneManager.UnloadSceneAsync(here);
        }

        transitioning = false;
    }

    // 씬 안의 카메라/오디오리스너/플레이어를 켜고 끈다. 매니저 등 나머지는 건드리지 않는다.
    private static void SetViewActive(Scene scene, bool active)
    {
        if (!scene.IsValid())
            return;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Camera cam in root.GetComponentsInChildren<Camera>(true))
                cam.enabled = active;
            foreach (AudioListener listener in root.GetComponentsInChildren<AudioListener>(true))
                listener.enabled = active;
        }

        // 플레이어는 오브젝트째로 켜고 끈다. (비활성 상태도 찾도록 includeInactive)
        GameObject player = scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
            .Select(t => t.gameObject)
            .FirstOrDefault(g => g.CompareTag("Player"));
        if (player != null)
            player.SetActive(active);
    }

    private void SetPrompt(bool value)
    {
        if (promptObject != null)
            promptObject.SetActive(value);
    }

    private bool IsPlayer(Collider2D other)
    {
        if (other.CompareTag(playerTag))
            return true;
        Rigidbody2D body = other.attachedRigidbody;
        return body != null && body.CompareTag(playerTag);
    }

#if UNITY_EDITOR
    public void EditorConfigure(DoorMode doorMode, string scene)
    {
        mode = doorMode;
        targetScene = scene;
    }
#endif
}
