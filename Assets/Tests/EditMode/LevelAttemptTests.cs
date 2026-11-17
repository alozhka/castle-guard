using CastleGuard.Level.Model;
using CastleGuard.Level.Model.Definition;
using NUnit.Framework;

namespace CastleGuard.Tests.EditMode;

public class LevelAttemptTests
{
    private static readonly Wall[] _shortRouteWalls = { new("A", "B"), new("B", "C"), };
    private LevelAttempt _attempt;

    [SetUp]
    public void SetUp()
    {
        _attempt = new LevelAttempt("A", new Wall[]
        {
            new("A", "B"),
            new("B", "C"),
            new("C", "D"),
            new("D", "A"),
        }, 10);
    }

    [TestCase("A")]
    [TestCase("D")]
    public void NewAttempt_StartsAtConfiguredTowerWithNoMoves(string startingTower)
    {
        _attempt = new LevelAttempt(startingTower, _attempt.Walls, 10);

        Assert.That(_attempt.CurrentTower, Is.EqualTo(startingTower));
        Assert.That(_attempt.History, Is.Empty);
        Assert.That(_attempt.IsComplete, Is.False);

        for (int i = 0; i < 4; i++)
        {
            Assert.That(_attempt.IsWallTraversed(i), Is.False);
        }
    }

    [TestCase("B", 0)]
    [TestCase("D", 3)]
    public void TryMove_ToNeighbor_RecordsMoveInEitherDirection(string destination, int wallIndex)
    {
        Assert.That(_attempt.TryMove(destination), Is.True);

        Assert.That(_attempt.CurrentTower, Is.EqualTo(destination));
        Assert.That(_attempt.IsWallTraversed(wallIndex), Is.True);
        Assert.That(_attempt.History.Count, Is.EqualTo(1));
        Assert.That(_attempt.History[0].From, Is.EqualTo("A"));
        Assert.That(_attempt.History[0].To, Is.EqualTo(destination));
        Assert.That(_attempt.History[0].WallIndex, Is.EqualTo(wallIndex));
        Assert.That(_attempt.IsComplete, Is.False);
    }

    [TestCase("A")]
    [TestCase("C")]
    [TestCase("Unknown")]
    public void TryMove_ToInvalidDestination_DoesNotChangeState(string destination)
    {
        Assert.That(_attempt.TryMove(destination), Is.False);

        Assert.That(_attempt.CurrentTower, Is.EqualTo("A"));
        Assert.That(_attempt.History, Is.Empty);
        Assert.That(_attempt.IsComplete, Is.False);

        for (int i = 0; i < 4; i++)
        {
            Assert.That(_attempt.IsWallTraversed(i), Is.False);
        }
    }

    [Test]
    public void TryMove_ThroughUsedWall_DoesNotChangeState()
    {
        _attempt.TryMove("B");

        Assert.That(_attempt.TryMove("A"), Is.False);

        Assert.That(_attempt.CurrentTower, Is.EqualTo("B"));
        Assert.That(_attempt.History.Count, Is.EqualTo(1));
        Assert.That(_attempt.History[0].From, Is.EqualTo("A"));
        Assert.That(_attempt.History[0].To, Is.EqualTo("B"));
        Assert.That(_attempt.History[0].WallIndex, Is.EqualTo(0));
        Assert.That(_attempt.IsWallTraversed(0), Is.True);
        Assert.That(_attempt.IsComplete, Is.False);
    }

    [Test]
    public void TryMove_AroundSquare_CompletesOnlyAfterFourthMove()
    {
        string[] route = { "B", "C", "D", "A", };

        for (int i = 0; i < route.Length; i++)
        {
            Assert.That(_attempt.TryMove(route[i]), Is.True);
            Assert.That(_attempt.CurrentTower, Is.EqualTo(route[i]));
            Assert.That(_attempt.History.Count, Is.EqualTo(i + 1));
            Assert.That(_attempt.IsWallTraversed(i), Is.True);
            Assert.That(_attempt.IsComplete, Is.EqualTo(i == 3));
        }

        Assert.That(_attempt.TryMove("B"), Is.False);
        Assert.That(_attempt.CurrentTower, Is.EqualTo("A"));
        Assert.That(_attempt.History.Count, Is.EqualTo(4));
        Assert.That(_attempt.IsComplete, Is.True);
    }

    [Test]
    public void TryUndo_AtStart_DoesNotChangeState()
    {
        Assert.That(_attempt.CanUndo, Is.False);
        Assert.That(_attempt.TryUndo(), Is.False);
        Assert.That(_attempt.CurrentTower, Is.EqualTo("A"));
        Assert.That(_attempt.History, Is.Empty);
    }

