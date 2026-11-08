using UnityEngine;
using UnityEngine.SceneManagement;

namespace CastleGuard.App
{
    public class GameNavigation : MonoBehaviour
    {
        public void Play()
        {
            SceneManager.LoadScene("Level");
        }

        public void ReturnToMenu()
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}
