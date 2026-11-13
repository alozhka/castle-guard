using CastleGuard.Economy;
using CastleGuard.Economy.Persistence;
using CastleGuard.Progress;
using UnityEngine;

namespace CastleGuard.App;

public static class GameSave
{
    private static CoinWallet _wallet;
    private static PlayerProgress _progress;

    public static CoinWallet Wallet => _wallet ??= new CoinWallet(new PlayerPrefsCoinWalletStore());
    public static PlayerProgress Progress => _progress ??= new PlayerProgress();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _wallet = null;
        _progress = null;
    }
}