    [Test]
    public void TryUndo_ReturnsToPreviousTowerAndFreesOnlyLastWall()
    {
        _attempt.TryMove("B");
        _attempt.TryMove("C");

        Assert.That(_attempt.CanUndo, Is.True);
        Assert.That(_attempt.TryUndo(), Is.True);
        Assert.That(_attempt.CurrentTower, Is.EqualTo("B"));
        Assert.That(_attempt.History.Count, Is.EqualTo(1));
        Assert.That(_attempt.IsWallTraversed(0), Is.True);
        Assert.That(_attempt.IsWallTraversed(1), Is.False);
        Assert.That(_attempt.TryMove("C"), Is.True);

        Assert.That(_attempt.TryUndo(), Is.True);
        Assert.That(_attempt.TryUndo(), Is.True);
        Assert.That(_attempt.CurrentTower, Is.EqualTo("A"));
        Assert.That(_attempt.CanUndo, Is.False);
    }

    [Test]
    public void LastMove_FreezesTimeRouteAndReward()
    {
        LevelAttempt attempt = new("A", _shortRouteWalls, 10);

        Assert.That(attempt.TryUndo(), Is.False);
        Assert.That(attempt.TryMove("C"), Is.False);
        Assert.That(attempt.TryAddElapsedTime(12), Is.True);
        Assert.That(attempt.TryMove("B"), Is.True);
        Assert.That(attempt.TryAddElapsedTime(17), Is.True);
        Assert.That(attempt.Reward, Is.Null);

        Assert.That(attempt.TryMove("C"), Is.True);
        Assert.That(attempt.IsComplete, Is.True);
        Assert.That(attempt.Reward.Value.Total, Is.EqualTo(20));
        Assert.That(attempt.CanUndo, Is.False);
        Assert.That(attempt.TryUndo(), Is.False);
        Assert.That(attempt.TryMove("B"), Is.False);
        Assert.That(attempt.TryRecordHintUse(), Is.False);
        Assert.That(attempt.TryAddElapsedTime(2), Is.False);
        Assert.That(attempt.ElapsedSeconds, Is.EqualTo(29));
        Assert.That(attempt.History.Count, Is.EqualTo(2));
        Assert.That(attempt.CurrentTower, Is.EqualTo("C"));
        Assert.That(attempt.UsedUndo, Is.False);
        Assert.That(attempt.UsedHint, Is.False);
        Assert.That(attempt.Reward.Value.Total, Is.EqualTo(20));
    }

    [Test]
    public void Undo_StillReducesRewardAfterRetraversingWall()
    {
        LevelAttempt attempt = new("A", _shortRouteWalls, 10);

        Assert.That(attempt.TryMove("B"), Is.True);
        Assert.That(attempt.TryUndo(), Is.True);
        Assert.That(attempt.IsWallTraversed(0), Is.False);
        Assert.That(attempt.TryMove("B"), Is.True);
        Assert.That(attempt.TryMove("C"), Is.True);

        Assert.That(attempt.UsedUndo, Is.True);
        Assert.That(attempt.Reward.Value.Total, Is.EqualTo(15));
    }

    [Test]
    public void NewAttempt_ResetsRouteTimeAndBonusFlags()
    {
        LevelAttempt first = new("A", _shortRouteWalls, 10);
        Assert.That(first.TryAddElapsedTime(25), Is.True);
        Assert.That(first.TryMove("B"), Is.True);
        Assert.That(first.TryUndo(), Is.True);
        Assert.That(first.TryMove("B"), Is.True);
        Assert.That(first.TryRecordHintUse(), Is.True);
        Assert.That(first.TryMove("C"), Is.True);
        Assert.That(first.Reward.Value.Total, Is.EqualTo(10));

        LevelAttempt next = new("A", _shortRouteWalls, 10);
        Assert.That(next.CurrentTower, Is.EqualTo("A"));
        Assert.That(next.History, Is.Empty);
        Assert.That(next.ElapsedSeconds, Is.Zero);
        Assert.That(next.UsedUndo, Is.False);
        Assert.That(next.UsedHint, Is.False);
        Assert.That(next.Reward, Is.Null);
        Assert.That(next.TryMove("B"), Is.True);
        Assert.That(next.TryMove("C"), Is.True);
        Assert.That(next.Reward.Value.Total, Is.EqualTo(20));
    }
}
