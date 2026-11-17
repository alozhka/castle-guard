using System;
using System.Collections.Generic;
using System.Text;
using CastleGuard.Level.Model.Definition;
using UnityEngine;
using Random = System.Random;

namespace CastleGuard.Level.Model.Generation;

public sealed class LevelGenerator
{
    public const string Version = "0.3.0";
    private const int MaximumAttempts = 128;
    private const int LevelsPerStage = 5;
    private const int MaximumStage = 4;
    private readonly int _attemptLimit;

    public LevelGenerator(int attemptLimit = MaximumAttempts)
    {
        if (attemptLimit is < 0 or > MaximumAttempts)
        {
            throw new ArgumentOutOfRangeException(nameof(attemptLimit));
        }

        _attemptLimit = attemptLimit;
    }

    public GeneratedLevel Generate(string name, int generatedLevelNumber = 1)
    {
        EnsureValid(name, generatedLevelNumber);

        int stage = Math.Min((generatedLevelNumber - 1) / LevelsPerStage, MaximumStage);
        int towerCount = 5 + stage;
        int wallCount = 6 + stage * 2;
        uint seed = Hash(name);
        Random random = new(unchecked((int)seed));
        for (int attempt = 0; attempt < _attemptLimit; attempt++)
        {
            Vector2Int[] positions = PlaceTowers(towerCount, random);
            List<Edge> edges = BuildGraph(positions, wallCount, random);
            if (edges.Count != wallCount)
            {
                continue;
            }

            GeneratedLevel result = CreateResult(name, seed, positions, edges, random);
            if (result != null)
            {
                return result;
            }
        }

        throw new InvalidOperationException($"Failed to generate a level after {_attemptLimit} attempts.");
    }

    private static Vector2Int[] PlaceTowers(int count, Random random)
    {
        List<Vector2Int> grid = new(25);
        for (int x = -2; x <= 2; x++)
        {
            for (int y = -2; y <= 2; y++)
            {
                grid.Add(new Vector2Int(x, y));
            }
        }

        Shuffle(grid, random);
        return grid.GetRange(0, count).ToArray();
    }

    private static List<Edge> BuildGraph(Vector2Int[] positions, int target, Random random)
    {
        List<Edge> candidates = CreateShuffledEdgeCandidates(positions.Length, random);
        List<Edge> edges = ConnectTowers(positions, candidates);
        if (edges.Count == positions.Length - 1)
        {
            AddRemainingWalls(positions, candidates, edges, target);
        }

        return edges;
    }

    private static List<Edge> CreateShuffledEdgeCandidates(int towerCount, Random random)
    {
        List<Edge> candidates = new();
        for (int from = 0; from < towerCount; from++)
        {
            for (int to = from + 1; to < towerCount; to++)
            {
                candidates.Add(new Edge(from, to));
            }
        }

        Shuffle(candidates, random);
        return candidates;
    }

    private static List<Edge> ConnectTowers(Vector2Int[] positions, List<Edge> candidates)
    {
        List<Edge> edges = new();
        int[] components = new int[positions.Length];
        for (int i = 0; i < components.Length; i++)
        {
            components[i] = i;
        }

        foreach (Edge edge in candidates)
        {
            if (components[edge.From] == components[edge.To] || !CanAdd(edge, positions, edges))
            {
                continue;
            }

            int oldComponent = components[edge.To];
            int newComponent = components[edge.From];
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == oldComponent)
                {
                    components[i] = newComponent;
                }
            }

