using System;
using CastleGuard.Level.Model.Definition;
using CastleGuard.Model;
using CastleGuard.Progress;

namespace CastleGuard.Level.Model;

public sealed class GameModel : IDisposable
{
    private readonly GameSettings _settings;
    private LevelCatalog _levels;

    public PlayerProgress Progress { get; }
    public LevelModel Level { get; private set; }
    public LevelDefinition CurrentLevel => Level?.CurrentLevel;

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
        _levels = new LevelCatalog(_settings.Levels, _settings.GeneratedLevel);
        Level = new LevelModel(_levels, Progress,
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
        _levels?.Dispose();
        _levels = null;
    }
}
