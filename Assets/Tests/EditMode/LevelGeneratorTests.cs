using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CastleGuard.Level;
using CastleGuard.Level.Generation;
using CastleGuard.Level.Model;
using NUnit.Framework;
using UnityEngine;

namespace CastleGuard.Tests.EditMode;

public class LevelGeneratorTests
{
    [Test]
    public void Generate_ManyNames_ProducesPlayablePlanarLevels()
    {
        LevelGenerator generator = new();
        int ordinaryCount = 0;
        for (int seed = 0; seed < 300; seed++)
        {
            GeneratedLevel level = generator.Generate("Уровень " + seed.ToString(CultureInfo.InvariantCulture));
            Validate(level);
            if (!level.IsFallback)
            {
                ordinaryCount++;
            }
        }

        Assert.That(ordinaryCount, Is.GreaterThan(270), "Generation should normally succeed before the reserve is used.");
    }

    [Test]
    public void Generate_SameName_RepeatsAllDataDespiteOtherCallsAndCulture()
    {
        LevelGenerator generator = new();
        GeneratedLevel first = generator.Generate("Уровень 4");
        generator.Generate("Unrelated level");
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            GeneratedLevel second = new LevelGenerator().Generate("Уровень 4");
            AssertSame(first, second);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Test]
    public void Generate_ExhaustedAttempts_ReturnsRepeatablePlayableReserve()
    {
        LevelGenerator generator = new(0);
        GeneratedLevel first = generator.Generate("reserve");
        Assert.That(first.IsFallback, Is.True);
        Validate(first);
        AssertSame(first, generator.Generate("reserve"));
    }

    [Test]
    public void Generate_UsesVersionedStableHash()
    {
        GeneratedLevel level = new LevelGenerator().Generate("hello");
        Assert.That(level.Seed, Is.EqualTo(0x4f9f2cabu));
        Assert.That(GeneratedLevel.Version, Is.EqualTo("0.1.0"));
    }

    [TestCase(null)]
    [TestCase("")]
    public void Generate_MissingName_RejectsInput(string name)
    {
        Assert.Throws<ArgumentException>(() => new LevelGenerator().Generate(name));
    }

    private static void AssertSame(GeneratedLevel first, GeneratedLevel second)
    {
        Assert.That(second.Seed, Is.EqualTo(first.Seed));
        Assert.That(GeneratedLevel.Version, Is.EqualTo(GeneratedLevel.Version));
        Assert.That(second.IsFallback, Is.EqualTo(first.IsFallback));
        Assert.That(second.StartingTower, Is.EqualTo(first.StartingTower));
        Assert.That(second.Towers, Is.EqualTo(first.Towers));
        Assert.That(second.Walls, Is.EqualTo(first.Walls));
        Assert.That(second.Route, Is.EqualTo(first.Route));
    }

    private static void Validate(GeneratedLevel level)
    {
        Assert.That(level.Towers.Count, Is.InRange(2, 10), level.Name);
        Assert.That(level.Walls.Count, Is.InRange(level.Towers.Count - 1, 14), level.Name);
        Dictionary<string, Vector2> positions = level.Towers.ToDictionary(tower => tower.id, tower => tower.position);
        HashSet<string> pairs = new();
        foreach (LevelDefinition.Wall wall in level.Walls)
        {
            Assert.That(wall.from, Is.Not.EqualTo(wall.to));
            string key = string.CompareOrdinal(wall.from, wall.to) < 0
                ? wall.from + wall.to : wall.to + wall.from;
            Assert.That(pairs.Add(key), Is.True, "Duplicate edge");
            Vector2 a = positions[wall.from];
            Vector2 b = positions[wall.to];
            foreach (KeyValuePair<string, Vector2> tower in positions)
            {
                if (tower.Key != wall.from && tower.Key != wall.to)
                {
                    float t = Mathf.Clamp01(Vector2.Dot(tower.Value - a, b - a) / (b - a).sqrMagnitude);
                    Assert.That(Vector2.Distance(tower.Value, a + t * (b - a)), Is.GreaterThan(0.449f));
                }
            }
        }

        for (int i = 0; i < level.Walls.Count; i++)
        {
            LevelDefinition.Wall first = level.Walls[i];
            for (int j = i + 1; j < level.Walls.Count; j++)
            {
                LevelDefinition.Wall second = level.Walls[j];
                Vector2 a = positions[first.from];
                Vector2 b = positions[first.to];
                Vector2 c = positions[second.from];
                Vector2 d = positions[second.to];
                float determinant = Cross(b - a, d - c);
                if (Mathf.Abs(determinant) > 0.0001f)
                {
                    float t = Cross(c - a, d - c) / determinant;
                    float u = Cross(c - a, b - a) / determinant;
                    Assert.That(t > 0.0001f && t < 0.9999f && u > 0.0001f && u < 0.9999f, Is.False);
                }
                else if (Mathf.Abs(Cross(b - a, c - a)) < 0.0001f)
                {
                    float t = Vector2.Dot(c - a, b - a) / (b - a).sqrMagnitude;
                    float u = Vector2.Dot(d - a, b - a) / (b - a).sqrMagnitude;
                    Assert.That(Mathf.Min(1, Mathf.Max(t, u)) - Mathf.Max(0, Mathf.Min(t, u)), Is.LessThan(0.0001f));
                }
            }
        }

        foreach (LevelDefinition.Tower tower in level.Towers)
        {
            Assert.That(level.Walls.Any(wall => wall.from == tower.id || wall.to == tower.id), Is.True);
            foreach (LevelDefinition.Tower other in level.Towers.Where(other => other.id != tower.id))
            {
                Assert.That(Vector2.Distance(tower.position, other.position), Is.GreaterThan(0.899f));
            }
        }

        LevelRun run = new(level.StartingTower, level.Walls.Select(wall => new Wall(wall.from, wall.to)));
        HashSet<int> traversed = new();
        HashSet<string> visited = new() { level.StartingTower, };
        foreach (Move move in level.Route)
        {
            Assert.That(move.From, Is.EqualTo(run.CurrentTower));
            Assert.That(traversed.Add(move.WallIndex), Is.True);
            Assert.That(run.TryMove(move.To), Is.True);
            Assert.That(run.IsWallTraversed(move.WallIndex), Is.True);
            visited.Add(move.To);
        }

        Assert.That(traversed.Count, Is.EqualTo(level.Walls.Count));
        Assert.That(visited.Count, Is.EqualTo(level.Towers.Count));
        Assert.That(run.IsComplete, Is.True);
    }

    private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
}
