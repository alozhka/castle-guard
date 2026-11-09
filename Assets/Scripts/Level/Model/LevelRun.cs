using System.Collections.Generic;

namespace CastleGuard.Level.Model;

public sealed class LevelRun
{
    private readonly string _startingTower;
    private readonly List<Wall> _walls;
    private readonly HashSet<int> _traversedWalls = new();
    private readonly List<Move> _history = new();

    public string CurrentTower { get; private set; }
    public IReadOnlyList<Move> History { get; }
    public IReadOnlyList<Wall> Walls { get; }
    public bool CanUndo => _history.Count > 0;
    public bool IsComplete => _traversedWalls.Count == _walls.Count;

    public LevelRun(string startingTower, IEnumerable<Wall> walls)
    {
        _startingTower = startingTower;
        CurrentTower = startingTower;
        _walls = new List<Wall>(walls);
        Walls = _walls.AsReadOnly();
        History = _history.AsReadOnly();
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
        return true;
    }

    public void Restart()
    {
        _history.Clear();
        _traversedWalls.Clear();
        CurrentTower = _startingTower;
    }

    public bool IsWallTraversed(int wallIndex)
    {
        return _traversedWalls.Contains(wallIndex);
    }

    public bool TryMove(string destination)
    {
        if (IsComplete || destination == CurrentTower)
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
}
