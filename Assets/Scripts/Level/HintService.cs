using CastleGuard.Economy;
using CastleGuard.Level.Model;
using System;

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

    public bool TryPurchase()
    {
        if (!CanPurchase)
        {
            return false;
        }

        _isPurchasing = true;
        try
        {
            if (!_wallet.TrySpend(Price))
            {
                return false;
            }

            return _attempt.TryRecordHintUse();
        }
        finally
        {
            _isPurchasing = false;
        }
    }
}
