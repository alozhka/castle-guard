using System;
using System.Collections.Generic;

namespace CastleGuard.Level.Model;

public static class RouteSolver
{
    public static bool TrySolve(LevelAttempt attempt, out IReadOnlyList<Move> route)
    {
        if (attempt == null)
        {
            throw new ArgumentNullException(nameof(attempt));
        }

        bool[] usedWalls = CopyTraversedWalls(attempt, out int remainingWallCount);
        List<Move> path = new(remainingWallCount);
        bool solved = TryCompleteRoute(attempt.Walls, usedWalls, attempt.CurrentTower, remainingWallCount, path);
        route = path.AsReadOnly();
        return solved;
    }

    private static bool[] CopyTraversedWalls(LevelAttempt attempt, out int remainingWallCount)
    {
        bool[] usedWalls = new bool[attempt.Walls.Count];
        remainingWallCount = 0;
        for (int i = 0; i < usedWalls.Length; i++)
        {
            usedWalls[i] = attempt.IsWallTraversed(i);
            if (!usedWalls[i])
            {
                remainingWallCount++;
            }
        }

        return usedWalls;
    }

    private static bool TryCompleteRoute(
        IReadOnlyList<Wall> walls,
        bool[] usedWalls,
        string tower,
        int requiredMoveCount,
        List<Move> path)
    {
        if (path.Count == requiredMoveCount)
        {
            return true;
        }

        for (int i = 0; i < walls.Count; i++)
        {
            Wall wall = walls[i];
            if (usedWalls[i] || wall.From == wall.To || (wall.From != tower && wall.To != tower))
            {
                continue;
            }

            string destination = wall.From == tower ? wall.To : wall.From;
            usedWalls[i] = true;
            path.Add(new Move(tower, destination, i));
            if (TryCompleteRoute(walls, usedWalls, destination, requiredMoveCount, path))
            {
                return true;
            }

            path.RemoveAt(path.Count - 1);
            usedWalls[i] = false;
        }

        return false;
    }
}
