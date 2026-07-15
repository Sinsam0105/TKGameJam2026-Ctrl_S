using Sinsam.SingletonSystem;
using UnityEngine.SceneManagement;

namespace ControlS
{
    [AutoSingleton]
    public sealed class GameSessionManager : MonoSingleton<GameSessionManager>
    {
        public void RestartCurrentGame()
        {
            ProgressManager.Instance?.ResetProgress();
            var scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0) SceneManager.LoadScene(scene.buildIndex);
            else if (!string.IsNullOrWhiteSpace(scene.path)) SceneManager.LoadScene(scene.path);
        }
    }
}
