using UnityEngine;

namespace CastleGuard.Economy
{
    [CreateAssetMenu(menuName = "Castle Guard/Economy Settings")]
    public class EconomySettings : ScriptableObject
    {
        [SerializeField, Min(1)] private int hintPrice = 5;

        public int HintPrice => hintPrice;
    }
}
