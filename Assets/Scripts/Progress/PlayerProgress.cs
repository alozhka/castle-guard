using System;
using UnityEngine;

namespace CastleGuard.Progress;

public sealed class PlayerProgress
{
    public const string DefaultKey = "CastleGuard.Progress.LevelNumber";

    private readonly string _key;

    public int LevelNumber { get; private set; }

    public PlayerProgress(string key = DefaultKey)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("A progress save key is required.", nameof(key));
        }

        _key = key;
        LevelNumber = PlayerPrefs.GetInt(_key, 1);
        if (LevelNumber < 1)
        {
            Debug.LogWarning("Invalid saved level number. Restoring 1.");
            Save(1);
        }
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
        PlayerPrefs.SetInt(_key, levelNumber);
        PlayerPrefs.Save();
        LevelNumber = levelNumber;
    }
}
