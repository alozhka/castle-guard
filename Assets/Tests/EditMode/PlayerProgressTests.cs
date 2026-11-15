using System;
using CastleGuard.Progress;
using NUnit.Framework;

namespace CastleGuard.Tests.EditMode;

public class PlayerProgressTests
{
    [Test]
    public void NewPlayer_StartsAtFirstLevelWithNoCoins()
    {
        PlayerProgress progress = new(new MemoryPlayerProgressStore());
        Assert.That(progress.LevelNumber, Is.EqualTo(1));
        Assert.That(progress.Wallet.Balance, Is.Zero);
    }

    [Test]
    public void LevelAndCoins_AreRestoredForAnotherSession()
    {
        MemoryPlayerProgressStore store = new();
        PlayerProgress progress = new(store);
        progress.CompleteLevel(2);
        progress.Wallet.Credit(25);
        progress.Wallet.TrySpend(10);

        PlayerProgress reopened = new(store);
        Assert.That(reopened.LevelNumber, Is.EqualTo(3));
        Assert.That(reopened.Wallet.Balance, Is.EqualTo(15));
        reopened.CompleteLevel(3);
        Assert.That(store.LevelNumber, Is.EqualTo(4));
        Assert.That(store.Balance, Is.EqualTo(15));
    }

    [Test]
    public void Changes_SaveLevelAndCoinsSeparately()
    {
        MemoryPlayerProgressStore store = new();
        PlayerProgress progress = new(store);

        progress.Wallet.Credit(10);
        Assert.That(store.CoinSaves, Is.EqualTo(1));
        Assert.That(store.LevelSaves, Is.Zero);

        progress.CompleteLevel(1);
        Assert.That(store.LevelSaves, Is.EqualTo(1));
        Assert.That(store.CoinSaves, Is.EqualTo(1));
    }

    [Test]
    public void RepeatedVictory_DoesNotAdvanceOrLoseProgress()
    {
        MemoryPlayerProgressStore store = new();
        PlayerProgress progress = new(store);
        progress.CompleteLevel(5);
        progress.CompleteLevel(5);
        progress.CompleteLevel(1);
        Assert.That(progress.LevelNumber, Is.EqualTo(6));
        Assert.That(new PlayerProgress(store).LevelNumber, Is.EqualTo(6));
        Assert.That(store.Saves, Is.EqualTo(1));
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(int.MinValue)]
    public void InvalidCompletedLevel_DoesNotChangeSave(int levelNumber)
    {
        MemoryPlayerProgressStore store = new();
        PlayerProgress progress = new(store);
        progress.CompleteLevel(3);
        Assert.Throws<ArgumentOutOfRangeException>(() => progress.CompleteLevel(levelNumber));
        Assert.That(new PlayerProgress(store).LevelNumber, Is.EqualTo(4));
    }

    [Test]
    public void BalanceNotification_ObservesSavedLevelAndCoins()
    {
        MemoryPlayerProgressStore store = new();
        PlayerProgress progress = new(store);
        progress.CompleteLevel(2);
        progress.Wallet.BalanceChanged += balance =>
        {
            Assert.That(store.LevelNumber, Is.EqualTo(3));
            Assert.That(store.Balance, Is.EqualTo(balance));
        };
        progress.Wallet.Credit(10);
    }

    [Test]
    public void FailedSave_LeavesLevelAndBalanceUnchanged()
    {
        MemoryPlayerProgressStore store = new();
        PlayerProgress progress = new(store);
        progress.Wallet.Credit(10);
        store.FailSave = true;
        progress.Wallet.BalanceChanged += _ => Assert.Fail("Failed save must not notify.");
        Assert.Throws<InvalidOperationException>(() => progress.CompleteLevel(1));
        Assert.Throws<InvalidOperationException>(() => progress.Wallet.Credit(1));
        Assert.Throws<InvalidOperationException>(() => progress.Wallet.TrySpend(1));
        Assert.That(progress.LevelNumber, Is.EqualTo(1));
        Assert.That(progress.Wallet.Balance, Is.EqualTo(10));
    }
}
