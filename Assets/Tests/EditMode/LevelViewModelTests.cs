using System.Collections.Generic;
using CastleGuard.Economy;
using CastleGuard.Economy.Persistence;
using CastleGuard.Level;
using CastleGuard.Level.Model;
using NUnit.Framework;

namespace CastleGuard.Tests.EditMode;

public class LevelViewModelTests
{
    private sealed class MemoryStore : ICoinWalletStore
    {
        private int _balance;
        public int Load() => _balance;
        public void Save(int balance) => _balance = balance;
    }

    private CoinWallet _wallet;
    private LevelViewModel _viewModel;
    private readonly List<int> _completed = new();
    private readonly List<int> _loaded = new();

    [SetUp]
    public void SetUp()
    {
        _completed.Clear();
        _loaded.Clear();
        _wallet = new CoinWallet(new MemoryStore());
        _viewModel = new LevelViewModel(CreateAttempt, _wallet, _completed.Add, 5, 0);
    }

    [TearDown]
    public void TearDown() => _viewModel.Dispose();

    [Test]
    public void Victory_CreditsAndSavesOnce_AndNextLoadsNextLevel()
    {
        _viewModel.NextLevel();
        Assert.That(_loaded, Is.EqualTo(new[] { 0, }));
        _viewModel.MoveTo("B");
        _viewModel.MoveTo("C");
        _viewModel.MoveTo("C");
        _viewModel.Restart();
        _viewModel.Undo();
        Assert.That(_wallet.Balance, Is.EqualTo(20));
        Assert.That(_completed, Is.EqualTo(new[] { 1, }));
        Assert.That(_viewModel.RewardText, Is.EqualTo("+20"));
        Assert.That(_viewModel.CanUndo, Is.False);
        Assert.That(_viewModel.CanRestart, Is.False);
        Assert.That(_viewModel.CanBuyHint, Is.False);
        _viewModel.NextLevel();
        Assert.That(_loaded, Is.EqualTo(new[] { 0, 1, }));
        Assert.That(_viewModel.LevelTitle, Is.EqualTo("Уровень 2"));
        Assert.That(_viewModel.RewardText, Is.Empty);
        Assert.That(_viewModel.CurrentTower, Is.EqualTo("A"));
    }

    [Test]
    public void Hint_PublishesCompletedPurchase()
    {
        _wallet.Credit(10);
        int notifications = 0;
        _viewModel.StateChanged += () =>
        {
            notifications++;
            Assert.That(_viewModel.HintedTower, Is.EqualTo("B"));
            Assert.That(_viewModel.CanBuyHint, Is.False);
            Assert.That(_wallet.Balance, Is.EqualTo(5));
        };
        _viewModel.BuyHint();
        Assert.That(notifications, Is.EqualTo(1));
    }

    [Test]
    public void UndoAndRestart_ResetBoardAndAttemptBonuses()
    {
        _wallet.Credit(5);
        _viewModel.MoveTo("B");
        _viewModel.BuyHint();
        _viewModel.Undo();
        Assert.That(_viewModel.HintedTower, Is.Null);
        Assert.That(_viewModel.CurrentTower, Is.EqualTo("A"));
        Assert.That(_viewModel.IsWallTraversed(0), Is.False);
        _viewModel.AddElapsedTime(1000);
        _viewModel.Restart();
        Assert.That(_loaded, Is.EqualTo(new[] { 0, 0, }));
        _viewModel.MoveTo("B");
        _viewModel.MoveTo("C");
        Assert.That(_wallet.Balance, Is.EqualTo(20));
    }

    [Test]
    public void ExternalBalanceChange_NotifiesUntilDisposed()
    {
        int notifications = 0;
        _viewModel.StateChanged += () => notifications++;
        Assert.That(_viewModel.CanBuyHint, Is.False);
        _wallet.Credit(5);
        Assert.That(_viewModel.CanBuyHint, Is.True);
        Assert.That(notifications, Is.EqualTo(1));
        _viewModel.Dispose();
        _wallet.Credit(5);
        Assert.That(notifications, Is.EqualTo(1));
    }

    [Test]
    public void CompletionNotification_ObservesSavedProgress()
    {
        _viewModel.StateChanged += () =>
        {
            if (_viewModel.IsComplete)
            {
                Assert.That(_completed, Is.EqualTo(new[] { 1, }));
                Assert.That(_wallet.Balance, Is.EqualTo(20));
            }
        };
        _viewModel.MoveTo("B");
        _viewModel.MoveTo("C");
    }

    private LevelAttempt CreateAttempt(int index)
    {
        _loaded.Add(index);
        return new LevelAttempt("A", new Wall[] { new("A", "B"), new("B", "C"), }, 10);
    }
}
