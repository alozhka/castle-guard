using CastleGuard.Economy;
using CastleGuard.Level.Model;
using CastleGuard.Level.Model.Definition;
using CastleGuard.Model;
using CastleGuard.Progress;
using NUnit.Framework;
using UnityEngine;

namespace CastleGuard.Tests.EditMode;

public class GameModelTests
{
    private GameSettings _settings;
    private PlayerProgress _progress;
    private GameModel _game;

    [SetUp]
    public void SetUp()
    {
        _settings = Resources.Load<GameSettings>("GameSettings");
        _progress = new PlayerProgress(new MemoryPlayerProgressStore());
        _game = new GameModel(_settings, _progress);
    }

    [TearDown]
    public void TearDown()
    {
        _game.Dispose();
    }

    [Test]
    public void StartLevel_SelectsSavedLevelFromCatalog()
    {
        _progress.CompleteLevel(1);

        LevelModel level = _game.StartLevel();

        Assert.That(_game.Level, Is.SameAs(level));
        Assert.That(level.LevelNumber, Is.EqualTo(2));
        Assert.That(_game.CurrentLevel, Is.SameAs(_settings.Levels[1]));
        Assert.That(level.HintPrice, Is.EqualTo(_settings.Economy.HintPrice));
    }

    [Test]
    public void Victory_UpdatesProgressAndWallet_AndSelectsNextLevel()
    {
        LevelModel level = _game.StartLevel();
        CompleteFirstLevel(level);

        Assert.That(_progress.LevelNumber, Is.EqualTo(2));
        Assert.That(_progress.Wallet.Balance, Is.EqualTo(level.Reward.Value.Total));
        Assert.That(_game.CurrentLevel, Is.SameAs(_settings.Levels[0]));

        level.NextLevel();

        Assert.That(level.LevelNumber, Is.EqualTo(2));
        Assert.That(_game.CurrentLevel, Is.SameAs(_settings.Levels[1]));
    }

    [Test]
    public void ReopenAfterVictory_ResumesNextLevelAndKeepsWallet()
    {
        LevelModel first = _game.StartLevel();
        CompleteFirstLevel(first);
        CoinWallet wallet = _progress.Wallet;
        int balance = wallet.Balance;
        _game.EndLevel(first);

        LevelModel resumed = _game.StartLevel();

        Assert.That(resumed.LevelNumber, Is.EqualTo(2));
        Assert.That(_progress.Wallet, Is.SameAs(wallet));
        Assert.That(wallet.Balance, Is.EqualTo(balance));
    }

    [Test]
    public void BeyondCatalog_GeneratesLevel_ReusesItOnRestartAndReleasesItOnExit()
    {
        _progress.CompleteLevel(_settings.Levels.Count);

        LevelModel level = _game.StartLevel();
        LevelDefinition generated = _game.CurrentLevel;
        Assert.That(generated.name, Is.EqualTo($"Уровень {_settings.Levels.Count + 1}"));
        Assert.That(generated.BaseReward, Is.EqualTo(_settings.GeneratedLevel.BaseReward));
        Assert.That(generated, Is.Not.SameAs(_settings.GeneratedLevel));

        level.Restart();
        Assert.That(_game.CurrentLevel, Is.SameAs(generated));

        _game.EndLevel(level);
        Assert.That(generated == null, Is.True);
        Assert.That(_game.Level, Is.Null);
        Assert.That(_game.CurrentLevel, Is.Null);
        Assert.That(_settings.GeneratedLevel != null, Is.True);
    }

    [Test]
    public void ReplacingLevel_UnsubscribesPreviousModel_AndIgnoresItsExit()
    {
        LevelModel first = _game.StartLevel();
        int notifications = 0;
        first.StateChanged += () => notifications++;

        LevelModel second = _game.StartLevel();
        _game.EndLevel(first);
        _progress.Wallet.Credit(10);

        Assert.That(_game.Level, Is.SameAs(second));
        Assert.That(second.CanBuyHint, Is.True);
        Assert.That(notifications, Is.Zero);
    }

    [TestCase(1, 5, 6)]
    [TestCase(5, 5, 6)]
    [TestCase(6, 6, 8)]
    [TestCase(10, 6, 8)]
    [TestCase(11, 7, 10)]
    [TestCase(16, 8, 12)]
    [TestCase(21, 9, 14)]
    [TestCase(100, 9, 14)]
    public void BeyondCatalog_UsesGeneratedLevelNumberForDifficulty(int generatedLevelNumber, int towers, int walls)
    {
        _progress.CompleteLevel(_settings.Levels.Count + generatedLevelNumber - 1);

        LevelModel level = _game.StartLevel();

        Assert.That(level.LevelNumber, Is.EqualTo(_settings.Levels.Count + generatedLevelNumber));
        Assert.That(_game.CurrentLevel.Towers.Count, Is.EqualTo(towers));
        Assert.That(_game.CurrentLevel.Walls.Count, Is.EqualTo(walls));
    }

    private static void CompleteFirstLevel(LevelModel level)
    {
        level.MoveTo("B");
        level.MoveTo("C");
        level.MoveTo("D");
        level.MoveTo("A");
        Assert.That(level.IsComplete, Is.True);
    }
}
