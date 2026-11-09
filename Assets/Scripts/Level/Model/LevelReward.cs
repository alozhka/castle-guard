namespace CastleGuard.Level.Model;

public readonly struct LevelReward
{
    public int BaseReward { get; }
    public decimal Multiplier { get; }
    public int Total { get; }

    internal LevelReward(int baseReward, decimal multiplier)
    {
        BaseReward = baseReward;
        Multiplier = multiplier;
        Total = (int)decimal.Ceiling(baseReward * multiplier);
    }
}
