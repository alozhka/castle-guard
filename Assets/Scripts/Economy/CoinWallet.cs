using System;
using CastleGuard.Progress.Persistence;

namespace CastleGuard.Economy;

public sealed class CoinWallet
{
    private readonly IPlayerProgressStore _store;

    public int Balance { get; private set; }
    public event Action<int> BalanceChanged;

    public CoinWallet(IPlayerProgressStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        int balance = _store.LoadCoins();
        if (balance < 0)
        {
            _store.SaveCoins(0);
            balance = 0;
        }

        Balance = balance;
    }

    public void Credit(decimal amount)
    {
        ValidateAmount(amount);
        int coins = checked((int)decimal.Ceiling(amount));
        SetBalance(checked(Balance + coins));
    }

    public bool TrySpend(int amount)
    {
        ValidateAmount(amount);
        if (amount > Balance)
        {
            return false;
        }

        SetBalance(Balance - amount);
        return true;
    }

    private static void ValidateAmount(decimal amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Coin amount must be nonnegative.");
        }
    }

    private void SetBalance(int balance)
    {
        if (balance == Balance)
        {
            return;
        }

        _store.SaveCoins(balance);
        Balance = balance;
        BalanceChanged?.Invoke(balance);
    }
}
