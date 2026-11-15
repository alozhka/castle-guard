using System.Collections.Generic;
using CastleGuard.Economy;
using CastleGuard.Level.Model;
using NUnit.Framework;

namespace CastleGuard.Tests.EditMode;

public class LevelModelTests
{
    private CoinWallet _wallet;
    private LevelModel _model;
    private readonly List<int> _completed = new();
    private readonly List<int> _loaded = new();

    [SetUp]
    public void SetUp()
    {
        _completed.Clear();
        _loaded.Clear();
        _wallet = new CoinWallet(new MemoryPlayerProgressStore());
        _model = new LevelModel(CreateAttempt, _wallet, _completed.Add, 5, 0);
    }

    [TearDown]
    public void TearDown() => _model.Dispose();

    [Test]
    public void Victory_CreditsAndSavesOnce_AndNextLoadsNextLevel()
    {
        _model.NextLevel();
        Assert.That(_loaded, Is.EqualTo(new[] { 0, }));
        _model.MoveTo("B");
        _model.MoveTo("C");
        _model.MoveTo("C");
        _model.Restart();
        _model.Undo();
        Assert.That(_wallet.Balance, Is.EqualTo(20));
        Assert.That(_completed, Is.EqualTo(new[] { 1, }));
        Assert.That(_model.Reward.Value.Total, Is.EqualTo(20));
        Assert.That(_model.CanUndo, Is.False);
        Assert.That(_model.CanRestart, Is.False);
        Assert.That(_model.CanBuyHint, Is.False);
        _model.NextLevel();
        Assert.That(_loaded, Is.EqualTo(new[] { 0, 1, }));
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
        Assert.That(_loaded, Is.EqualTo(new[] { 0, 0, }));
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
                Assert.That(_completed, Is.EqualTo(new[] { 1, }));
                Assert.That(_wallet.Balance, Is.EqualTo(20));
            }
        };
        _model.MoveTo("B");
        _model.MoveTo("C");
    }

    private LevelAttempt CreateAttempt(int index)
    {
        _loaded.Add(index);
        return new LevelAttempt("A", new Wall[] { new("A", "B"), new("B", "C"), }, 10);
    }
}
