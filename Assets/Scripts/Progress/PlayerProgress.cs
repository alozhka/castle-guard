using System;
using CastleGuard.Economy;
using CastleGuard.Progress.Persistence;

namespace CastleGuard.Progress;

public sealed class PlayerProgress
{
    private readonly IPlayerProgressStore _store;

    public int LevelNumber { get; private set; }
    public CoinWallet Wallet { get; }

    public PlayerProgress(IPlayerProgressStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        LevelNumber = _store.LoadLevel();
        Wallet = new CoinWallet(_store);
    }

    public void CompleteLevel(int levelNumber)
    {
        if (levelNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(levelNumber));
        }

        if (levelNumber < LevelNumber)
        {
            return;
        }

        Save(checked(levelNumber + 1));
    }

    private void Save(int levelNumber)
    {
        _store.SaveLevel(levelNumber);
        LevelNumber = levelNumber;
    }
}
