using CastleGuard.Level;
using CastleGuard.Level.Model;
using CastleGuard.Economy;
using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CastleGuard.Tests.EditMode;

public class LevelDefinitionTests
{
    [TestCase("Level01", "ABCDA")]
    [TestCase("Level02", "ABCDAC")]
    [TestCase("Level03", "DABCDEC")]
    [TestCase("Level04GeneratedPlaceholder", "ABCA")]
    public void Level_CheckRoute_TraversesEveryWallExactlyOnce(string assetName, string route)
    {
        LevelDefinition level = AssetDatabase.LoadAssetAtPath<LevelDefinition>($"Assets/Levels/{assetName}.asset");
        Assert.That(level, Is.Not.Null);
        Assert.DoesNotThrow(level.ValidateEconomy);
        Assert.That(level.BaseReward, Is.EqualTo(10));

        string[] towerIds = level.Towers.Select(tower => tower.id).ToArray();
        Assert.That(towerIds, Is.Unique);
        foreach (LevelDefinition.Wall wall in level.Walls)
        {
            Assert.That(towerIds, Does.Contain(wall.from));
            Assert.That(towerIds, Does.Contain(wall.to));
        }

        Wall[] walls = level.Walls.Select(wall => new Wall(wall.from, wall.to)).ToArray();
        LevelRun run = new(level.StartingTower, walls);
        Assert.That(run.CurrentTower, Is.EqualTo(route[0].ToString()));
        Assert.That(route.Length - 1, Is.EqualTo(walls.Length));

        for (int i = 1; i < route.Length; i++)
        {
            Assert.That(run.TryMove(route[i].ToString()), Is.True, $"Move {i} in {assetName}");
            Assert.That(run.IsComplete, Is.EqualTo(i == route.Length - 1));
        }

        for (int i = 0; i < walls.Length; i++)
        {
            Assert.That(run.IsWallTraversed(i), Is.True);
        }
    }

    [TestCase(-1, false)]
    [TestCase(1073741824, false)]
    [TestCase(1073741823, true)]
    [TestCase(10, true)]
    [TestCase(0, true)]
    public void ValidateEconomy_ChecksBaseAndMaximumReward(int reward, bool valid)
    {
        LevelDefinition level = ScriptableObject.CreateInstance<LevelDefinition>();
        try
        {
            // Set fields without invoking OnValidate, so the explicit validation is exercised.
            typeof(LevelDefinition).GetField("baseReward", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(level, reward);
            if (valid)
            {
                Assert.DoesNotThrow(level.ValidateEconomy);
            }
            else
            {
                Assert.Throws<InvalidOperationException>(level.ValidateEconomy);
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(level);
        }
    }

    [Test]
    public void EconomyAsset_HasPrototypeHintPrice()
    {
        EconomySettings settings = AssetDatabase.LoadAssetAtPath<EconomySettings>("Assets/Settings/Economy.asset");

        Assert.That(settings, Is.Not.Null);
        Assert.DoesNotThrow(settings.Validate);
        Assert.That(settings.HintPrice, Is.EqualTo(5));
    }

    [TestCase(-1)]
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(int.MaxValue)]
    public void EconomySettings_RequiresPositivePrice(int price)
    {
        EconomySettings settings = ScriptableObject.CreateInstance<EconomySettings>();
        try
        {
            typeof(EconomySettings).GetField("hintPrice", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(settings, price);
            if (price > 0)
            {
                Assert.DoesNotThrow(settings.Validate);
            }
            else
            {
                Assert.Throws<InvalidOperationException>(settings.Validate);
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(settings);
        }
    }
}
