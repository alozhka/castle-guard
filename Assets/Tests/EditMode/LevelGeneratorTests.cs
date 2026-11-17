using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CastleGuard.Level.Model;
using CastleGuard.Level.Model.Definition;
using CastleGuard.Level.Model.Generation;
using NUnit.Framework;
using UnityEngine;

namespace CastleGuard.Tests.EditMode;

public class LevelGeneratorTests
{
    [TestCase(1, 5, 6)]
    [TestCase(6, 6, 8)]
    [TestCase(11, 7, 10)]
    [TestCase(16, 8, 12)]
    [TestCase(21, 9, 14)]
    public void Generate_ManyNames_ProducesPlayablePlanarLevels(int generatedLevelNumber, int towers, int walls)
    {
        LevelGenerator generator = new();
        int generatedCount = 0;
        for (int seed = 0; seed < 300; seed++)
        {
            GeneratedLevel level;
            try
            {
                level = generator.Generate("Уровень " + seed.ToString(CultureInfo.InvariantCulture), generatedLevelNumber);
            }
            catch (InvalidOperationException exception)
            {
                Assert.That(exception.Message, Is.EqualTo("Failed to generate a level after 128 attempts."));
                continue;
            }

            Assert.That(level.Towers.Count, Is.EqualTo(towers), level.Name);
            Assert.That(level.Walls.Count, Is.EqualTo(walls), level.Name);
            int perimeterTowers = level.Towers.Count(tower =>
                Mathf.Approximately(Mathf.Abs(tower.Position.x), 1.8f)
                || Mathf.Approximately(Mathf.Abs(tower.Position.y), 1.8f));
            Assert.That(perimeterTowers, Is.EqualTo((towers * 2 + 1) / 3), level.Name);
            int[] sideCounts =
            {
                level.Towers.Count(tower => Mathf.Approximately(tower.Position.x, -1.8f)),
                level.Towers.Count(tower => Mathf.Approximately(tower.Position.x, 1.8f)),
                level.Towers.Count(tower => Mathf.Approximately(tower.Position.y, -1.8f)),
                level.Towers.Count(tower => Mathf.Approximately(tower.Position.y, 1.8f)),
            };
            Assert.That(sideCounts.Max() - sideCounts.Min(), Is.LessThanOrEqualTo(1), level.Name);
            Validate(level);
            generatedCount++;
        }

        Assert.That(generatedCount, Is.GreaterThan(270), "Generation should normally succeed within the attempt limit.");
    }

    [TestCase(1)]
    [TestCase(6)]
    [TestCase(11)]
    [TestCase(16)]
    [TestCase(21)]
    public void Generate_SameName_RepeatsAllDataDespiteOtherCallsAndCulture(int generatedLevelNumber)
    {
        LevelGenerator generator = new();
        GeneratedLevel first = generator.Generate("Уровень 4", generatedLevelNumber);
        generator.Generate("Unrelated level");
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            GeneratedLevel second = new LevelGenerator().Generate("Уровень 4", generatedLevelNumber);
            AssertSame(first, second);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [TestCase(1)]
    [TestCase(6)]
    [TestCase(11)]
    [TestCase(16)]
    [TestCase(21)]
    public void Generate_ExhaustedAttempts_Throws(int generatedLevelNumber)
    {
        LevelGenerator generator = new(0);
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => generator.Generate("Level", generatedLevelNumber));

        Assert.That(exception.Message, Is.EqualTo("Failed to generate a level after 0 attempts."));
    }

