using System.Collections.Generic;
using CastleGuard.Level.Model.Definition;
using CastleGuard.Level.Model.Rewards;

namespace CastleGuard.Level.Model;

public sealed class LevelAttempt
{
    private readonly List<Wall> _walls;
    private readonly HashSet<int> _traversedWalls = new();
    private readonly List<Move> _history = new();
    private readonly int _baseReward;
    private bool _hintLocked;

    public string CurrentTower { get; private set; }
    public IReadOnlyList<Move> History { get; }
    public IReadOnlyList<Wall> Walls { get; }
    public bool CanUndo => !IsComplete && _history.Count > 0;
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
        CurrentTower = startingTower;
        _walls = new List<Wall>(walls);
        Walls = _walls.AsReadOnly();
        History = _history.AsReadOnly();
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
        if (IsComplete || !TryTraverseWall(destination))
        {
            return false;
        }

        _hintLocked = false;
        CompleteIfFinished();
        return true;
    }

    public bool TryUndo()
    {
        if (!CanUndo)
        {
            return false;
        }

        Move move = _history[^1];
        _history.RemoveAt(_history.Count - 1);
        _traversedWalls.Remove(move.WallIndex);
        CurrentTower = move.From;
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
        return _traversedWalls.Contains(wallIndex);
    }

    private bool TryTraverseWall(string destination)
    {
        if (destination == CurrentTower)
        {
            return false;
        }

        for (int i = 0; i < _walls.Count; i++)
        {
            Wall wall = _walls[i];
            if (!wall.Connects(CurrentTower, destination) || _traversedWalls.Contains(i))
            {
                continue;
            }

            _history.Add(new Move(CurrentTower, destination, i));
            _traversedWalls.Add(i);
            CurrentTower = destination;
            return true;
        }

        return false;
    }

    private void CompleteIfFinished()
    {
        if (_traversedWalls.Count == _walls.Count)
        {
            Reward = LevelRewardCalculator.Calculate(_baseReward, ElapsedSeconds, UsedUndo, UsedHint);
        }
    }
}
