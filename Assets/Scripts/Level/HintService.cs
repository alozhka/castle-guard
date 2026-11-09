using CastleGuard.Economy;
using CastleGuard.Level.Model;
using System;
using System.Collections.Generic;

namespace CastleGuard.Level;

public sealed class HintService
{
    private readonly CoinWallet _wallet;
    private readonly LevelAttempt _attempt;
    private bool _isPurchasing;

    public int Price { get; }
    public bool CanPurchase => !_isPurchasing && _attempt.CanUseHint && _wallet.Balance >= Price;

    public HintService(CoinWallet wallet, LevelAttempt attempt, int price)
    {
        _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
        _attempt = attempt ?? throw new ArgumentNullException(nameof(attempt));
        if (price <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price));
        }

        Price = price;
    }

    public HintResult Purchase()
    {
        if (!CanPurchase)
        {
            return default;
        }

        _isPurchasing = true;
        try
        {
            HintResult result = RouteSolver.TrySolve(_attempt, out IReadOnlyList<Move> route)
                ? HintResult.Forward(route[0])
                : HintResult.Undo(_attempt.History[^1]);

            if (!_wallet.TrySpend(Price))
            {
                return default;
            }

            _attempt.TryRecordHintUse();
            return result;
        }
        finally
        {
            _isPurchasing = false;
        }
    }
}
