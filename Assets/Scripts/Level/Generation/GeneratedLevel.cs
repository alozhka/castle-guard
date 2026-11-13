using System;
using System.Collections.Generic;
using CastleGuard.Level.Model;

namespace CastleGuard.Level.Generation;

public sealed class GeneratedLevel
{
    public static string Version => LevelGenerator.Version;

    public string Name { get; }
    public uint Seed { get; }
    public bool IsFallback { get; }
    public IReadOnlyList<LevelDefinition.Tower> Towers { get; }
    public IReadOnlyList<LevelDefinition.Wall> Walls { get; }
    public string StartingTower { get; }
    public IReadOnlyList<Move> Route { get; }

    internal GeneratedLevel(string name, uint seed, bool isFallback,
        LevelDefinition.Tower[] towers, LevelDefinition.Wall[] walls,
        string startingTower, IReadOnlyList<Move> route)
    {
        Name = name;
        Seed = seed;
        IsFallback = isFallback;
        Towers = Array.AsReadOnly(towers);
        Walls = Array.AsReadOnly(walls);
        StartingTower = startingTower;
        Route = route;
    }
}
