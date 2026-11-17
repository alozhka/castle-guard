using System;
using System.Collections.Generic;
using CastleGuard.Level.Model.Definition;
using CastleGuard.Level.Model.Generation;
using UnityEngine;

namespace CastleGuard.Level.Model;

public sealed class LevelCatalog : IDisposable
{
    private readonly IReadOnlyList<LevelDefinition> _levels;
    private readonly LevelDefinition _generatedLevel;
    private readonly LevelGenerator _generator = new();
    private LevelDefinition _runtimeLevel;
    private int _generatedIndex = -1;

    public LevelCatalog(IReadOnlyList<LevelDefinition> levels, LevelDefinition generatedLevel)
    {
        _levels = levels;
        _generatedLevel = generatedLevel;
    }

    public LevelDefinition GetLevel(int index)
    {
        LevelDefinition level = index < _levels.Count ? _levels[index] : GetGeneratedLevel(index);
        level.ValidateEconomy();
        return level;
    }

    public void Dispose() => ReleaseGeneratedLevel();

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
        _generatedIndex = -1;
    }

    private LevelDefinition GetGeneratedLevel(int index)
    {
        if (_generatedIndex != index)
        {
            ReleaseGeneratedLevel();
            int generatedLevelNumber = index - _levels.Count + 1;
            GeneratedLevel data = _generator.Generate($"Уровень {index + 1}", generatedLevelNumber);
            _runtimeLevel = LevelDefinition.Create(data, _generatedLevel.BaseReward);
            _generatedIndex = index;
        }

        return _runtimeLevel;
    }
}
