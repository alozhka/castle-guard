using CastleGuard.Level.Model;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CastleGuard.Tests.EditMode;

public class RouteSolverTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void Solve_CoversRemainingWallsWithoutChangingAttempt(bool afterMove)
    {
        LevelAttempt attempt = new("A", new Wall[]
        {
            new("A", "D"),
            new("B", "A"),
            new("B", "C"),
            new("C", "A"),
        }, 10);
        if (afterMove)
        {
            attempt.TryMove("B");
        }

        attempt.TryAddElapsedTime(12);
        attempt.TryRecordHintUse();
        AssertUnchangedBySearch(attempt, true, out IReadOnlyList<Move> route);
        Assert.That(route.Select(move => move.WallIndex), Is.EqualTo(afterMove
            ? new[] { 2, 3, 0, }
            : new[] { 1, 2, 3, 0, }));
        AssertRouteCompletesAttempt(attempt, route);
    }

    [Test]
    public void Solve_AfterUndoIncludesRestoredWall()
    {
        LevelAttempt attempt = new("A", new Wall[] { new("A", "B"), new("B", "C"), }, 10);
        attempt.TryMove("B");
        attempt.TryUndo();

        AssertUnchangedBySearch(attempt, true, out IReadOnlyList<Move> route);
        AssertRouteCompletesAttempt(attempt, route);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Solve_DeadEndWithOrWithoutFreeNeighborReturnsEmptyRoute(bool hasFreeNeighbor)
    {
        Wall[] walls = hasFreeNeighbor
            ? new Wall[] { new("A", "B"), new("B", "E"), new("E", "F"), new("F", "B"), new("A", "C"), new("C", "D"), new("D", "A"), }
            : new Wall[] { new("A", "B"), new("A", "C"), new("C", "D"), new("D", "A"), };
        LevelAttempt attempt = new("A", walls, 10);
        attempt.TryMove("B");

        AssertUnchangedBySearch(attempt, false, out IReadOnlyList<Move> route);
        Assert.That(route, Is.Empty);
    }

    [Test]
    public void Solve_ParallelWallsUseOriginalIndices()
    {
        LevelAttempt attempt = new("A", new Wall[] { new("A", "B"), new("B", "A"), }, 10);

        AssertUnchangedBySearch(attempt, true, out IReadOnlyList<Move> route);
        AssertRouteCompletesAttempt(attempt, route);
    }

    [Test]
    public void Solve_CompletedAttemptReturnsEmptySolution()
    {
        LevelAttempt attempt = new("A", Array.Empty<Wall>(), 10);

        AssertUnchangedBySearch(attempt, true, out IReadOnlyList<Move> route);
        Assert.That(route, Is.Empty);
    }

    [Test]
    public void Solve_SelfLoopCannotBeTraversed()
    {
        LevelAttempt attempt = new("A", new Wall[] { new("A", "A"), }, 10);

        AssertUnchangedBySearch(attempt, false, out IReadOnlyList<Move> route);
        Assert.That(route, Is.Empty);
    }

    [Test]
    public void Solve_NullAttemptIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => RouteSolver.TrySolve(null, out _));
    }

    private static void AssertUnchangedBySearch(LevelAttempt attempt, bool expected, out IReadOnlyList<Move> route)
    {
        object[] before = Snapshot(attempt);
        Assert.That(RouteSolver.TrySolve(attempt, out route), Is.EqualTo(expected));
        Assert.That(Snapshot(attempt), Is.EqualTo(before));
        Move[] originalRoute = route.ToArray();
        Assert.That(RouteSolver.TrySolve(attempt, out IReadOnlyList<Move> repeated), Is.EqualTo(expected));
        Assert.That(repeated, Is.EqualTo(originalRoute));
        Assert.That(route, Is.EqualTo(originalRoute));
        Assert.That(Snapshot(attempt), Is.EqualTo(before));
    }

    private static object[] Snapshot(LevelAttempt attempt) => new object[]
    {
        attempt.CurrentTower,
        attempt.History.ToArray(),
        Enumerable.Range(0, attempt.Walls.Count).Select(attempt.IsWallTraversed).ToArray(),
        attempt.ElapsedSeconds,
        attempt.UsedHint,
        attempt.UsedUndo,
        attempt.CanUseHint,
        attempt.Reward,
    };

    private static void AssertRouteCompletesAttempt(LevelAttempt attempt, IReadOnlyList<Move> route)
    {
        int[] remaining = Enumerable.Range(0, attempt.Walls.Count)
            .Where(index => !attempt.IsWallTraversed(index)).ToArray();
        Assert.That(route.Select(move => move.WallIndex), Is.EquivalentTo(remaining));
        foreach (Move move in route)
        {
            Assert.That(move.From, Is.EqualTo(attempt.CurrentTower));
            Assert.That(attempt.Walls[move.WallIndex].Connects(move.From, move.To), Is.True);
            Assert.That(attempt.TryMove(move.To), Is.True);
            Assert.That(attempt.History[^1].WallIndex, Is.EqualTo(move.WallIndex));
        }

        Assert.That(attempt.IsComplete, Is.True);
    }
}
