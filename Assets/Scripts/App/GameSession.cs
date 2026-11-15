using CastleGuard.Progress;
using CastleGuard.Progress.Persistence;
using UnityEngine;

namespace CastleGuard.App;

public static class GameSession
{
    private static GameModel _model;

    public static GameModel Model => _model ??= new GameModel(
        Resources.Load<GameSettings>("GameSettings"),
        new PlayerProgress(new PlayerPrefsPlayerProgressStore()));

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _model?.Dispose();
        _model = null;
    }
}
