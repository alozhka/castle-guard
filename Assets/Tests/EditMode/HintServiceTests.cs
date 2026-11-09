using CastleGuard.Economy;
using CastleGuard.Economy.Persistence;
using CastleGuard.Level;
using CastleGuard.Level.Model;
using NUnit.Framework;
using System;

namespace CastleGuard.Tests.EditMode;

public class HintServiceTests
{
    private sealed class MemoryStore : ICoinWalletStore
    {
        public int Balance;
        public int Saves;
        public bool FailSave;

        public int Load() => Balance;

        public void Save(int balance)
        {
            if (FailSave)
            {
                throw new InvalidOperationException();
            }

            Balance = balance;
            Saves++;
        }
    }

    private static LevelAttempt CreateAttempt() =>
        new("A", new Wall[] { new("A", "B"), new("B", "C"), new("C", "D"), }, 10);

    [TestCase(5, true, 0)]
    [TestCase(4, false, 4)]
    [TestCase(0, false, 0)]
    public void Purchase_ChecksFundsAndChangesBonusOnlyOnSuccess(int balance, bool success, int remaining)
    {
        MemoryStore store = new() { Balance = balance, };
        CoinWallet wallet = new(store);
        LevelAttempt attempt = CreateAttempt();
        HintService service = new(wallet, attempt, 5);

        Assert.That(service.CanPurchase, Is.EqualTo(success));
        HintResult result = service.Purchase();
        Assert.That(result.Status, Is.EqualTo(success ? HintStatus.Forward : HintStatus.Unavailable));
        Assert.That(result.Step, Is.EqualTo(success ? new Move("A", "B", 0) : (Move?)null));
        Assert.That(wallet.Balance, Is.EqualTo(remaining));
        Assert.That(store.Saves, Is.EqualTo(success ? 1 : 0));
        Assert.That(attempt.UsedHint, Is.EqualTo(success));
        Assert.That(attempt.CurrentTower, Is.EqualTo("A"));
        Assert.That(attempt.History, Is.Empty);
        attempt.TryMove("B");
        attempt.TryMove("C");
        attempt.TryMove("D");
        Assert.That(attempt.Reward.Value.Total, Is.EqualTo(success ? 10 : 20));
    }

    [Test]
    public void RepeatPurchase_RequiresSuccessfulMove_NotInvalidTapOrUndo()
    {
        MemoryStore store = new() { Balance = 20, };
        CoinWallet wallet = new(store);
        LevelAttempt attempt = CreateAttempt();
        HintService service = new(wallet, attempt, 5);

        attempt.TryMove("B");
        Assert.That(service.Purchase().Status, Is.EqualTo(HintStatus.Forward));
        Assert.That(service.Purchase().Status, Is.EqualTo(HintStatus.Unavailable));
        Assert.That(attempt.TryMove("D"), Is.False);
        Assert.That(service.Purchase().Status, Is.EqualTo(HintStatus.Unavailable));
        Assert.That(attempt.TryUndo(), Is.True);
        Assert.That(service.Purchase().Status, Is.EqualTo(HintStatus.Unavailable));
        Assert.That(attempt.TryUndo(), Is.False);
        Assert.That(service.Purchase().Status, Is.EqualTo(HintStatus.Unavailable));
        Assert.That(wallet.Balance, Is.EqualTo(15));
        Assert.That(attempt.TryMove("B"), Is.True);
        Assert.That(service.CanPurchase, Is.True);
        Assert.That(service.Purchase().Status, Is.EqualTo(HintStatus.Forward));
        Assert.That(wallet.Balance, Is.EqualTo(10));
        Assert.That(store.Saves, Is.EqualTo(2));
    }

    [Test]
    public void NewAttempt_UnlocksPurchaseWithoutRefund()
    {
        CoinWallet wallet = new(new MemoryStore { Balance = 15, });
        LevelAttempt first = CreateAttempt();
        Assert.That(new HintService(wallet, first, 5).Purchase().Status, Is.EqualTo(HintStatus.Forward));

        LevelAttempt next = CreateAttempt();
        HintService service = new(wallet, next, 5);
        Assert.That(next.UsedHint, Is.False);
        Assert.That(wallet.Balance, Is.EqualTo(10));
        Assert.That(service.Purchase().Status, Is.EqualTo(HintStatus.Forward));
        Assert.That(wallet.Balance, Is.EqualTo(5));
    }

