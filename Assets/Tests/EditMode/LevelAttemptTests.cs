using CastleGuard.Level.Model;
using NUnit.Framework;

namespace CastleGuard.Tests.EditMode;

public class LevelAttemptTests
{
    private static readonly Wall[] Walls = { new("A", "B"), new("B", "C"), };

    [Test]
    public void LastMove_FreezesTimeRouteAndReward()
    {
        LevelAttempt attempt = new("A", Walls, 10);

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
        LevelAttempt attempt = new("A", Walls, 10);

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
        LevelAttempt first = new("A", Walls, 10);
        Assert.That(first.TryAddElapsedTime(25), Is.True);
        Assert.That(first.TryMove("B"), Is.True);
        Assert.That(first.TryUndo(), Is.True);
        Assert.That(first.TryMove("B"), Is.True);
        Assert.That(first.TryRecordHintUse(), Is.True);
        Assert.That(first.TryMove("C"), Is.True);
        Assert.That(first.Reward.Value.Total, Is.EqualTo(10));

        LevelAttempt next = new("A", Walls, 10);
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
