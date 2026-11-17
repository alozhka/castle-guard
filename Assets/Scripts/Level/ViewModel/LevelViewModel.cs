using System;
using CastleGuard.Level.Model;
using CastleGuard.Level.Model.Definition;

namespace CastleGuard.Level.ViewModel;

public sealed class LevelViewModel
{
    private readonly LevelModel _model;
    private readonly Func<LevelDefinition> _getCurrentLevel;

    public event Action StateChanged
    {
        add => _model.StateChanged += value;
        remove => _model.StateChanged -= value;
    }

    public LevelDefinition CurrentLevel => _getCurrentLevel();
    public bool CanUndo => _model.CanUndo;
    public bool CanRestart => _model.CanRestart;
    public bool CanBuyHint => _model.CanBuyHint;
    public bool IsComplete => _model.IsComplete;
    public string LevelTitle => $"Уровень {_model.LevelNumber}";
    public string HintText => $"Подсказка  {_model.HintPrice}";
    public string RewardText => _model.Reward.HasValue ? $"+{_model.Reward.Value.Total}" : string.Empty;

    public LevelViewModel(LevelModel model, Func<LevelDefinition> getCurrentLevel)
    {
        _model = model;
        _getCurrentLevel = getCurrentLevel;
    }

    public TowerViewModel CreateTower(string towerId) => new(towerId, _model);

    public bool IsWallTraversed(int index) => _model.IsWallTraversed(index);

    public void AddElapsedTime(double seconds) => _model.AddElapsedTime(seconds);

    public void Undo() => _model.Undo();

    public void Restart() => _model.Restart();

    public void NextLevel() => _model.NextLevel();

    public void BuyHint() => _model.BuyHint();
}
