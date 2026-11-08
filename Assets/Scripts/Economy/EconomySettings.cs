using System;
using UnityEngine;

namespace CastleGuard.Economy
{
    [CreateAssetMenu(menuName = "Castle Guard/Economy Settings")]
    public class EconomySettings : ScriptableObject
    {
        [SerializeField, Min(1)] private int hintPrice = 5;

        public int HintPrice => hintPrice;

        public void Validate()
        {
            if (hintPrice <= 0)
            {
                throw new InvalidOperationException("Hint price must be positive.");
            }
        }

        private void OnValidate()
        {
            try
            {
                Validate();
            }
            catch (InvalidOperationException exception)
            {
                Debug.LogError($"{name}: {exception.Message}", this);
            }
        }
    }
}
