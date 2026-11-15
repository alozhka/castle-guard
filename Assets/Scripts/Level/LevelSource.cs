using System;
using System.Linq;
using CastleGuard.Level.Generation;
using CastleGuard.Level.Model;

namespace CastleGuard.Level;

public sealed class LevelSource : IDisposable
{
    private readonly LevelDefinition[] _levels;
    private readonly LevelDefinition _generatedLevel;
    private LevelDefinition _runtimeLevel;
    private int _currentIndex = -1;

    public LevelDefinition Current { get; private set; }

    public LevelSource(LevelDefinition[] levels, LevelDefinition generatedLevel)
    {
        _levels = levels;
        _generatedLevel = generatedLevel;
    }

    public int GetInitialIndex(int savedIndex) => _generatedLevel == null
        ? Math.Min(savedIndex, _levels.Length - 1)
        : savedIndex;

    public LevelAttempt CreateAttempt(int index)
    {
        if (_currentIndex != index)
        {
            Current = index < _levels.Length ? _levels[index] : CreateGeneratedLevel(index);
            Current.ValidateEconomy();
            _currentIndex = index;
        }

        return new LevelAttempt(Current.StartingTower,
            Current.Walls.Select(wall => new Wall(wall.from, wall.to)), Current.BaseReward);
    }

    public void Dispose()
    {
        if (_runtimeLevel != null)
        {
            UnityEngine.Object.Destroy(_runtimeLevel);
        }
    }

    private LevelDefinition CreateGeneratedLevel(int index)
    {
        Dispose();
        GeneratedLevel data = new LevelGenerator().Generate($"Уровень {index + 1}");
        _runtimeLevel = LevelDefinition.Create(data, _generatedLevel.BaseReward);
        return _runtimeLevel;
    }
}
