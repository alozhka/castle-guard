using CastleGuard.Level;
using CastleGuard.Level.Model;
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
    [TestCase("GeneratedLevel", "ABCA")]
    public void Level_CheckRoute_TraversesEveryWallExactlyOnce(string assetName, string route)
    {
        LevelDefinition level = AssetDatabase.LoadAssetAtPath<LevelDefinition>($"Assets/Levels/{assetName}.asset");
        Assert.That(level, Is.Not.Null);
        Assert.DoesNotThrow(level.ValidateEconomy);
        Assert.That(level.BaseReward, Is.EqualTo(10));

        string[] towerIds = level.Towers.Select(tower => tower.id).ToArray();
        Assert.That(towerIds, Is.Unique);
        foreach (Wall wall in level.Walls)
        {
            Assert.That(towerIds, Does.Contain(wall.From));
            Assert.That(towerIds, Does.Contain(wall.To));
        }

        LevelRun run = new(level.StartingTower, level.Walls);
        Assert.That(run.CurrentTower, Is.EqualTo(route[0].ToString()));
        Assert.That(route.Length - 1, Is.EqualTo(level.Walls.Count));

        for (int i = 1; i < route.Length; i++)
        {
            Assert.That(run.TryMove(route[i].ToString()), Is.True, $"Move {i} in {assetName}");
            Assert.That(run.IsComplete, Is.EqualTo(i == route.Length - 1));
        }

        for (int i = 0; i < level.Walls.Count; i++)
        {
            Assert.That(run.IsWallTraversed(i), Is.True);
        }
    }

    [Test]
    public void Walls_InspectorEditing_UpdatesEndpoints()
    {
        LevelDefinition level = ScriptableObject.CreateInstance<LevelDefinition>();
        try
        {
            SerializedObject serializedLevel = new(level);
            SerializedProperty walls = serializedLevel.FindProperty("walls");
            walls.arraySize = 1;
            SerializedProperty wall = walls.GetArrayElementAtIndex(0);
            wall.FindPropertyRelative("from").stringValue = "A";
            wall.FindPropertyRelative("to").stringValue = "B";
            serializedLevel.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(level.Walls.Count, Is.EqualTo(1));
            Assert.That(level.Walls[0].From, Is.EqualTo("A"));
            Assert.That(level.Walls[0].To, Is.EqualTo("B"));
            Assert.That(level.Walls[0].Connects("B", "A"), Is.True);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(level);
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
}
