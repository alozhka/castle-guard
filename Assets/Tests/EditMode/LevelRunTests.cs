using NUnit.Framework;

public class LevelRunTests
{
    private LevelRun run;

    [SetUp]
    public void SetUp()
    {
        run = new LevelRun("A", new[]
        {
            ("A", "B"),
            ("B", "C"),
            ("C", "D"),
            ("D", "A")
        });
    }

    [Test]
    public void NewRun_StartsAtAWithNoMoves()
    {
        Assert.That(run.CurrentTower, Is.EqualTo("A"));
        Assert.That(run.History, Is.Empty);
        Assert.That(run.IsComplete, Is.False);

        for (int i = 0; i < 4; i++)
        {
            Assert.That(run.IsWallTraversed(i), Is.False);
        }
    }

    [TestCase("B", 0)]
    [TestCase("D", 3)]
    public void TryMove_ToNeighbor_RecordsMoveInEitherDirection(string destination, int wallIndex)
    {
        Assert.That(run.TryMove(destination), Is.True);

        Assert.That(run.CurrentTower, Is.EqualTo(destination));
        Assert.That(run.IsWallTraversed(wallIndex), Is.True);
        Assert.That(run.History.Count, Is.EqualTo(1));
        Assert.That(run.History[0].From, Is.EqualTo("A"));
        Assert.That(run.History[0].To, Is.EqualTo(destination));
        Assert.That(run.History[0].WallIndex, Is.EqualTo(wallIndex));
        Assert.That(run.IsComplete, Is.False);
    }

    [TestCase("A")]
    [TestCase("C")]
    [TestCase("Unknown")]
    public void TryMove_ToInvalidDestination_DoesNotChangeState(string destination)
    {
        Assert.That(run.TryMove(destination), Is.False);

        Assert.That(run.CurrentTower, Is.EqualTo("A"));
        Assert.That(run.History, Is.Empty);
        Assert.That(run.IsComplete, Is.False);

        for (int i = 0; i < 4; i++)
        {
            Assert.That(run.IsWallTraversed(i), Is.False);
        }
    }

    [Test]
    public void TryMove_ThroughUsedWall_DoesNotChangeState()
    {
        run.TryMove("B");

        Assert.That(run.TryMove("A"), Is.False);

        Assert.That(run.CurrentTower, Is.EqualTo("B"));
        Assert.That(run.History.Count, Is.EqualTo(1));
        Assert.That(run.History[0].From, Is.EqualTo("A"));
        Assert.That(run.History[0].To, Is.EqualTo("B"));
        Assert.That(run.History[0].WallIndex, Is.EqualTo(0));
        Assert.That(run.IsWallTraversed(0), Is.True);
        Assert.That(run.IsComplete, Is.False);
    }

    [Test]
    public void TryMove_AroundSquare_CompletesOnlyAfterFourthMove()
    {
        string[] route = { "B", "C", "D", "A" };

        for (int i = 0; i < route.Length; i++)
        {
            Assert.That(run.TryMove(route[i]), Is.True);
            Assert.That(run.CurrentTower, Is.EqualTo(route[i]));
            Assert.That(run.History.Count, Is.EqualTo(i + 1));
            Assert.That(run.IsWallTraversed(i), Is.True);
            Assert.That(run.IsComplete, Is.EqualTo(i == 3));
        }

        Assert.That(run.TryMove("B"), Is.False);
        Assert.That(run.CurrentTower, Is.EqualTo("A"));
        Assert.That(run.History.Count, Is.EqualTo(4));
        Assert.That(run.IsComplete, Is.True);
    }
}
