using CastleGuard.App;
using TMPro;
using UnityEngine;

namespace CastleGuard.Progress
{
    public sealed class ContinueLevelView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;

        private void OnEnable()
        {
            label.text = $"Продолжить с уровня {GameSave.Progress.LevelNumber}";
        }
    }
}
