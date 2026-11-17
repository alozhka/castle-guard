using CastleGuard.Economy;
using CastleGuard.Level.Model;
using CastleGuard.Level.Model.Definition;
using CastleGuard.Progress;
using NUnit.Framework;
using UnityEngine;

namespace CastleGuard.Tests.EditMode;

public class LevelModelTests
{
    private CoinWallet _wallet;
    private LevelModel _model;
    private MemoryPlayerProgressStore _store;
    private PlayerProgress _progress;
    private LevelDefinition _firstLevel;
    private LevelDefinition _nextLevel;
    private LevelCatalog _levels;

    [SetUp]
    public void SetUp()
    {
        _store = new MemoryPlayerProgressStore();
        _progress = new PlayerProgress(_store);
        _wallet = _progress.Wallet;
        _firstLevel = ScriptableObject.CreateInstance<LevelDefinition>();
        JsonUtility.FromJsonOverwrite(
            "{\"startingTower\":\"A\",\"walls\":[{\"from\":\"A\",\"to\":\"B\"},{\"from\":\"B\",\"to\":\"C\"}]}",
            _firstLevel);
        _nextLevel = Object.Instantiate(_firstLevel);
        _levels = new LevelCatalog(new[] { _firstLevel, _nextLevel, }, null);
        _model = new LevelModel(_levels, _progress, 5, 0);
    }

    [TearDown]
    public void TearDown()
    {
        _model.Dispose();
        _levels.Dispose();
        Object.DestroyImmediate(_firstLevel);
        Object.DestroyImmediate(_nextLevel);
    }

    [Test]
    public void Victory_CreditsAndSavesOnce_AndNextLoadsNextLevel()
    {
        _model.NextLevel();
        Assert.That(_model.CurrentLevel, Is.SameAs(_firstLevel));
        _model.MoveTo("B");
        _model.MoveTo("C");
        _model.MoveTo("C");
        _model.Restart();
        _model.Undo();
        Assert.That(_wallet.Balance, Is.EqualTo(20));
        Assert.That(_progress.LevelNumber, Is.EqualTo(2));
        Assert.That(_store.LevelSaves, Is.EqualTo(1));
        Assert.That(_model.Reward.Value.Total, Is.EqualTo(20));
        Assert.That(_model.CanUndo, Is.False);
        Assert.That(_model.CanRestart, Is.False);
        Assert.That(_model.CanBuyHint, Is.False);
        _model.NextLevel();
        Assert.That(_model.CurrentLevel, Is.SameAs(_nextLevel));
        Assert.That(_model.LevelNumber, Is.EqualTo(2));
        Assert.That(_model.Reward, Is.Null);
        Assert.That(_model.CurrentTower, Is.EqualTo("A"));
    }

    [Test]
    public void Hint_PublishesCompletedPurchase()
    {
        _wallet.Credit(10);
        int notifications = 0;
        _model.StateChanged += () =>
        {
            notifications++;
            Assert.That(_model.HintedTower, Is.EqualTo("B"));
            Assert.That(_model.CanBuyHint, Is.False);
            Assert.That(_wallet.Balance, Is.EqualTo(5));
        };
        _model.BuyHint();
        Assert.That(notifications, Is.EqualTo(1));
    }

    [Test]
    public void UndoAndRestart_ResetBoardAndAttemptBonuses()
    {
        _wallet.Credit(5);
        _model.MoveTo("B");
        _model.BuyHint();
        _model.Undo();
        Assert.That(_model.HintedTower, Is.Null);
        Assert.That(_model.CurrentTower, Is.EqualTo("A"));
        Assert.That(_model.IsWallTraversed(0), Is.False);
        _model.AddElapsedTime(1000);
        _model.Restart();
        Assert.That(_model.CurrentLevel, Is.SameAs(_firstLevel));
        Assert.That(_model.Reward, Is.Null);
        _model.MoveTo("B");
        _model.MoveTo("C");
        Assert.That(_wallet.Balance, Is.EqualTo(20));
    }

    [Test]
    public void ExternalBalanceChange_NotifiesUntilDisposed()
    {
        int notifications = 0;
        _model.StateChanged += () => notifications++;
        Assert.That(_model.CanBuyHint, Is.False);
        _wallet.Credit(5);
        Assert.That(_model.CanBuyHint, Is.True);
        Assert.That(notifications, Is.EqualTo(1));
        _model.Dispose();
        _wallet.Credit(5);
        Assert.That(notifications, Is.EqualTo(1));
    }

    [Test]
    public void CompletionNotification_ObservesSavedProgress()
    {
        _model.StateChanged += () =>
        {
            if (_model.IsComplete)
            {
                Assert.That(_progress.LevelNumber, Is.EqualTo(2));
                Assert.That(_store.LevelSaves, Is.EqualTo(1));
                Assert.That(_wallet.Balance, Is.EqualTo(20));
            }
        };
        _model.MoveTo("B");
        _model.MoveTo("C");
    }
}
