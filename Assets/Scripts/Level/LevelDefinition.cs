using System;
using System.Collections.Generic;
using UnityEngine;

namespace CastleGuard.Level
{
    [CreateAssetMenu(menuName = "Castle Guard/Level")]
    public class LevelDefinition : ScriptableObject
    {
        [Serializable]
        public struct Tower
        {
            public string id;
            public Vector2 position;
        }

        [Serializable]
        public struct Wall
        {
            public string from;
            public string to;
        }

        [SerializeField, Min(0)] private int baseReward = 10;
        [SerializeField] private Tower[] towers;
        [SerializeField] private Wall[] walls;
        [SerializeField] private string startingTower;

        public IReadOnlyList<Tower> Towers => towers;
        public IReadOnlyList<Wall> Walls => walls;
        public string StartingTower => startingTower;
        public int BaseReward => baseReward;

        public void ValidateEconomy()
        {
            if (baseReward < 0)
            {
                throw new InvalidOperationException("Base reward must be nonnegative.");
            }

            long maximumReward = (long)baseReward * 2;
            if (maximumReward > int.MaxValue)
            {
                throw new InvalidOperationException("Maximum level reward exceeds the coin balance limit.");
            }
        }

        private void OnValidate()
        {
            try
            {
                ValidateEconomy();
            }
            catch (InvalidOperationException exception)
            {
                Debug.LogError($"{name}: {exception.Message}", this);
            }
        }
    }
}
