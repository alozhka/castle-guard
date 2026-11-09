using CastleGuard.App;
using UnityEngine;
using UnityEngine.UI;

namespace CastleGuard.Economy
{
    public sealed class CoinBalanceView : MonoBehaviour
    {
        [SerializeField] private Text amount;

        private CoinWallet _wallet;

        private void OnEnable()
        {
            _wallet = GameEconomy.Wallet;
            _wallet.BalanceChanged += ShowBalance;
            ShowBalance(_wallet.Balance);
        }

        private void OnDisable()
        {
            _wallet.BalanceChanged -= ShowBalance;
            _wallet = null;
        }

        private void ShowBalance(int balance)
        {
            amount.text = balance.ToString();
        }
    }
}
