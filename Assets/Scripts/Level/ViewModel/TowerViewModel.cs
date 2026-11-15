using System;
using CastleGuard.Level.Model;

namespace CastleGuard.Level.ViewModel;

public sealed class TowerViewModel
{
    private readonly LevelModel _model;

    public event Action StateChanged
    {
        add => _model.StateChanged += value;
        remove => _model.StateChanged -= value;
    }

    public string Id { get; }
    public bool IsCurrent => _model.CurrentTower == Id;
    public bool IsHinted => _model.HintedTower == Id;

    public TowerViewModel(string id, LevelModel model)
    {
        Id = id;
        _model = model;
    }

    public void Click() => _model.MoveTo(Id);
}
