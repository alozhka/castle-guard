using System.Collections.Generic;
using CastleGuard.Economy;
using CastleGuard.Level.Model.Definition;
using UnityEngine;

namespace CastleGuard.Model
{
    [CreateAssetMenu(menuName = "Castle Guard/Game Settings")]
    public sealed class GameSettings : ScriptableObject
    {
        [SerializeField] private LevelDefinition[] levels;
        [SerializeField] private LevelDefinition generatedLevel;
        [SerializeField] private EconomySettings economy;

        public IReadOnlyList<LevelDefinition> Levels => levels;
        public LevelDefinition GeneratedLevel => generatedLevel;
        public EconomySettings Economy => economy;
    }
}
