using System.Collections.Generic;

public sealed class LevelRun
{
    public readonly struct Move
    {
        public string From { get; }
        public string To { get; }
        public int WallIndex { get; }

        public Move(string from, string to, int wallIndex)
        {
            From = from;
            To = to;
            WallIndex = wallIndex;
        }
    }

    private readonly List<(string From, string To)> walls;
    private readonly HashSet<int> traversedWalls = new();
    private readonly List<Move> history = new();

    public string CurrentTower { get; private set; }
    public IReadOnlyList<Move> History { get; }
    public bool IsComplete => traversedWalls.Count == walls.Count;

    public LevelRun(string startingTower, IEnumerable<(string From, string To)> walls)
    {
        CurrentTower = startingTower;
        this.walls = new(walls);
        History = history.AsReadOnly();
    }

    public bool IsWallTraversed(int wallIndex)
    {
        return traversedWalls.Contains(wallIndex);
    }

    public bool TryMove(string destination)
    {
        if (IsComplete || destination == CurrentTower)
        {
            return false;
        }

        for (int i = 0; i < walls.Count; i++)
        {
            (string From, string To) wall = walls[i];
            bool connectsTowers = wall.From == CurrentTower && wall.To == destination
                || wall.To == CurrentTower && wall.From == destination;

            if (!connectsTowers || traversedWalls.Contains(i))
            {
                continue;
            }

            history.Add(new(CurrentTower, destination, i));
            traversedWalls.Add(i);
            CurrentTower = destination;
            return true;
        }

        return false;
    }
}
