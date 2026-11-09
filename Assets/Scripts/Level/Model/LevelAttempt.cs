using System.Collections.Generic;

namespace CastleGuard.Level.Model;

public sealed class LevelAttempt
{
    private readonly LevelRun _run;
    private readonly int _baseReward;
    private bool _hintLocked;

    public string CurrentTower => _run.CurrentTower;
    public IReadOnlyList<Move> History => _run.History;
    public bool CanUndo => !IsComplete && _run.CanUndo;
    public bool IsComplete => Reward.HasValue;
    public bool CanUseHint => !IsComplete && !_hintLocked;

    public double ElapsedSeconds { get; private set; }
    public bool UsedUndo { get; private set; }
    public bool UsedHint { get; private set; }
    public LevelReward? Reward { get; private set; }

    public LevelAttempt(string startingTower, IEnumerable<Wall> walls, int baseReward)
    {
        LevelRewardCalculator.ValidateBaseReward(baseReward);
        _baseReward = baseReward;
        _run = new LevelRun(startingTower, walls);
        CompleteIfFinished();
    }

    public bool TryAddElapsedTime(double seconds)
    {
        if (IsComplete)
        {
            return false;
        }

        LevelRewardCalculator.ValidateElapsedSeconds(seconds);
        double elapsedSeconds = ElapsedSeconds + seconds;
        LevelRewardCalculator.ValidateElapsedSeconds(elapsedSeconds);
        ElapsedSeconds = elapsedSeconds;
        return true;
    }

    public bool TryMove(string destination)
    {
        if (IsComplete || !_run.TryMove(destination))
        {
            return false;
        }

        _hintLocked = false;
        CompleteIfFinished();
        return true;
    }

    public bool TryUndo()
    {
        if (IsComplete || !_run.TryUndo())
        {
            return false;
        }

        UsedUndo = true;
        return true;
    }

    public bool TryRecordHintUse()
    {
        if (!CanUseHint)
        {
            return false;
        }

        _hintLocked = true;
        UsedHint = true;
        return true;
    }

    public bool IsWallTraversed(int wallIndex)
    {
        return _run.IsWallTraversed(wallIndex);
    }

    private void CompleteIfFinished()
    {
        if (_run.IsComplete)
        {
            Reward = LevelRewardCalculator.Calculate(_baseReward, ElapsedSeconds, UsedUndo, UsedHint);
        }
    }
}
