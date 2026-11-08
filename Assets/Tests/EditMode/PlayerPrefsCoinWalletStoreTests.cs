using CastleGuard.Economy.Persistence;
using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CastleGuard.Tests.EditMode;

public class PlayerPrefsCoinWalletStoreTests
{
    private string _key;
    private PlayerPrefsCoinWalletStore _store;

    [SetUp]
    public void SetUp()
    {
        _key = $"CastleGuard.Tests.Coins.{Guid.NewGuid():N}";
        _store = new PlayerPrefsCoinWalletStore(_key);
    }

    [TearDown]
    public void TearDown()
    {
        PlayerPrefs.DeleteKey(_key);
        PlayerPrefs.Save();
    }

    [Test]
    public void MissingSave_ReturnsZero()
    {
        Assert.That(_store.Load(), Is.Zero);
        Assert.That(PlayerPrefs.HasKey(_key), Is.False);
    }

    [TestCase(0)]
    [TestCase(123)]
    [TestCase(int.MaxValue)]
    public void Save_CanBeLoadedByAnotherStore(int balance)
    {
        _store.Save(balance);
        Assert.That(PlayerPrefs.GetInt(_key, -1), Is.EqualTo(balance));

        PlayerPrefsCoinWalletStore reopened = new(_key);
        Assert.That(reopened.Load(), Is.EqualTo(balance));
    }

    [TestCase("-1")]
    [TestCase("2147483648")]
    [TestCase("invalid")]
    [TestCase("")]
    [TestCase("1.5")]
    public void CorruptSave_LogsAndReplacesWithZero(string saved)
    {
        PlayerPrefs.SetString(_key, saved);
        LogAssert.Expect(LogType.Warning, "Invalid saved coin balance. Restoring 0.");

        Assert.That(_store.Load(), Is.Zero);
        Assert.That(_store.Load(), Is.Zero);
        Assert.That(PlayerPrefs.GetInt(_key, -1), Is.Zero);
    }

    [Test]
    public void NegativeSavedBalance_IsRecovered()
    {
        PlayerPrefs.SetInt(_key, -1);
        LogAssert.Expect(LogType.Warning, "Invalid saved coin balance. Restoring 0.");

        Assert.That(_store.Load(), Is.Zero);
    }

    [Test]
    public void NegativeSave_IsRejectedWithoutChangingSavedBalance()
    {
        _store.Save(7);

        Assert.Throws<ArgumentOutOfRangeException>(() => _store.Save(-1));
        Assert.That(_store.Load(), Is.EqualTo(7));
    }
}
