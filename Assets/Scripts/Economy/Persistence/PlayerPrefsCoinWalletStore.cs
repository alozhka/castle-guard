using System;
using UnityEngine;

namespace CastleGuard.Economy.Persistence;

public sealed class PlayerPrefsCoinWalletStore : ICoinWalletStore
{
    public const string DefaultKey = "CastleGuard.Economy.CoinBalance";

    private readonly string _key;

    public PlayerPrefsCoinWalletStore(string key = DefaultKey)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("A wallet save key is required.", nameof(key));
        }

        _key = key;
    }

    public int Load()
    {
        if (!PlayerPrefs.HasKey(_key))
        {
            return 0;
        }

        int balance = PlayerPrefs.GetInt(_key, -1);
        if (balance >= 0)
        {
            return balance;
        }

        Debug.LogWarning("Invalid saved coin balance. Restoring 0.");
        Save(0);
        return 0;
    }

    public void Save(int balance)
    {
        if (balance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(balance));
        }

        PlayerPrefs.SetInt(_key, balance);
        PlayerPrefs.Save();
    }
}
