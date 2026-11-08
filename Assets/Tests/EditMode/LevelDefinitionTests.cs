using System.Linq;
using NUnit.Framework;
using UnityEditor;

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
}
