using CastleGuard.Economy;
using CastleGuard.Economy.Persistence;
using System;
using NUnit.Framework;

namespace CastleGuard.Tests.EditMode;

public class CoinWalletTests
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
                throw new InvalidOperationException("Save failed.");
            }

            Balance = balance;
            Saves++;
        }
    }

    [Test]
    public void NewWallet_StartsAtZero()
    {
        MemoryStore store = new();
        CoinWallet wallet = new(store);

        Assert.That(wallet.Balance, Is.Zero);
        Assert.That(store.Saves, Is.Zero);
    }

    [Test]
    public void Changes_SaveAndNotifyAndSurviveRecreation()
    {
        MemoryStore store = new() { Balance = 4, };
        CoinWallet wallet = new(store);
        int notifications = 0;
        wallet.BalanceChanged += balance =>
        {
            notifications++;
            Assert.That(store.Balance, Is.EqualTo(balance));
            Assert.That(wallet.Balance, Is.EqualTo(balance));
        };

        wallet.Credit(10);
        Assert.That(wallet.TrySpend(5), Is.True);

        Assert.That(wallet.Balance, Is.EqualTo(9));
        Assert.That(new CoinWallet(store).Balance, Is.EqualTo(9));
        Assert.That(store.Saves, Is.EqualTo(2));
        Assert.That(notifications, Is.EqualTo(2));
    }

    [Test]
    public void Spend_AllCoins_Succeeds()
    {
        CoinWallet wallet = new(new MemoryStore { Balance = 5, });

        Assert.That(wallet.TrySpend(5), Is.True);
        Assert.That(wallet.Balance, Is.Zero);
    }

    [Test]
    public void InsufficientFundsAndZeroAmounts_DoNotSaveOrNotify()
    {
        MemoryStore store = new() { Balance = 4, };
        CoinWallet wallet = new(store);
        wallet.BalanceChanged += _ => Assert.Fail("No change expected.");

        Assert.That(wallet.TrySpend(5), Is.False);
        Assert.That(wallet.TrySpend(0), Is.True);
        wallet.Credit(0);

        Assert.That(wallet.Balance, Is.EqualTo(4));
        Assert.That(store.Saves, Is.Zero);
    }

    [Test]
    public void NegativeAmountsAndOverflow_LeaveBalanceAndStoreUnchanged()
    {
        MemoryStore store = new() { Balance = int.MaxValue, };
        CoinWallet wallet = new(store);

        Assert.Throws<ArgumentOutOfRangeException>(() => wallet.Credit(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => wallet.TrySpend(-1));
        Assert.Throws<OverflowException>(() => wallet.Credit(1));

        Assert.That(wallet.Balance, Is.EqualTo(int.MaxValue));
        Assert.That(store.Saves, Is.Zero);
    }

    [Test]
    public void InvalidSavedBalance_RestoresZero()
    {
        MemoryStore store = new() { Balance = -1, };

        CoinWallet wallet = new(store);

        Assert.That(wallet.Balance, Is.Zero);
        Assert.That(store.Balance, Is.Zero);
        Assert.That(store.Saves, Is.EqualTo(1));
    }

    [TestCase("0", 0)]
    [TestCase("0.1", 1)]
    [TestCase("7.1", 8)]
    [TestCase("7.5", 8)]
    [TestCase("8", 8)]
    [TestCase("2147483647", int.MaxValue)]
    public void Credit_RoundsUpToWholeCoins(string amount, int expected)
    {
        MemoryStore store = new();
        CoinWallet wallet = new(store);

        wallet.Credit(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture));

        Assert.That(wallet.Balance, Is.EqualTo(expected));
        Assert.That(store.Balance, Is.EqualTo(expected));
    }

    [TestCase("-0.1")]
    [TestCase("2147483647.1")]
    [TestCase("2147483648")]
    public void Credit_InvalidFractionalAmount_DoesNotChangeBalanceOrSave(string amount)
    {
        MemoryStore store = new();
        CoinWallet wallet = new(store);
        decimal coins = decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture);

        if (coins < 0)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => wallet.Credit(coins));
        }
        else
        {
            Assert.Throws<OverflowException>(() => wallet.Credit(coins));
        }

        Assert.That(wallet.Balance, Is.Zero);
        Assert.That(store.Saves, Is.Zero);
    }

    [Test]
    public void Credit_RoundedAmountOverflow_DoesNotChangeBalanceOrSave()
    {
        MemoryStore store = new() { Balance = int.MaxValue - 1, };
        CoinWallet wallet = new(store);

        Assert.Throws<OverflowException>(() => wallet.Credit(1.1m));

        Assert.That(wallet.Balance, Is.EqualTo(int.MaxValue - 1));
        Assert.That(store.Saves, Is.Zero);
    }

    [Test]
    public void FailedSave_DoesNotChangeBalanceOrNotify()
    {
        MemoryStore store = new() { Balance = 5, FailSave = true, };
        CoinWallet wallet = new(store);
        wallet.BalanceChanged += _ => Assert.Fail("Failed save must not notify.");

        Assert.Throws<InvalidOperationException>(() => wallet.Credit(1));
        Assert.Throws<InvalidOperationException>(() => wallet.TrySpend(1));
        Assert.That(wallet.Balance, Is.EqualTo(5));
    }
}
