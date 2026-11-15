using System;
using CastleGuard.Progress;
using CastleGuard.Progress.Persistence;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CastleGuard.Tests.EditMode;

public class PlayerPrefsPlayerProgressStoreTests
{
    private string _levelKey;
    private string _coinKey;
    private PlayerPrefsPlayerProgressStore _store;

    [SetUp]
    public void SetUp()
    {
        _levelKey = $"CastleGuard.Tests.Progress.{Guid.NewGuid():N}";
        _coinKey = $"{_levelKey}.Coins";
        _store = new PlayerPrefsPlayerProgressStore(_levelKey, _coinKey);
    }

    [TearDown]
    public void TearDown()
    {
        PlayerPrefs.DeleteKey(_levelKey);
        PlayerPrefs.DeleteKey(_coinKey);
        PlayerPrefs.Save();
    }

    [Test]
    public void MissingSave_ReturnsFirstLevelAndZeroCoinsWithoutWriting()
    {
        Assert.That(_store.LoadLevel(), Is.EqualTo(1));
        Assert.That(_store.LoadCoins(), Is.Zero);
        Assert.That(PlayerPrefs.HasKey(_levelKey), Is.False);
        Assert.That(PlayerPrefs.HasKey(_coinKey), Is.False);
    }

    [TestCase(1, 0)]
    [TestCase(7, 123)]
    [TestCase(int.MaxValue, int.MaxValue)]
    public void Save_RestoresBothValuesForAnotherStore(int level, int coins)
    {
        _store.SaveLevel(level);
        _store.SaveCoins(coins);
        PlayerPrefsPlayerProgressStore reopened = new(_levelKey, _coinKey);
        Assert.That(reopened.LoadLevel(), Is.EqualTo(level));
        Assert.That(reopened.LoadCoins(), Is.EqualTo(coins));
    }

    [Test]
    public void SaveCoins_WritesOnlyCoinKey()
    {
        _store.SaveCoins(25);

        Assert.That(PlayerPrefs.GetInt(_coinKey), Is.EqualTo(25));
        Assert.That(PlayerPrefs.HasKey(_levelKey), Is.False);
    }

    [Test]
    public void SaveLevel_WritesOnlyLevelKey()
    {
        _store.SaveLevel(3);

        Assert.That(PlayerPrefs.GetInt(_levelKey), Is.EqualTo(3));
        Assert.That(PlayerPrefs.HasKey(_coinKey), Is.False);
    }

    [Test]
    public void ExistingSeparateKeys_AreLoadedTogether()
    {
        PlayerPrefs.SetInt(_levelKey, 7);
        PlayerPrefs.SetInt(_coinKey, 35);
        PlayerProgress progress = new(_store);
        Assert.That(progress.LevelNumber, Is.EqualTo(7));
        Assert.That(progress.Wallet.Balance, Is.EqualTo(35));
    }

    [TestCase("-1")]
    [TestCase("2147483648")]
    [TestCase("invalid")]
    [TestCase("")]
    [TestCase("1.5")]
    public void CorruptCoins_AreRepairedWithoutLosingLevel(string saved)
    {
        _store.SaveLevel(7);
        _store.SaveCoins(30);
        PlayerPrefs.SetString(_coinKey, saved);
        LogAssert.Expect(LogType.Warning, "Invalid saved coin balance. Restoring 0.");
        Assert.That(_store.LoadLevel(), Is.EqualTo(7));
        Assert.That(_store.LoadCoins(), Is.Zero);
        Assert.That(_store.LoadCoins(), Is.Zero);
        Assert.That(PlayerPrefs.GetInt(_coinKey, -1), Is.Zero);
    }

    [TestCase(0)]
    [TestCase(-10)]
    public void InvalidLevel_IsRepairedWithoutLosingCoins(int level)
    {
        _store.SaveLevel(7);
        _store.SaveCoins(30);
        PlayerPrefs.SetInt(_levelKey, level);
        LogAssert.Expect(LogType.Warning, "Invalid saved level number. Restoring 1.");
        Assert.That(_store.LoadLevel(), Is.EqualTo(1));
        Assert.That(_store.LoadCoins(), Is.EqualTo(30));
        Assert.That(PlayerPrefs.GetInt(_levelKey), Is.EqualTo(1));
    }

    [Test]
    public void NegativeCoins_AreRepaired()
    {
        PlayerPrefs.SetInt(_coinKey, -1);
        LogAssert.Expect(LogType.Warning, "Invalid saved coin balance. Restoring 0.");
        Assert.That(_store.LoadCoins(), Is.Zero);
    }

    [Test]
    public void InvalidValues_AreRejectedWithoutChangingSave()
    {
        _store.SaveLevel(3);
        _store.SaveCoins(7);
        Assert.Throws<ArgumentOutOfRangeException>(() => _store.SaveLevel(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => _store.SaveCoins(-1));
        Assert.That(_store.LoadLevel(), Is.EqualTo(3));
        Assert.That(_store.LoadCoins(), Is.EqualTo(7));
    }
}
