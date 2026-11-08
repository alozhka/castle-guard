using CastleGuard.Economy;
using CastleGuard.Economy.Persistence;
using UnityEngine;

namespace CastleGuard.App
{
    public sealed class GameEconomy : MonoBehaviour
    {
        [SerializeField] private EconomySettings settings;

        private static GameEconomy _instance;
        private static CoinWallet _wallet;

        public static CoinWallet Wallet => _wallet ??= new CoinWallet(new PlayerPrefsCoinWalletStore());
        public EconomySettings Settings => settings;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
            _wallet = null;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            settings.Validate();
            _instance = this;
            _wallet ??= new CoinWallet(new PlayerPrefsCoinWalletStore());
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
