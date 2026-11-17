using CastleGuard.Economy;
using CastleGuard.Level;
using CastleGuard.Level.Model;
using CastleGuard.Level.ViewModel;
using NUnit.Framework;
using UnityEngine;

namespace CastleGuard.Tests.EditMode;

public class LevelViewModelTests
{
    private LevelDefinition _firstLevel;
    private LevelDefinition _nextLevel;
    private LevelDefinition _currentLevel;
    private CoinWallet _wallet;
    private LevelModel _model;
    private LevelViewModel _viewModel;

    [SetUp]
    public void SetUp()
    {
        _firstLevel = ScriptableObject.CreateInstance<LevelDefinition>();
        JsonUtility.FromJsonOverwrite(
            "{\"startingTower\":\"A\",\"walls\":[{\"from\":\"A\",\"to\":\"B\"},{\"from\":\"B\",\"to\":\"C\"}]}",
            _firstLevel);
        _nextLevel = Object.Instantiate(_firstLevel);
        _wallet = new CoinWallet(new MemoryPlayerProgressStore { Balance = 5, });
        _model = new LevelModel(CreateAttempt, _wallet, level => { }, 5, 0);
        _viewModel = new LevelViewModel(_model, () => _currentLevel);
    }

    [TearDown]
    public void TearDown()
    {
        _model.Dispose();
        Object.DestroyImmediate(_firstLevel);
        Object.DestroyImmediate(_nextLevel);
    }

    [Test]
    public void NextLevel_ExposesNewBoardBeforeNotifyingView()
    {
        Assert.That(_viewModel.CurrentLevel, Is.SameAs(_firstLevel));
        _viewModel.CreateTower("B").Click();
        _viewModel.CreateTower("C").Click();
        int notifications = 0;
        _viewModel.StateChanged += () =>
        {
            notifications++;
            Assert.That(_viewModel.CurrentLevel, Is.SameAs(_nextLevel));
            Assert.That(_viewModel.LevelTitle, Is.EqualTo("Уровень 2"));
            Assert.That(_viewModel.CreateTower("A").IsCurrent, Is.True);
        };

        _viewModel.NextLevel();

        Assert.That(notifications, Is.EqualTo(1));
    }

    [Test]
    public void TowerClick_UpdatesSharedModelAndPublishesCompletedVictory()
    {
        TowerViewModel start = _viewModel.CreateTower("A");
        TowerViewModel middle = _viewModel.CreateTower("B");
        TowerViewModel end = _viewModel.CreateTower("C");
        int notifications = 0;
        _viewModel.StateChanged += () =>
        {
            notifications++;
            if (_viewModel.IsComplete)
            {
                Assert.That(end.IsCurrent, Is.True);
                Assert.That(_wallet.Balance, Is.EqualTo(25));
                Assert.That(_viewModel.RewardText, Is.EqualTo("+20"));
            }
        };

        Assert.That(start.IsCurrent, Is.True);
        middle.Click();
        Assert.That(start.IsCurrent, Is.False);
        Assert.That(middle.IsCurrent, Is.True);
        Assert.That(_viewModel.IsWallTraversed(0), Is.True);
        end.Click();
        end.Click();

        Assert.That(notifications, Is.EqualTo(2));
        Assert.That(_viewModel.CanUndo, Is.False);
        Assert.That(_viewModel.CanRestart, Is.False);
        Assert.That(_viewModel.CanBuyHint, Is.False);
    }

    [Test]
    public void TowerNotification_ObservesPurchasedHintAndClearsItAfterMove()
    {
        TowerViewModel tower = _viewModel.CreateTower("B");
        int notifications = 0;
        void ObserveState()
        {
            notifications++;
            Assert.That(tower.IsHinted, Is.True);
            Assert.That(_wallet.Balance, Is.Zero);
            Assert.That(_viewModel.CanBuyHint, Is.False);
        }

        tower.StateChanged += ObserveState;
        _viewModel.BuyHint();
        Assert.That(notifications, Is.EqualTo(1));
        tower.StateChanged -= ObserveState;
        tower.Click();
        Assert.That(tower.IsHinted, Is.False);
        Assert.That(tower.IsCurrent, Is.True);
        Assert.That(notifications, Is.EqualTo(1));
    }

    [Test]
    public void Tower_KeepsWorkingAfterUndoRestartAndNextLevel()
    {
        TowerViewModel tower = _viewModel.CreateTower("B");
        tower.Click();
        _viewModel.Undo();
        Assert.That(tower.IsCurrent, Is.False);
        tower.Click();
        _viewModel.Restart();
        Assert.That(tower.IsCurrent, Is.False);
        tower.Click();
        Assert.That(tower.IsCurrent, Is.True);
        _viewModel.CreateTower("C").Click();
        _viewModel.NextLevel();
        Assert.That(_viewModel.LevelTitle, Is.EqualTo("Уровень 2"));
        Assert.That(_viewModel.HintText, Is.EqualTo("Подсказка  5"));
        Assert.That(_viewModel.RewardText, Is.Empty);
        Assert.That(tower.IsCurrent, Is.False);
        tower.Click();
        Assert.That(tower.IsCurrent, Is.True);
    }

    private LevelAttempt CreateAttempt(int index)
    {
        _currentLevel = index == 0 ? _firstLevel : _nextLevel;
        return new LevelAttempt(_currentLevel.StartingTower,
            _currentLevel.Walls, _currentLevel.BaseReward);
    }

}
