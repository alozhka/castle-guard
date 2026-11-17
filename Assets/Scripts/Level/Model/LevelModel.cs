using System;
using CastleGuard.Level.Model.Definition;
using CastleGuard.Level.Model.Hints;
using CastleGuard.Level.Model.Rewards;
using CastleGuard.Progress;

namespace CastleGuard.Level.Model;

public sealed class LevelModel : IDisposable
{
    private readonly LevelCatalog _levels;
    private readonly PlayerProgress _progress;
    private LevelAttempt _attempt;
    private HintService _hintService;
    private int _levelIndex;
    private bool _isChangingState;

    public event Action StateChanged;

    public LevelDefinition CurrentLevel { get; private set; }
    public bool CanUndo => _attempt.CanUndo;
    public bool CanRestart => !IsComplete;
    public bool CanBuyHint => _hintService.CanPurchase;
    public bool IsComplete => _attempt.IsComplete;
    public string CurrentTower => _attempt.CurrentTower;
    public string HintedTower { get; private set; }
    public int LevelNumber => _levelIndex + 1;
    public int HintPrice { get; }
    public LevelReward? Reward => _attempt.Reward;

    public LevelModel(LevelCatalog levels, PlayerProgress progress, int hintPrice, int initialIndex)
    {
        _levels = levels;
        _progress = progress;
        HintPrice = hintPrice;
        _levelIndex = initialIndex;
        StartAttempt();
        _progress.Wallet.BalanceChanged += HandleBalanceChanged;
    }

    public bool IsWallTraversed(int index) => _attempt.IsWallTraversed(index);

    public void AddElapsedTime(double seconds) => _attempt.TryAddElapsedTime(seconds);

    public void MoveTo(string towerId)
    {
        if (_isChangingState || !_attempt.TryMove(towerId))
        {
            return;
        }

        HintedTower = null;
        _isChangingState = true;
        try
        {
            if (_attempt.Reward.HasValue)
            {
                _progress.Wallet.Credit(_attempt.Reward.Value.Total);
                _progress.CompleteLevel(LevelNumber);
            }
        }
        finally
        {
            _isChangingState = false;
        }

        StateChanged?.Invoke();
    }

    public void Undo()
    {
        if (!_isChangingState && _attempt.TryUndo())
        {
            HintedTower = null;
            StateChanged?.Invoke();
        }
    }

    public void Restart()
    {
        if (!_isChangingState && CanRestart)
        {
            StartAttempt();
            StateChanged?.Invoke();
        }
    }

    public void NextLevel()
    {
        if (!_isChangingState && IsComplete)
        {
            _levelIndex++;
            StartAttempt();
            StateChanged?.Invoke();
        }
    }

    public void BuyHint()
    {
        if (_isChangingState)
        {
            return;
        }

        _isChangingState = true;
        try
        {
            if (_hintService.Purchase().Step is Move step)
            {
                HintedTower = step.To;
            }
        }
        finally
        {
            _isChangingState = false;
        }

        StateChanged?.Invoke();
    }

    public void Dispose() => _progress.Wallet.BalanceChanged -= HandleBalanceChanged;

    private void StartAttempt()
    {
        CurrentLevel = _levels.GetLevel(_levelIndex);
        _attempt = new LevelAttempt(CurrentLevel.StartingTower, CurrentLevel.Walls, CurrentLevel.BaseReward);
        _hintService = new HintService(_progress.Wallet, _attempt, HintPrice);
        HintedTower = null;
    }

    private void HandleBalanceChanged(int balance)
    {
        if (!_isChangingState)
        {
            StateChanged?.Invoke();
        }
    }
}
