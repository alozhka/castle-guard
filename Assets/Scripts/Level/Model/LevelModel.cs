using System;
using CastleGuard.Economy;

namespace CastleGuard.Level.Model;

public sealed class LevelModel : IDisposable
{
    private readonly Func<int, LevelAttempt> _createAttempt;
    private readonly CoinWallet _wallet;
    private readonly Action<int> _completeLevel;
    private LevelAttempt _attempt;
    private HintService _hintService;
    private int _levelIndex;
    private bool _isChangingState;

    public event Action StateChanged;

    public bool CanUndo => _attempt.CanUndo;
    public bool CanRestart => !IsComplete;
    public bool CanBuyHint => _hintService.CanPurchase;
    public bool IsComplete => _attempt.IsComplete;
    public string CurrentTower => _attempt.CurrentTower;
    public string HintedTower { get; private set; }
    public int LevelNumber => _levelIndex + 1;
    public int HintPrice { get; }
    public LevelReward? Reward => _attempt.Reward;

    public LevelModel(Func<int, LevelAttempt> createAttempt, CoinWallet wallet,
        Action<int> completeLevel, int hintPrice, int initialIndex)
    {
        _createAttempt = createAttempt;
        _wallet = wallet;
        _completeLevel = completeLevel;
        HintPrice = hintPrice;
        _levelIndex = initialIndex;
        StartAttempt();
        _wallet.BalanceChanged += HandleBalanceChanged;
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
                _wallet.Credit(_attempt.Reward.Value.Total);
                _completeLevel(_levelIndex + 1);
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

    public void Dispose() => _wallet.BalanceChanged -= HandleBalanceChanged;

    private void StartAttempt()
    {
        _attempt = _createAttempt(_levelIndex);
        _hintService = new HintService(_wallet, _attempt, HintPrice);
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
