using System;
using CastleGuard.Progress.Persistence;

namespace CastleGuard.Tests.EditMode;

internal sealed class MemoryPlayerProgressStore : IPlayerProgressStore
{
    public int LevelNumber { get; set; } = 1;
    public int Balance { get; set; }
    public int LevelSaves { get; private set; }
    public int CoinSaves { get; private set; }
    public int Saves => LevelSaves + CoinSaves;
    public bool FailSave { get; set; }

    public int LoadLevel() => LevelNumber;
    public int LoadCoins() => Balance;

    public void SaveLevel(int levelNumber)
    {
        ThrowIfSaveFails();
        LevelNumber = levelNumber;
        LevelSaves++;
    }

    public void SaveCoins(int balance)
    {
        ThrowIfSaveFails();
        Balance = balance;
        CoinSaves++;
    }

    private void ThrowIfSaveFails()
    {
        if (FailSave)
        {
            throw new InvalidOperationException("Save failed.");
        }
    }
}