    [Test]
    public void Generate_Progression_KeepsSizeForFiveLevelsThenIncreasesUpToLimit()
    {
        LevelGenerator generator = new();
        for (int stage = 0; stage < 5; stage++)
        {
            for (int offset = 0; offset < 5; offset++)
            {
                int number = stage * 5 + offset + 1;
                GeneratedLevel level = generator.Generate($"Уровень {number}", number);
                Assert.That(level.Towers.Count, Is.EqualTo(5 + stage), level.Name);
                Assert.That(level.Walls.Count, Is.EqualTo(6 + stage * 2), level.Name);
            }
        }

        GeneratedLevel last = generator.Generate("Last", int.MaxValue);
        Assert.That(last.Towers.Count, Is.EqualTo(9));
        Assert.That(last.Walls.Count, Is.EqualTo(14));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void Generate_InvalidGeneratedLevelNumber_RejectsInput(int generatedLevelNumber)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LevelGenerator().Generate("Level", generatedLevelNumber));
    }

    [Test]
    public void Generate_UsesVersionedStableHash()
    {
        GeneratedLevel level = new LevelGenerator().Generate("hello");
        Assert.That(level.Seed, Is.EqualTo(0x4f9f2cabu));
        Assert.That(GeneratedLevel.Version, Is.EqualTo("0.5.0"));
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
        Assert.That(second.StartingTower, Is.EqualTo(first.StartingTower));
        Assert.That(second.Towers, Is.EqualTo(first.Towers));
        Assert.That(second.Walls, Is.EqualTo(first.Walls));
        Assert.That(second.Route, Is.EqualTo(first.Route));
    }

    private static void Validate(GeneratedLevel level)
    {
        Assert.That(level.Towers.Count, Is.InRange(2, 10), level.Name);
        Assert.That(level.Walls.Count, Is.InRange(level.Towers.Count - 1, 14), level.Name);
        Dictionary<string, Vector2> positions = level.Towers.ToDictionary(tower => tower.Id, tower => tower.Position);
        HashSet<string> pairs = new();
        foreach (Wall wall in level.Walls)
        {
            Assert.That(wall.From, Is.Not.EqualTo(wall.To));
            string key = string.CompareOrdinal(wall.From, wall.To) < 0
                ? wall.From + wall.To : wall.To + wall.From;
            Assert.That(pairs.Add(key), Is.True, "Duplicate edge");
            Vector2 a = positions[wall.From];
            Vector2 b = positions[wall.To];
            foreach (KeyValuePair<string, Vector2> tower in positions)
            {
                if (tower.Key != wall.From && tower.Key != wall.To)
                {
                    float t = Mathf.Clamp01(Vector2.Dot(tower.Value - a, b - a) / (b - a).sqrMagnitude);
                    Assert.That(Vector2.Distance(tower.Value, a + t * (b - a)), Is.GreaterThan(0.449f));
                }
            }
        }

        for (int i = 0; i < level.Walls.Count; i++)
        {
            Wall first = level.Walls[i];
            for (int j = i + 1; j < level.Walls.Count; j++)
            {
                Wall second = level.Walls[j];
                Vector2 a = positions[first.From];
                Vector2 b = positions[first.To];
                Vector2 c = positions[second.From];
                Vector2 d = positions[second.To];
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

        foreach (Tower tower in level.Towers)
        {
            Assert.That(level.Walls.Any(wall => wall.From == tower.Id || wall.To == tower.Id), Is.True);
            foreach (Tower other in level.Towers.Where(other => other.Id != tower.Id))
            {
                Assert.That(Vector2.Distance(tower.Position, other.Position), Is.GreaterThan(0.899f));
            }
        }

        LevelAttempt attempt = new(level.StartingTower, level.Walls, 10);
        HashSet<int> traversed = new();
        HashSet<string> visited = new() { level.StartingTower, };
        foreach (Move move in level.Route)
        {
            Assert.That(move.From, Is.EqualTo(attempt.CurrentTower));
            Assert.That(traversed.Add(move.WallIndex), Is.True);
            Assert.That(attempt.TryMove(move.To), Is.True);
            Assert.That(attempt.IsWallTraversed(move.WallIndex), Is.True);
            visited.Add(move.To);
        }

        Assert.That(traversed.Count, Is.EqualTo(level.Walls.Count));
        Assert.That(visited.Count, Is.EqualTo(level.Towers.Count));
        Assert.That(attempt.IsComplete, Is.True);
    }

    private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
}
