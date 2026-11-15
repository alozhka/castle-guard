using CastleGuard.Economy;
using UnityEngine;

namespace CastleGuard.App
{
    public sealed class GameEconomy : MonoBehaviour
    {
        [SerializeField] private EconomySettings settings;

        private static GameEconomy _instance;

        public EconomySettings Settings => settings;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
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