    [Test]
    public void Victory_RejectsPurchaseAndPreservesReward()
    {
        CoinWallet wallet = new(new MemoryStore { Balance = 20, });
        LevelAttempt attempt = CreateAttempt();
        HintService service = new(wallet, attempt, 5);
        attempt.TryMove("B");
        attempt.TryMove("C");
        attempt.TryMove("D");

        Assert.That(service.CanPurchase, Is.False);
        Assert.That(service.Purchase().Status, Is.EqualTo(HintStatus.Unavailable));
        Assert.That(wallet.Balance, Is.EqualTo(20));
        Assert.That(attempt.UsedHint, Is.False);
        Assert.That(attempt.Reward.Value.Total, Is.EqualTo(20));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void DeadEnd_PurchaseSuggestsUndoWithoutMovingPlayer(bool hasFreeNeighbor)
    {
        MemoryStore store = new() { Balance = 15, };
        CoinWallet wallet = new(store);
        Wall[] walls = hasFreeNeighbor
            ? new Wall[] { new("A", "B"), new("B", "E"), new("E", "F"), new("F", "B"), new("A", "C"), new("C", "D"), new("D", "A"), }
            : new Wall[] { new("A", "B"), new("A", "C"), new("C", "D"), new("D", "A"), };
        LevelAttempt attempt = new("A", walls, 10);
        attempt.TryMove("B");
        HintService service = new(wallet, attempt, 5);

        HintResult result = service.Purchase();
        Assert.That(result.Status, Is.EqualTo(HintStatus.Undo));
        Assert.That(result.Step, Is.EqualTo(new Move("B", "A", 0)));
        Assert.That(attempt.CurrentTower, Is.EqualTo("B"));
        Assert.That(attempt.History.Count, Is.EqualTo(1));
        Assert.That(attempt.IsWallTraversed(0), Is.True);
        Assert.That(attempt.UsedHint, Is.True);
        Assert.That(service.Purchase().Status, Is.EqualTo(HintStatus.Unavailable));
        Assert.That(attempt.TryUndo(), Is.True);
        Assert.That(service.Purchase().Status, Is.EqualTo(HintStatus.Unavailable));
        Assert.That(wallet.Balance, Is.EqualTo(10));
        Assert.That(store.Saves, Is.EqualTo(1));
    }

    [Test]
    public void FailedSave_PreservesAttemptAndAllowsRetry()
    {
        MemoryStore store = new() { Balance = 5, FailSave = true, };
        CoinWallet wallet = new(store);
        LevelAttempt attempt = CreateAttempt();
        HintService service = new(wallet, attempt, 5);

        Assert.Throws<InvalidOperationException>(() => service.Purchase());
        Assert.That(wallet.Balance, Is.EqualTo(5));
        Assert.That(attempt.UsedHint, Is.False);
        Assert.That(service.CanPurchase, Is.True);
        store.FailSave = false;
        Assert.That(service.Purchase().Status, Is.EqualTo(HintStatus.Forward));
    }

    [Test]
    public void BalanceNotification_CannotPurchaseTwice()
    {
        MemoryStore store = new() { Balance = 20, };
        CoinWallet wallet = new(store);
        HintService service = new(wallet, CreateAttempt(), 5);
        wallet.BalanceChanged += _ => Assert.That(service.Purchase().Status, Is.EqualTo(HintStatus.Unavailable));

        Assert.That(service.Purchase().Status, Is.EqualTo(HintStatus.Forward));
        Assert.That(wallet.Balance, Is.EqualTo(15));
        Assert.That(store.Saves, Is.EqualTo(1));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void NonpositivePrice_IsRejected(int price)
    {
        CoinWallet wallet = new(new MemoryStore());
        Assert.Throws<ArgumentOutOfRangeException>(() => new HintService(wallet, CreateAttempt(), price));
    }
}
