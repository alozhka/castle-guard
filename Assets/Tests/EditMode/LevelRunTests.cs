using CastleGuard.Level.Model;
using NUnit.Framework;

namespace CastleGuard.Tests.EditMode;

public class LevelRunTests
{
    private LevelRun _run;

    [SetUp]
    public void SetUp()
    {
        _run = new LevelRun("A", new Wall[]
        {
            new("A", "B"),
            new("B", "C"),
            new("C", "D"),
            new("D", "A"),
        });
    }

    [Test]
    public void NewRun_StartsAtAWithNoMoves()
    {
        Assert.That(_run.CurrentTower, Is.EqualTo("A"));
        Assert.That(_run.History, Is.Empty);
        Assert.That(_run.IsComplete, Is.False);

        for (int i = 0; i < 4; i++)
        {
            Assert.That(_run.IsWallTraversed(i), Is.False);
        }
    }

    [TestCase("B", 0)]
    [TestCase("D", 3)]
    public void TryMove_ToNeighbor_RecordsMoveInEitherDirection(string destination, int wallIndex)
    {
        Assert.That(_run.TryMove(destination), Is.True);

        Assert.That(_run.CurrentTower, Is.EqualTo(destination));
        Assert.That(_run.IsWallTraversed(wallIndex), Is.True);
        Assert.That(_run.History.Count, Is.EqualTo(1));
        Assert.That(_run.History[0].From, Is.EqualTo("A"));
        Assert.That(_run.History[0].To, Is.EqualTo(destination));
        Assert.That(_run.History[0].WallIndex, Is.EqualTo(wallIndex));
        Assert.That(_run.IsComplete, Is.False);
    }

    [TestCase("A")]
    [TestCase("C")]
    [TestCase("Unknown")]
    public void TryMove_ToInvalidDestination_DoesNotChangeState(string destination)
    {
        Assert.That(_run.TryMove(destination), Is.False);

        Assert.That(_run.CurrentTower, Is.EqualTo("A"));
        Assert.That(_run.History, Is.Empty);
        Assert.That(_run.IsComplete, Is.False);

        for (int i = 0; i < 4; i++)
        {
            Assert.That(_run.IsWallTraversed(i), Is.False);
        }
    }

    [Test]
    public void TryMove_ThroughUsedWall_DoesNotChangeState()
    {
        _run.TryMove("B");

        Assert.That(_run.TryMove("A"), Is.False);

        Assert.That(_run.CurrentTower, Is.EqualTo("B"));
        Assert.That(_run.History.Count, Is.EqualTo(1));
        Assert.That(_run.History[0].From, Is.EqualTo("A"));
        Assert.That(_run.History[0].To, Is.EqualTo("B"));
        Assert.That(_run.History[0].WallIndex, Is.EqualTo(0));
        Assert.That(_run.IsWallTraversed(0), Is.True);
        Assert.That(_run.IsComplete, Is.False);
    }

    [Test]
    public void TryMove_AroundSquare_CompletesOnlyAfterFourthMove()
    {
        string[] route = { "B", "C", "D", "A", };

        for (int i = 0; i < route.Length; i++)
        {
            Assert.That(_run.TryMove(route[i]), Is.True);
            Assert.That(_run.CurrentTower, Is.EqualTo(route[i]));
            Assert.That(_run.History.Count, Is.EqualTo(i + 1));
            Assert.That(_run.IsWallTraversed(i), Is.True);
            Assert.That(_run.IsComplete, Is.EqualTo(i == 3));
        }

        Assert.That(_run.TryMove("B"), Is.False);
        Assert.That(_run.CurrentTower, Is.EqualTo("A"));
        Assert.That(_run.History.Count, Is.EqualTo(4));
        Assert.That(_run.IsComplete, Is.True);
    }
    [Test]
    public void TryUndo_AtStart_DoesNotChangeState()
    {
        Assert.That(_run.CanUndo, Is.False);
        Assert.That(_run.TryUndo(), Is.False);
        Assert.That(_run.CurrentTower, Is.EqualTo("A"));
        Assert.That(_run.History, Is.Empty);
    }

    [Test]
    public void TryUndo_ReturnsToPreviousTowerAndFreesOnlyLastWall()
    {
        _run.TryMove("B");
        _run.TryMove("C");

        Assert.That(_run.CanUndo, Is.True);
        Assert.That(_run.TryUndo(), Is.True);
        Assert.That(_run.CurrentTower, Is.EqualTo("B"));
        Assert.That(_run.History.Count, Is.EqualTo(1));
        Assert.That(_run.IsWallTraversed(0), Is.True);
        Assert.That(_run.IsWallTraversed(1), Is.False);
        Assert.That(_run.TryMove("C"), Is.True);

        Assert.That(_run.TryUndo(), Is.True);
        Assert.That(_run.TryUndo(), Is.True);
        Assert.That(_run.CurrentTower, Is.EqualTo("A"));
        Assert.That(_run.CanUndo, Is.False);
    }

    [Test]
    public void TryUndo_AfterCompletion_ReopensRun()
    {
        foreach (string tower in new[] { "B", "C", "D", "A", })
        {
            _run.TryMove(tower);
        }

        Assert.That(_run.TryUndo(), Is.True);
        Assert.That(_run.IsComplete, Is.False);
        Assert.That(_run.CurrentTower, Is.EqualTo("D"));
        Assert.That(_run.TryMove("A"), Is.True);
        Assert.That(_run.IsComplete, Is.True);
    }

    [TestCase(0)]
    [TestCase(2)]
    [TestCase(4)]
    public void Restart_ClearsHistoryAndAllTraversedWalls(int moveCount)
    {
        string[] route = { "B", "C", "D", "A", };
        for (int i = 0; i < moveCount; i++)
        {
            _run.TryMove(route[i]);
        }

        _run.Restart();

        Assert.That(_run.CurrentTower, Is.EqualTo("A"));
        Assert.That(_run.History, Is.Empty);
        Assert.That(_run.CanUndo, Is.False);
        Assert.That(_run.IsComplete, Is.False);
        for (int i = 0; i < 4; i++)
        {
            Assert.That(_run.IsWallTraversed(i), Is.False);
        }
        Assert.That(_run.TryMove("B"), Is.True);
    }

    [Test]
    public void Restart_UsesConfiguredStartingTower()
    {
        _run = new LevelRun("D", new Wall[] { new("A", "D"), });
        _run.TryMove("A");

        _run.Restart();

        Assert.That(_run.CurrentTower, Is.EqualTo("D"));
        Assert.That(_run.TryMove("A"), Is.True);
    }
}
