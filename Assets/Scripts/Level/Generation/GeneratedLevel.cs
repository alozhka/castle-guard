using System;
using System.Collections.Generic;
using CastleGuard.Level.Model;

namespace CastleGuard.Level.Generation;

public sealed class GeneratedLevel
{
    public static string Version => LevelGenerator.Version;

    public string Name { get; }
    public uint Seed { get; }
    public IReadOnlyList<LevelDefinition.Tower> Towers { get; }
    public IReadOnlyList<Wall> Walls { get; }
    public string StartingTower { get; }
    public IReadOnlyList<Move> Route { get; }

    internal GeneratedLevel(
        string name,
        uint seed,
        LevelDefinition.Tower[] towers,
        Wall[] walls,
        string startingTower,
        IReadOnlyList<Move> route)
    {
        Name = name;
        Seed = seed;
        Towers = Array.AsReadOnly(towers);
        Walls = Array.AsReadOnly(walls);
        StartingTower = startingTower;
        Route = route;
    }
}
