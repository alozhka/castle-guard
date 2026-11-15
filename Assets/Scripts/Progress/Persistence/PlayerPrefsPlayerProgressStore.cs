using System;
using UnityEngine;

namespace CastleGuard.Progress.Persistence;

public sealed class PlayerPrefsPlayerProgressStore : IPlayerProgressStore
{
    private const string DefaultLevelKey = "CastleGuard.Progress.LevelNumber";
    private const string DefaultCoinKey = "CastleGuard.Economy.CoinBalance";

    private readonly string _levelKey;
    private readonly string _coinKey;

    public PlayerPrefsPlayerProgressStore(string levelKey = DefaultLevelKey, string coinKey = DefaultCoinKey)
    {
        if (string.IsNullOrWhiteSpace(levelKey))
        {
            throw new ArgumentException("A level save key is required.", nameof(levelKey));
        }

        if (string.IsNullOrWhiteSpace(coinKey) || coinKey == levelKey)
        {
            throw new ArgumentException("A separate coin save key is required.", nameof(coinKey));
        }

        _levelKey = levelKey;
        _coinKey = coinKey;
    }

    public int LoadLevel()
    {
        int levelNumber = PlayerPrefs.HasKey(_levelKey) ? PlayerPrefs.GetInt(_levelKey, 0) : 1;
        if (levelNumber < 1)
        {
            Debug.LogWarning("Invalid saved level number. Restoring 1.");
            levelNumber = 1;
            SaveLevel(levelNumber);
        }

        return levelNumber;
    }

    public int LoadCoins()
    {
        int balance = PlayerPrefs.HasKey(_coinKey) ? PlayerPrefs.GetInt(_coinKey, -1) : 0;
        if (balance < 0)
        {
            Debug.LogWarning("Invalid saved coin balance. Restoring 0.");
            balance = 0;
            SaveCoins(balance);
        }

        return balance;
    }

    public void SaveLevel(int levelNumber)
    {
        if (levelNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(levelNumber));
        }

        PlayerPrefs.SetInt(_levelKey, levelNumber);
        PlayerPrefs.Save();
    }

    public void SaveCoins(int balance)
    {
        if (balance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(balance));
        }

        PlayerPrefs.SetInt(_coinKey, balance);
        PlayerPrefs.Save();
    }
}
