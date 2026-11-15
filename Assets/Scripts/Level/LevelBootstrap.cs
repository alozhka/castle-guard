using System;
using System.Linq;
using CastleGuard.App;
using CastleGuard.Economy;
using CastleGuard.Level.Generation;
using CastleGuard.Level.Model;
using CastleGuard.Level.View;
using CastleGuard.Level.ViewModel;
using UnityEngine;

namespace CastleGuard.Level
{
    public sealed class LevelBootstrap : MonoBehaviour
    {
        [SerializeField] private LevelDefinition[] levels;
        [SerializeField] private LevelDefinition generatedLevel;
        [SerializeField] private EconomySettings economySettings;
        [SerializeField] private LevelView levelView;

        private LevelDefinition _currentLevel;
        private LevelDefinition _runtimeLevel;
        private int _currentIndex = -1;
        private LevelModel _model;

        private void Awake()
        {
            int savedIndex = GameSave.Progress.LevelNumber - 1;
            int initialIndex = generatedLevel == null ? Math.Min(savedIndex, levels.Length - 1) : savedIndex;
            _model = new LevelModel(
                CreateAttempt, GameSave.Wallet, GameSave.Progress.CompleteLevel,
                economySettings.HintPrice, initialIndex);
            levelView.Initialize(new LevelViewModel(_model, () => _currentLevel));
        }

        private LevelAttempt CreateAttempt(int index)
        {
            if (_currentIndex != index)
            {
                _currentLevel = index < levels.Length ? levels[index] : CreateGeneratedLevel(index);
                _currentLevel.ValidateEconomy();
                _currentIndex = index;
            }

            return new LevelAttempt(_currentLevel.StartingTower,
                _currentLevel.Walls.Select(wall => new Wall(wall.from, wall.to)), _currentLevel.BaseReward);
        }

        private LevelDefinition CreateGeneratedLevel(int index)
        {
            if (_runtimeLevel != null)
            {
                Destroy(_runtimeLevel);
            }

            GeneratedLevel data = new LevelGenerator().Generate($"Уровень {index + 1}");
            _runtimeLevel = LevelDefinition.Create(data, generatedLevel.BaseReward);
            return _runtimeLevel;
        }

        private void OnDestroy()
        {
            _model?.Dispose();
            if (_runtimeLevel != null)
            {
                Destroy(_runtimeLevel);
            }
        }
    }
}