            edges.Add(edge);
        }

        return edges;
    }

    private static void AddRemainingWalls(Vector2Int[] positions, List<Edge> candidates, List<Edge> edges, int target)
    {
        foreach (Edge edge in candidates)
        {
            if (edges.Count == target)
            {
                break;
            }

            if (!edges.Contains(edge) && CanAdd(edge, positions, edges))
            {
                edges.Add(edge);
            }
        }
    }

    private static bool CanAdd(Edge edge, Vector2Int[] positions, List<Edge> edges)
    {
        if (!HasTowerClearance(edge, positions))
        {
            return false;
        }

        foreach (Edge existing in edges)
        {
            if (WallsConflict(edge, existing, positions))
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasTowerClearance(Edge edge, Vector2Int[] positions)
    {
        Vector2Int a = positions[edge.From];
        Vector2Int b = positions[edge.To];
        Vector2Int direction = b - a;
        int lengthSquared = Dot(direction, direction);
        for (int i = 0; i < positions.Length; i++)
        {
            if (i == edge.From || i == edge.To)
            {
                continue;
            }

            Vector2Int offset = positions[i] - a;
            int projection = Dot(offset, direction);
            int cross = Cross(direction, offset);

            if (projection > 0 && projection < lengthSquared && 4 * cross * cross <= lengthSquared)
            {
                return false;
            }
        }

        return true;
    }

    private static bool WallsConflict(Edge edge, Edge existing, Vector2Int[] positions)
    {
        int? shared = null;
        if (edge.From == existing.From || edge.From == existing.To)
        {
            shared = edge.From;
        }
        else
        {
            if (edge.To == existing.From || edge.To == existing.To)
            {
                shared = edge.To;
            }
        }

        if (shared.HasValue)
        {
            Vector2Int origin = positions[shared.Value];
            Vector2Int first = positions[edge.From == shared ? edge.To : edge.From] - origin;
            Vector2Int second = positions[existing.From == shared ? existing.To : existing.From] - origin;
            return Cross(first, second) == 0 && Dot(first, second) > 0;
        }

        return Intersects(positions[edge.From], positions[edge.To], positions[existing.From], positions[existing.To]);
    }

    private static bool Intersects(Vector2Int a, Vector2Int b, Vector2Int c, Vector2Int d)
    {
        return Math.Max(a.x, b.x) >= Math.Min(c.x, d.x)
               && Math.Max(c.x, d.x) >= Math.Min(a.x, b.x)
               && Math.Max(a.y, b.y) >= Math.Min(c.y, d.y)
               && Math.Max(c.y, d.y) >= Math.Min(a.y, b.y)
               && Cross(b - a, c - a) * Cross(b - a, d - a) <= 0
               && Cross(d - c, a - c) * Cross(d - c, b - c) <= 0;
    }

    private static int Cross(Vector2Int a, Vector2Int b) => a.x * b.y - a.y * b.x;
    private static int Dot(Vector2Int a, Vector2Int b) => a.x * b.x + a.y * b.y;

    private static GeneratedLevel CreateResult(
        string name,
        uint seed,
        Vector2Int[] positions,
        List<Edge> edges,
        Random random
    )
    {
        if (!TryChooseStartingTower(positions.Length, edges, random, out int start))
        {
            return null;
        }

        Tower[] towers = CreateTowers(positions);
        Wall[] walls = CreateWalls(edges);
        LevelAttempt attempt = new(TowerId(start), walls, 10);
        if (!RouteSolver.TrySolve(attempt, out IReadOnlyList<Move> route) || route.Count != edges.Count)
        {
            return null;
        }

        return new GeneratedLevel(name, seed, towers, walls, TowerId(start), route);
    }

    private static bool TryChooseStartingTower(int towerCount, List<Edge> edges, Random random, out int start)
    {
        start = -1;
        int[] degrees = CountTowerDegrees(towerCount, edges);
        List<int> odd = new();
        for (int i = 0; i < degrees.Length; i++)
        {
            if (degrees[i] == 0)
            {
                return false;
            }

            if (degrees[i] % 2 != 0)
            {
                odd.Add(i);
            }
        }

        if (odd.Count != 0 && odd.Count != 2)
        {
            return false;
        }

        start = odd.Count == 2 ? odd[random.Next(2)] : random.Next(towerCount);
        return true;
    }

    private static int[] CountTowerDegrees(int towerCount, List<Edge> edges)
    {
        int[] degrees = new int[towerCount];
        foreach (Edge edge in edges)
        {
            degrees[edge.From]++;
            degrees[edge.To]++;
        }

        return degrees;
    }

    private static Tower[] CreateTowers(Vector2Int[] positions)
    {
        Tower[] towers = new Tower[positions.Length];
        for (int i = 0; i < towers.Length; i++)
        {
            towers[i] = new Tower(TowerId(i), new Vector2(positions[i].x * 0.9f, positions[i].y * 0.9f));
        }

        return towers;
    }

    private static Wall[] CreateWalls(List<Edge> edges)
    {
        Wall[] walls = new Wall[edges.Count];
        for (int i = 0; i < walls.Length; i++)
        {
            Edge edge = edges[i];
            walls[i] = new Wall(TowerId(edge.From), TowerId(edge.To));
        }

        return walls;
    }

    private static string TowerId(int index) => ((char)('A' + index)).ToString();

    private static uint Hash(string name)
    {
        uint hash = 2166136261;
        foreach (byte value in Encoding.UTF8.GetBytes(name))
        {
            hash = unchecked((hash ^ value) * 16777619);
        }

        return hash;
    }

    private static void Shuffle<T>(List<T> items, Random random)
    {
        for (int i = items.Count - 1; i > 0; i--)
        {
            int other = random.Next(i + 1);
            (items[i], items[other]) = (items[other], items[i]);
        }
    }

    private static void EnsureValid(string name, int generatedLevelNumber)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException("A level name is required.", nameof(name));
        }

        if (generatedLevelNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(generatedLevelNumber));
        }
    }

    private readonly record struct Edge
    {
        public int From { get; }
        public int To { get; }

        public Edge(int from, int to)
        {
            From = from;
            To = to;
        }
    }
}