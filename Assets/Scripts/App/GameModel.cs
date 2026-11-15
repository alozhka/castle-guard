using System;
using System.Linq;
using CastleGuard.Level;
using CastleGuard.Level.Generation;
using CastleGuard.Level.Model;
using CastleGuard.Progress;
using UnityEngine;

namespace CastleGuard.App;

public sealed class GameModel : IDisposable
{
    private readonly LevelGenerator _generator = new();
    private readonly GameSettings _settings;
    private LevelDefinition _runtimeLevel;
    private int _currentIndex = -1;

    public PlayerProgress Progress { get; }
    public LevelModel Level { get; private set; }
    public LevelDefinition CurrentLevel { get; private set; }

    public GameModel(GameSettings settings, PlayerProgress progress)
    {
        _settings = settings;
        Progress = progress;
    }

    public LevelModel StartLevel()
    {
        ClearLevel();
        int savedIndex = Progress.LevelNumber - 1;
        int initialIndex = _settings.GeneratedLevel == null
            ? Math.Min(savedIndex, _settings.Levels.Count - 1)
            : savedIndex;
        Level = new LevelModel(CreateAttempt, Progress.Wallet, Progress.CompleteLevel,
            _settings.Economy.HintPrice, initialIndex);
        return Level;
    }

    public void EndLevel(LevelModel level)
    {
        if (ReferenceEquals(Level, level))
        {
            ClearLevel();
        }
    }

    public void Dispose() => ClearLevel();

    private void ClearLevel()
    {
        Level?.Dispose();
        Level = null;
        CurrentLevel = null;
        _currentIndex = -1;
        ReleaseGeneratedLevel();
    }

    private LevelAttempt CreateAttempt(int index)
    {
        if (_currentIndex != index)
        {
            CurrentLevel = index < _settings.Levels.Count ? _settings.Levels[index] : CreateGeneratedLevel(index);
            CurrentLevel.ValidateEconomy();
            _currentIndex = index;
        }

        return new LevelAttempt(CurrentLevel.StartingTower,
            CurrentLevel.Walls.Select(wall => new Wall(wall.from, wall.to)), CurrentLevel.BaseReward);
    }

    private LevelDefinition CreateGeneratedLevel(int index)
    {
        ReleaseGeneratedLevel();
        GeneratedLevel data = _generator.Generate($"Уровень {index + 1}");
        _runtimeLevel = LevelDefinition.Create(data, _settings.GeneratedLevel.BaseReward);
        return _runtimeLevel;
    }

    private void ReleaseGeneratedLevel()
    {
        if (_runtimeLevel == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            UnityEngine.Object.Destroy(_runtimeLevel);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(_runtimeLevel);
        }

        _runtimeLevel = null;
    }
}
