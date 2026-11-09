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
        Assert.That(service.TryPurchase(), Is.EqualTo(success));
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
        Assert.That(service.TryPurchase(), Is.True);
        Assert.That(service.TryPurchase(), Is.False);
        Assert.That(attempt.TryMove("D"), Is.False);
        Assert.That(service.TryPurchase(), Is.False);
        Assert.That(attempt.TryUndo(), Is.True);
        Assert.That(service.TryPurchase(), Is.False);
        Assert.That(attempt.TryUndo(), Is.False);
        Assert.That(service.TryPurchase(), Is.False);
        Assert.That(wallet.Balance, Is.EqualTo(15));
        Assert.That(attempt.TryMove("B"), Is.True);
        Assert.That(service.CanPurchase, Is.True);
        Assert.That(service.TryPurchase(), Is.True);
        Assert.That(wallet.Balance, Is.EqualTo(10));
        Assert.That(store.Saves, Is.EqualTo(2));
    }

    [Test]
    public void NewAttempt_UnlocksPurchaseWithoutRefund()
    {
        CoinWallet wallet = new(new MemoryStore { Balance = 15, });
        LevelAttempt first = CreateAttempt();
        Assert.That(new HintService(wallet, first, 5).TryPurchase(), Is.True);

        LevelAttempt next = CreateAttempt();
        HintService service = new(wallet, next, 5);
        Assert.That(next.UsedHint, Is.False);
        Assert.That(wallet.Balance, Is.EqualTo(10));
        Assert.That(service.TryPurchase(), Is.True);
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
        Assert.That(service.TryPurchase(), Is.False);
        Assert.That(wallet.Balance, Is.EqualTo(20));
        Assert.That(attempt.UsedHint, Is.False);
        Assert.That(attempt.Reward.Value.Total, Is.EqualTo(20));
    }

    [Test]
    public void DeadEnd_PurchaseIsAllowedWithoutMovingPlayer()
    {
        CoinWallet wallet = new(new MemoryStore { Balance = 5, });
        LevelAttempt attempt = new("A", new Wall[] { new("A", "B"), new("A", "C"), }, 10);
        attempt.TryMove("B");

        Assert.That(new HintService(wallet, attempt, 5).TryPurchase(), Is.True);
        Assert.That(attempt.CurrentTower, Is.EqualTo("B"));
        Assert.That(attempt.History.Count, Is.EqualTo(1));
        Assert.That(wallet.Balance, Is.Zero);
    }

    [Test]
    public void FailedSave_PreservesAttemptAndAllowsRetry()
    {
        MemoryStore store = new() { Balance = 5, FailSave = true, };
        CoinWallet wallet = new(store);
        LevelAttempt attempt = CreateAttempt();
        HintService service = new(wallet, attempt, 5);

        Assert.Throws<InvalidOperationException>(() => service.TryPurchase());
        Assert.That(wallet.Balance, Is.EqualTo(5));
        Assert.That(attempt.UsedHint, Is.False);
        Assert.That(service.CanPurchase, Is.True);
        store.FailSave = false;
        Assert.That(service.TryPurchase(), Is.True);
    }

    [Test]
    public void BalanceNotification_CannotPurchaseTwice()
    {
        MemoryStore store = new() { Balance = 20, };
        CoinWallet wallet = new(store);
        HintService service = new(wallet, CreateAttempt(), 5);
        wallet.BalanceChanged += _ => Assert.That(service.TryPurchase(), Is.False);

        Assert.That(service.TryPurchase(), Is.True);
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
