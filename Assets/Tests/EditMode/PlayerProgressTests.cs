using CastleGuard.Progress;
using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CastleGuard.Tests.EditMode;

public class PlayerProgressTests
{
    private string _key;

    [SetUp]
    public void SetUp()
    {
        _key = $"CastleGuard.Tests.Progress.{Guid.NewGuid():N}";
    }

    [TearDown]
    public void TearDown()
    {
        PlayerPrefs.DeleteKey(_key);
        PlayerPrefs.Save();
    }

    [Test]
    public void NewPlayer_StartsAtFirstLevel()
    {
        PlayerProgress progress = new(_key);

        Assert.That(progress.LevelNumber, Is.EqualTo(1));
    }

    [Test]
    public void Victory_PersistsNextLevelForAnotherSession()
    {
        PlayerProgress progress = new(_key);
        progress.CompleteLevel(1);

        PlayerProgress reopened = new(_key);
        Assert.That(reopened.LevelNumber, Is.EqualTo(2));
        reopened.CompleteLevel(2);
        Assert.That(new PlayerProgress(_key).LevelNumber, Is.EqualTo(3));
    }

    [Test]
    public void RepeatedVictory_DoesNotAdvanceOrLoseProgress()
    {
        PlayerProgress progress = new(_key);
        progress.CompleteLevel(5);
        progress.CompleteLevel(5);
        progress.CompleteLevel(1);

        Assert.That(progress.LevelNumber, Is.EqualTo(6));
        Assert.That(new PlayerProgress(_key).LevelNumber, Is.EqualTo(6));
    }

    [TestCase(0)]
    [TestCase(-10)]
    public void InvalidSavedLevel_IsRepaired(int levelNumber)
    {
        PlayerPrefs.SetInt(_key, levelNumber);
        LogAssert.Expect(LogType.Warning, "Invalid saved level number. Restoring 1.");

        Assert.That(new PlayerProgress(_key).LevelNumber, Is.EqualTo(1));
        Assert.That(PlayerPrefs.GetInt(_key), Is.EqualTo(1));
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(int.MinValue)]
    public void InvalidCompletedLevel_DoesNotChangeSave(int levelNumber)
    {
        PlayerProgress progress = new(_key);
        progress.CompleteLevel(3);

        Assert.Throws<ArgumentOutOfRangeException>(() => progress.CompleteLevel(levelNumber));
        Assert.That(new PlayerProgress(_key).LevelNumber, Is.EqualTo(4));
    }
}
