using System;
using System.Collections.Generic;
using System.Text;
using CastleGuard.Level.Model;
using UnityEngine;

namespace CastleGuard.Level.Generation;

public sealed class LevelGenerator
{
    public const string Version = "0.2.0";
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
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException("A level name is required.", nameof(name));
        }

        if (generatedLevelNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(generatedLevelNumber));
        }

        int stage = Math.Min((generatedLevelNumber - 1) / LevelsPerStage, MaximumStage);
        int towerCount = 5 + stage;
        int wallCount = 6 + stage * 2;
        uint seed = Hash(name);
        RandomSource random = new(seed);
        for (int attempt = 0; attempt < _attemptLimit; attempt++)
        {
            Vector2Int[] positions = PlaceTowers(towerCount, random);
            List<Edge> edges = BuildGraph(positions, wallCount, random);
            if (edges.Count != wallCount)
            {
                continue;
            }

            GeneratedLevel result = CreateResult(name, seed, false, positions, edges, random);
            if (result != null)
            {
                return result;
            }
        }

        (Vector2Int[] reserve, List<Edge> reserveEdges) = CreateReserve(towerCount);
        return CreateResult(name, seed, true, reserve, reserveEdges, random)
               ?? throw new InvalidOperationException("The reserve level must be solvable.");
    }

    private static Vector2Int[] PlaceTowers(int count, RandomSource random)
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

    private static List<Edge> BuildGraph(Vector2Int[] positions, int target, RandomSource random)
    {
        List<Edge> candidates = new();
        for (int from = 0; from < positions.Length; from++)
        {
            for (int to = from + 1; to < positions.Length; to++)
            {
                candidates.Add(new Edge(from, to));
            }
        }

        Shuffle(candidates, random);
        List<Edge> edges = new();
        int[] components = new int[positions.Length];
        for (int i = 0; i < components.Length; i++)
        {
            components[i] = i;
        }

        // Join components first; every accepted edge preserves the geometry.
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

        if (edges.Count != positions.Length - 1)
        {
            return edges;
        }

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

        return edges;
    }

    private static (Vector2Int[], List<Edge>) CreateReserve(int towerCount)
    {
        // Each template is planar and has an Euler route and the stage's exact size.
        return towerCount switch
        {
            5 => (
                new Vector2Int[] { new(2, -2), new(-2, 1), new(-2, -2), new(2, 1), new(-1, 1), },
                new List<Edge> { new(2, 4), new(0, 2), new(2, 3), new(1, 4), new(1, 2), new(0, 3), }
            ),
            6 => (
                new Vector2Int[] { new(2, 0), new(1, -2), new(-1, 2), new(-1, -1), new(-2, 1), new(0, 0), },
                new List<Edge> { new(1, 5), new(2, 3), new(1, 3), new(2, 4), new(0, 2), new(2, 5), new(0, 5), new(3, 5), }
            ),
            7 => (
                new Vector2Int[] { new(-1, 2), new(-2, 2), new(2, 2), new(-1, 0), new(-2, -1), new(0, -1), new(0, 0), },
                new List<Edge>
                {
                    new(2, 3), new(3, 6), new(1, 4), new(3, 5), new(3, 4),
                    new(0, 3), new(1, 3), new(2, 6), new(5, 6), new(0, 2),
                }
            ),
            8 => (
                new Vector2Int[] { new(2, -2), new(-2, 1), new(0, 1), new(-2, -2), new(1, 1), new(2, 2), new(-2, 0), new(1, -1), },
                new List<Edge>
                {
                    new(3, 4), new(0, 7), new(2, 6), new(0, 3), new(1, 6), new(2, 4),
                    new(4, 5), new(4, 7), new(3, 7), new(1, 2), new(3, 6), new(5, 7),
                }
            ),
            9 => (
                new Vector2Int[] { new(-1, 2), new(-2, 0), new(-1, 0), new(0, -1), new(-1, -1), new(-2, -1), new(-2, 1), new(-2, -2), new(1, 2), },
                new List<Edge>
                {
                    new(1, 4), new(4, 7), new(5, 7), new(0, 8), new(0, 3), new(2, 3), new(0, 6),
                    new(3, 4), new(1, 5), new(3, 8), new(2, 6), new(1, 6), new(1, 2), new(0, 2),
                }
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(towerCount)),
        };
    }

    private static bool CanAdd(Edge edge, Vector2Int[] positions, List<Edge> edges)
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
            // Keep half a grid cell free around every unrelated tower.
            if (projection > 0 && projection < lengthSquared && 4 * cross * cross <= lengthSquared)
            {
                return false;
            }
        }

        foreach (Edge existing in edges)
        {
            int shared = edge.From == existing.From || edge.From == existing.To ? edge.From
                : edge.To == existing.From || edge.To == existing.To ? edge.To : -1;
            if (shared >= 0)
            {
                Vector2Int origin = positions[shared];
                Vector2Int first = positions[edge.From == shared ? edge.To : edge.From] - origin;
                Vector2Int second = positions[existing.From == shared ? existing.To : existing.From] - origin;
                if (Cross(first, second) == 0 && Dot(first, second) > 0)
                {
                    return false;
                }
            }
            else if (Intersects(a, b, positions[existing.From], positions[existing.To]))
            {
                return false;
            }
        }

        return true;
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
        bool fallback,
        Vector2Int[] positions,
        List<Edge> edges,
        RandomSource random
    )
    {
        int[] degrees = new int[positions.Length];
        foreach (Edge edge in edges)
        {
            degrees[edge.From]++;
            degrees[edge.To]++;
        }

        List<int> odd = new();
        for (int i = 0; i < degrees.Length; i++)
        {
            if (degrees[i] == 0)
            {
                return null;
            }

            if (degrees[i] % 2 != 0)
            {
                odd.Add(i);
            }
        }

        if (odd.Count != 0 && odd.Count != 2)
        {
            return null;
        }

        int start = odd.Count == 2 ? odd[random.Next(2)] : random.Next(positions.Length);
        LevelDefinition.Tower[] towers = new LevelDefinition.Tower[positions.Length];
        for (int i = 0; i < towers.Length; i++)
        {
            towers[i] = new LevelDefinition.Tower
            {
                id = TowerId(i),
                position = new Vector2(positions[i].x * 0.9f, positions[i].y * 0.9f),
            };
        }

        LevelDefinition.Wall[] walls = new LevelDefinition.Wall[edges.Count];
        List<Wall> connections = new(edges.Count);
        for (int i = 0; i < walls.Length; i++)
        {
            Edge edge = edges[i];
            walls[i] = new LevelDefinition.Wall { from = TowerId(edge.From), to = TowerId(edge.To), };
            connections.Add(new Wall(walls[i].from, walls[i].to));
        }

        LevelAttempt attempt = new(TowerId(start), connections, 10);
        if (!RouteSolver.TrySolve(attempt, out IReadOnlyList<Move> route) || route.Count != edges.Count)
        {
            return null;
        }

        return new GeneratedLevel(name, seed, fallback, towers, walls, TowerId(start), route);
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

    private static void Shuffle<T>(List<T> items, RandomSource random)
    {
        for (int i = items.Count - 1; i > 0; i--)
        {
            int other = random.Next(i + 1);
            (items[i], items[other]) = (items[other], items[i]);
        }
    }

    private readonly struct Edge
    {
        public int From { get; }
        public int To { get; }

        public Edge(int from, int to)
        {
            From = from;
            To = to;
        }
    }

    private sealed class RandomSource
    {
        private uint _state;

        public RandomSource(uint seed)
        {
            _state = seed == 0 ? 0x6D2B79F5u : seed;
        }

        public int Next(int exclusiveMaximum)
        {
            _state ^= _state << 13;
            _state ^= _state >> 17;
            _state ^= _state << 5;
            return (int)(_state % (uint)exclusiveMaximum);
        }
    }
}